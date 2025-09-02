using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;

    // === �U�����C���[�p�F�q�~�L�T�[�i2���͂ŃN���X�t�F�[�h�j ===
    private AnimationMixerPlayable attackSubMixer; // _player.mixer �� Input[2] �ɐڑ�
    private AnimationClipPlayable playableA;
    private AnimationClipPlayable playableB;
    private int activeSlot; // 0:A �Đ��� / 1:B �Đ���

    // ���ݒi
    private ComboAction currentAction;
    private int currentComboIndex;
    private double actionDuration;
    private double elapsedTime;

    // --- ���̓L���[/�o�b�t�@ ---
    private bool queuedNext;                 // ���i�֍s���v�������邩
    private bool forceReset;                 // ����j�󓙂ŋ����I�ɘA�i�I��
    private float inputBufferTime = 0.2f;    // ��s��t�o�b�t�@�i�b�j
    private float inputBufferedTimer;

    // --- �����蔻�� / VFX ---
    private bool hasCheckedHit;              // �i���̃q�b�g������s������
    private bool hasSpawnedAttackVFX;        // �i���̍U��VFX�𐶐�������
    private const float HIT_VFX_TOWARD_PLAYER = 1f;
    private const float HIT_VFX_SEQUENCE_DELAY = 0.05f;
    private DamageData damageData = new DamageData(1);

    // 0GC �p�̈ꎞ�o�b�t�@�i�K�v�ɉ����ăT�C�Y�����j
    private static readonly Collider[] hitBuffer = new Collider[32];
    // �� ����G�̕����R���C�_�d����r�����邽�߂̃Z�b�g
    private static readonly System.Collections.Generic.HashSet<Enemy> uniqueEnemyHits = new System.Collections.Generic.HashSet<Enemy>();

    // --- ����Q�� ---
    private WeaponInstance weapon;

    public PlayerAttackState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        PlayerEvents.OnWeaponBroke += OnWeaponBroke;

        currentComboIndex = 0;
        forceReset = false;

        weapon = _player.GetMainWeapon() ?? _player.fist;
        if (weapon == null || weapon.template == null || weapon.template.mainWeaponCombo == null || weapon.template.mainWeaponCombo.Count == 0)
        {
            Debug.LogWarning("No attack combo found.");
            _player.ToIdle();
            return;
        }

        damageData = new DamageData(weapon.template.attackPower);

        // --- �q�~�L�T�[�\�z�i����̂ݍ쐬�E�ȍ~�g���񂵁j ---
        _player.EnsureMixerInputCount(3);
        if (!attackSubMixer.IsValid())
        {
            attackSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 2);
            attackSubMixer.SetInputCount(2);
        }

        // �e�~�L�T�[�̍U�����͂������ւ�
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackSubMixer, 0, 1f);

        // ���i���[�h
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null);
        attackSubMixer.SetInputWeight(0, 1f);
        attackSubMixer.SetInputWeight(1, 0f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;
        queuedNext = false;

        // �e�~�L�T�[�̏d��
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack(=�q�~�L�T�[)

        _player.playableGraph.Evaluate();

        // SFX�i�C�Ӂj
        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }

    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;

        // �U�����C���[�� 0 �Ɂi�q�~�L�T�[�͉����j
        _player.mixer.SetInputWeight(2, 0f);

        // A/B �̃N���b�v�����j��
        if (playableA.IsValid()) playableA.Destroy();
        if (playableB.IsValid()) playableB.Destroy();
    }

    public void OnUpdate(float deltaTime)
    {
        if (currentAction == null) return;

        // ���̓o�b�t�@�X�V
        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;
        float norm = (float)(elapsedTime / actionDuration);

        // ���̓E�B���h�E���F�������� or �o�b�t�@ �� ���i�v��
        if (!queuedNext && !forceReset && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // �U��VFX�i�{���^�j�F�^�C�~���O���B�ň�x��������
        if (!hasSpawnedAttackVFX && currentAction.attackVFXPrefab != null && elapsedTime >= currentAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // �q�b�g����i���Ԏw�肪�L���Ȃ�j
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // End �����ł̐ؑցi�L���[�ς� & �܂������j
        double endTime = Mathf.Clamp01(currentAction.endNormalizedTime) * actionDuration;
        if (!forceReset && queuedNext && HasNextCombo() && elapsedTime >= endTime)
        {
            CrossfadeToNext();
            return;
        }

        // �t�H�[���o�b�N�F�i�̊��S�I��
        if (elapsedTime >= actionDuration)
        {
            if (!forceReset && queuedNext && HasNextCombo()) CrossfadeToNext();
            else _player.ToIdle();
        }
    }

    // ===== �q�~�L�T�[�F���i�փN���X�t�F�[�h =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        // �Đ��p�����[�^
        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);
        nextPlayable.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed)); // �� ���푬�x�ōĐ����x��␳

        // �u�����h
        float blend = Mathf.Max(0f, currentAction.blendToNext);
        _player.StartCoroutine(CrossfadeCoroutine(activeSlot, nextSlot, blend));

        // �i�̍X�V
        currentAction = nextAction;
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;
        queuedNext = false;
        activeSlot = nextSlot;

        // SFX
        if (currentAction.swingSFX)
        {
            switch (currentAction.actionType)
            {
                case ATKActType.BasicCombo: _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX); break;
                case ATKActType.ComboEnd: _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.LHand, currentAction.swingSFX); break;
            }
        }
    }

    // ===== A/B �̃N���b�v�����ւ��i���݂��Ȃ���ΐ����j =====
    private void CreateOrReplacePlayable(int slot, AnimationClip clip)
    {
        // ���{��Fslot==0 �� A�A1 �� B�B����������Έ�x�ؒf��Destroy���V�K�쐬���Đڑ�
        if (slot == 0)
        {
            if (playableA.IsValid())
            {
                attackSubMixer.DisconnectInput(0);
                playableA.Destroy();
            }
            playableA = (clip != null) ? AnimationClipPlayable.Create(_player.playableGraph, clip)
                                       : AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
            playableA.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed)); // �� ���x�␳
            attackSubMixer.ConnectInput(0, playableA, 0, 0f);
        }
        else
        {
            if (playableB.IsValid())
            {
                attackSubMixer.DisconnectInput(1);
                playableB.Destroy();
            }
            playableB = (clip != null) ? AnimationClipPlayable.Create(_player.playableGraph, clip)
                                       : AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
            playableB.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed)); // �� ���x�␳
            attackSubMixer.ConnectInput(1, playableB, 0, 0f);
        }
    }

    // ===== �d�݃t�F�[�h�p�R���[�`���i�ȈՁj =====
    private System.Collections.IEnumerator CrossfadeCoroutine(int fromSlot, int toSlot, float duration)
    {
        if (duration <= 0f)
        {
            attackSubMixer.SetInputWeight(fromSlot, 0f);
            attackSubMixer.SetInputWeight(toSlot, 1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);
            attackSubMixer.SetInputWeight(fromSlot, 1f - w);
            attackSubMixer.SetInputWeight(toSlot, w);
            yield return null;
        }
        attackSubMixer.SetInputWeight(fromSlot, 0f);
        attackSubMixer.SetInputWeight(toSlot, 1f);
    }

    // ===== ���̓E�B���h�E���� =====
    private bool IsInInputWindow(float normalizedTime, ComboAction action)
    {
        float s = Mathf.Clamp01(action.inputWindowStart);
        float e = Mathf.Clamp01(Mathf.Max(action.inputWindowStart, action.inputWindowEnd));
        return normalizedTime >= s && normalizedTime <= e;
    }

    private bool HasNextCombo()
    {
        var list = weapon?.template?.mainWeaponCombo;
        if (list == null) return false;
        if (currentComboIndex >= list.Count - 1) return false;
        if (currentAction.actionType == ATKActType.ComboEnd) return false;
        return true;
    }

    // ====== �q�b�g����i�e�G���Ƃ�VFX/SFX���o�� & �d���r���E�ʒu�␳�j ======
    private void DoAttackHitCheck()
    {
        // ���{��F���[�J�����S�����[���h
        Vector3 worldCenter = _player.transform.TransformPoint(currentAction.hitBoxCenter);
        Vector3 halfExtents = currentAction.hitBoxSize * 0.5f;
        Quaternion rot = _player.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(
            worldCenter, halfExtents, hitBuffer, rot, ~0, QueryTriggerInteraction.Ignore
        );

        ShowHitBox(worldCenter, currentAction.hitBoxSize, rot, 0.1f, Color.red);

        uniqueEnemyHits.Clear();
        bool anyHit = false;

        // ���{��FVFX�����ʒu�̈ꎞ���X�g�i�������ɕ��ׂ邽�߁j
        var vfxPositions = new System.Collections.Generic.List<Vector3>(8);

        for (int i = 0; i < count; ++i)
        {
            var col = hitBuffer[i];
            if (col == null) continue;

            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            if (!uniqueEnemyHits.Add(enemy)) continue;

            try
            {
                enemy.TakeDamage(damageData);
                anyHit = true;

                // === VFX �ʒu�Z�o�F�G���S���v���C���[������ 0.5f �V�t�g ===
                Vector3 enemyCenter = col.bounds.center;
                Vector3 toPlayer = _player.transform.position - enemyCenter;

                Vector3 fxPos;
                if (toPlayer.sqrMagnitude > 1e-6f)
                {
                    fxPos = enemyCenter + toPlayer.normalized * HIT_VFX_TOWARD_PLAYER;
                }
                else
                {
                    // ���{��F�[�������̏ꍇ�͏�����ɓ�����
                    fxPos = enemyCenter + Vector3.up * 0.1f;
                }

                vfxPositions.Add(fxPos);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Enemy.TakeDamage threw: {e.Message}");
            }
        }

        // ���{��F����������Αϋv����i�i���Ɓj
        if (anyHit && weapon?.template != null)
        {
            _player.weaponInventory.ConsumeDurability(HandType.Main, currentAction.durabilityCost);
        }

        // === VFX �������̋߂����ɕ��ׁA0.15�b�Ԋu�ŏ������� ===
        if (vfxPositions.Count > 0)
        {
            // ���{��F�v���C���[����̋����ŏ����\�[�g�i�߂��G�������G�j
            vfxPositions.Sort((a, b) =>
            {
                float da = (a - _player.transform.position).sqrMagnitude;
                float db = (b - _player.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            _player.StartCoroutine(SpawnHitVFXSequence(vfxPositions, HIT_VFX_SEQUENCE_DELAY));
            // �K�v�Ȃ炱���� SFX ���܂Ƃ߂Ė炷/�e�������ɖ炷�i���̃R���[�`�����ŌĂԁj
        }

        // ���{��F�o�b�t�@��Еt��
        for (int i = 0; i < count; ++i) hitBuffer[i] = null;
    }

    // ====== �q�b�gVFX��������������R���[�`�� ======
    private System.Collections.IEnumerator SpawnHitVFXSequence(System.Collections.Generic.List<Vector3> positions, float interval)
    {
        // ���{��F�e�G�ɑ΂��� 0.15s �Ԋu�� VFX ���o��
        for (int i = 0; i < positions.Count; ++i)
        {
            SpawnHitVFXAt(positions[i]);
            //PlayHitSFX(); // �K�v�Ȃ�ʍĐ��B�S�̂�1�񂾂��Ȃ炱�����ŏ��݂̂ɂ���
            if (i < positions.Count - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }
    }

    // ====== �U��VFX�i�{���^�j�F�q�b�g�ۂɊ֌W�Ȃ��莞�ɏo�� ======
    private void SpawnAttackVFX()
    {
        // ���{��F�q�b�g�{�b�N�X���S�i���E���W�j�ɐ����B�g���C�����͕���\�P�b�g�ɕt���ւ��Ă��ǂ�
        if (currentAction.attackVFXPrefab == null) return;

        Vector3 worldCenter = _player.transform.position;
        Quaternion rot = _player.transform.rotation;
        var go = Object.Instantiate(currentAction.attackVFXPrefab, worldCenter, rot);
        // ���{��FVFX �̎��Ȕj���ɗ���B�Ȃ���Έ�莞�Ԍ�ɔj��
        Object.Destroy(go, 3f);
    }

    // ====== AnimationEvent ����Ăׂ���J�t�b�N�i�C�Ӂj ======
    public void AnimEvent_DoHitCheck()
    {
        // ���{��FAnimationEvent �p�BhitCheckTime<0 �̂Ƃ����A�蓮�ŌĂ�
        if (!hasCheckedHit)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }
    }

    public void AnimEvent_SpawnAttackVFX()
    {
        if (!hasSpawnedAttackVFX)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }
    }
    // ====== ����VFX�i����̃f�t�H���g���g�p�B�K�v�Ȃ�A�N�V�����ʂɊg���j ======
    private void SpawnHitVFXAt(Vector3 pos)
    {
        // ���{��FWeaponItem ����VFX���e�G���Ƃɐ���
        var prefab = weapon?.template?.hitVFXPrefab;
        if (prefab == null) return;
        Object.Instantiate(prefab, pos, Quaternion.identity);
    }

    // ====== ����SFX�i�C�Ӂj ======
    private void PlayHitSFX()
    {
        var sfx = weapon?.template?.hitSFX;
        if (sfx != null) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.LHand, sfx);
    }

    // ====== ����j��C�x���g�F�A�i�������I������ ======
    private void OnWeaponBroke(HandType hand)
    {
        if (hand == HandType.Main) forceReset = true;
    }

    private void ShowHitBox(Vector3 center, Vector3 size, Quaternion rot, float time, Color color)
    {
        var obj = new GameObject("AttackHitBoxVisualizer");
        var vis = obj.AddComponent<AttackHitBoxVisualizer>();
        vis.Init(center, size, rot, time, color);
    }
}




/*using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;



    // === �U�����C���[�p�F�q�~�L�T�[�i2���͂ŃN���X�t�F�[�h�j ===
    private AnimationMixerPlayable attackSubMixer; // �� ����� _player.mixer �� Input[2] �ɐڑ�����
    private AnimationClipPlayable playableA;
    private AnimationClipPlayable playableB;
    private int activeSlot; // 0:A ���Đ��� / 1:B ���Đ���

    // ���ݒi
    private ComboAction currentAction;
    private int currentComboIndex;
    private double actionDuration;
    private double elapsedTime;

    // --- ���̓L���[/�o�b�t�@ ---
    private bool queuedNext;      // ���i�֍s���v�������邩
    private bool forceReset;      // ����j�󓙂ŋ����I�ɘA�i�I��
    private float inputBufferTime = 0.2f; // �������s��t����o�b�t�@�i�b�j
    private float inputBufferedTimer;

    // --- �����蔻�� ---
    private bool hasCheckedHit;
    private DamageData damageData = new DamageData(1);

    // --- ����Q�� ---
    private WeaponInstance weapon;

    public PlayerAttackState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        PlayerEvents.OnWeaponBroke += OnWeaponBroke;

        currentComboIndex = 0;
        forceReset = false;

        weapon = _player.GetMainWeapon() ?? _player.fist;
        if (weapon == null || weapon.template == null || weapon.template.mainWeaponCombo == null || weapon.template.mainWeaponCombo.Count == 0)
        {
            Debug.LogWarning("No attack combo found.");
            _player.ToIdle();
            return;
        }

        damageData = new DamageData(weapon.template.attackPower);

        // --- �q�~�L�T�[�\�z�F��x�����������A�ȍ~�� A/B ���g���� ---
        // �� �܂����쐬�Ȃ���
        _player.EnsureMixerInputCount(3);
        if (!attackSubMixer.IsValid())
        {
            attackSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 2);
            // �������񗼕� 0 �d��
            attackSubMixer.SetInputCount(2);
        }

        // ������ Attack ���͂�ؒf���Ďq�~�L�T�[������
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackSubMixer, 0, 1f);

        // ���i�Đ�
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null); // �g��Ȃ����͋�
        attackSubMixer.SetInputWeight(0, 1f);
        attackSubMixer.SetInputWeight(1, 0f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        queuedNext = false;

        // ��ʃ~�L�T�[�̊e�d��
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack(=�q�~�L�T�[)

        _player.playableGraph.Evaluate();

        // SFX�i�C�Ӂj
        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }

    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;

        // �U�����C���[�� 0 �Ɂi�q�~�L�T�[���͎̂g���񂵂������̂Ŕj�����Ȃ��j
        _player.mixer.SetInputWeight(2, 0f);

        // A/B �̃N���b�v�����͓s�x�j��
        if (playableA.IsValid()) playableA.Destroy();
        if (playableB.IsValid()) playableB.Destroy();
    }

    public void OnUpdate(float deltaTime)
    {
        if (currentAction == null) return;

        // �o�b�t�@�X�V
        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;

        // ���͎�t
        float norm = (float)(elapsedTime / actionDuration);
        if (!queuedNext && !forceReset && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // �q�b�g����
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // �� End �����ł̐ؑցi�u�����h����j
        double endTime = Mathf.Clamp01(currentAction.endNormalizedTime) * actionDuration;

        // �E���i���L���[�ς݁A���� End ���B�A���� ���������� �� ���܃N���X�t�F�[�h�J�n
        if (!forceReset && queuedNext && HasNextCombo() && elapsedTime >= endTime)
        {
            CrossfadeToNext(); // �� ����?������i�C��� blend
            return;
        }

        // �E�i�����S�I���i�ی��j�F�i�� or Idle
        if (elapsedTime >= actionDuration)
        {
            if (!forceReset && queuedNext && HasNextCombo())
            {
                CrossfadeToNext(); // �قړ��B���Ȃ������S�̂���
            }
            else
            {
                _player.ToIdle();
            }
        }
    }

    // ===== �q�~�L�T�[�F���i�փN���X�t�F�[�h =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        // ���i�̃A�N�V����
        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        // ��A�N�e�B�u���̃X���b�g�ɍ����ւ�
        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        // ���ԂƏd�݂̏�����
        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);
        nextPlayable.SetSpeed(1);

        // �u�����h
        float blend = Mathf.Max(0f, currentAction.blendToNext);
        // ���{��R�����g�F�蓮��ԁBEvaluate �x�[�X�ŏ��X�ɏd�݂����ւ���ȈՔŁB
        // �i�K�v�Ȃ� DOTween/�J�[�u����� CustomPlayableBehaviour �ɒu�������\�j
        _player.StartCoroutine(CrossfadeCoroutine(activeSlot, nextSlot, blend));

        // ��ԍX�V
        currentAction = nextAction;
        actionDuration = nextAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        queuedNext = false;
        activeSlot = nextSlot;

        // SFX�i�C�ӂ̍��E�o�������j
        if (currentAction.swingSFX)
        {
            switch (currentAction.actionType)
            {
                case ATKActType.BasicCombo: _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX); break;
                case ATKActType.ComboEnd: _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.LHand, currentAction.swingSFX); break;
            }
        }
    }

    // ===== A/B �̃N���b�v�����ւ��i���݂��Ȃ���ΐ����j =====
    private void CreateOrReplacePlayable(int slot, AnimationClip clip)
    {
        // ���{��R�����g�Fslot==0 �� A�A1 �� B�B����������Έ�x�ؒf��Destroy���V�K�쐬���Đڑ��B
        if (slot == 0)
        {
            if (playableA.IsValid())
            {
                attackSubMixer.DisconnectInput(0);
                playableA.Destroy();
            }
            playableA = (clip != null) ? AnimationClipPlayable.Create(_player.playableGraph, clip) : AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
            attackSubMixer.ConnectInput(0, playableA, 0, 0f);
        }
        else
        {
            if (playableB.IsValid())
            {
                attackSubMixer.DisconnectInput(1);
                playableB.Destroy();
            }
            playableB = (clip != null) ? AnimationClipPlayable.Create(_player.playableGraph, clip) : AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
            attackSubMixer.ConnectInput(1, playableB, 0, 0f);
        }
    }

    // ===== �d�݃t�F�[�h�p�R���[�`���i�ȈՁj =====
    private System.Collections.IEnumerator CrossfadeCoroutine(int fromSlot, int toSlot, float duration)
    {
        // ���{��R�����g�Fduration=0 �̏ꍇ�͑��ؑ�
        if (duration <= 0f)
        {
            attackSubMixer.SetInputWeight(fromSlot, 0f);
            attackSubMixer.SetInputWeight(toSlot, 1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);
            attackSubMixer.SetInputWeight(fromSlot, 1f - w);
            attackSubMixer.SetInputWeight(toSlot, w);
            yield return null;
        }
        attackSubMixer.SetInputWeight(fromSlot, 0f);
        attackSubMixer.SetInputWeight(toSlot, 1f);
    }

    // ===== ���̓E�B���h�E���� =====
    private bool IsInInputWindow(float normalizedTime, ComboAction action)
    {
        float s = Mathf.Clamp01(action.inputWindowStart);
        float e = Mathf.Clamp01(Mathf.Max(action.inputWindowStart, action.inputWindowEnd));
        return normalizedTime >= s && normalizedTime <= e;
    }

    private bool HasNextCombo()
    {
        var list = weapon?.template?.mainWeaponCombo;
        if (list == null) return false;
        if (currentComboIndex >= list.Count - 1) return false;
        if (currentAction.actionType == ATKActType.ComboEnd) return false;
        return true;
    }

 
    // ====== �q�b�g���聕�ϋv���� ======
    private void DoAttackHitCheck()
    {
        float attackRange = 2.0f;
        float boxWidth = 3f;
        float boxHeight = 4f;

        Vector3 center = _player.transform.position
                         + _player.transform.forward * (attackRange * 0.5f)
                         + Vector3.up * 0.5f;

        Vector3 halfExtents = new Vector3(boxWidth * 0.5f, boxHeight * 0.5f, attackRange * 0.5f);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        ShowHitBox(center, halfExtents * 2, _player.transform.rotation, 0.1f, Color.red);

        bool hitEnemy = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    try
                    {
                        enemy.TakeDamage(damageData);
                        hitEnemy = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Enemy.TakeDamage threw: {e.Message}");
                    }
                }
            }
        }

        // �E����������ϋv����i�i���Ɓj
        if (hitEnemy && weapon != null && weapon.template != null)
        {
            _player.weaponInventory.ConsumeDurability(HandType.Main, currentAction.durabilityCost);
        }
    }

    // ====== ����j��C�x���g�F�A�i�������I������ ======
    private void OnWeaponBroke(HandType hand)
    {
        if (hand == HandType.Main) forceReset = true;
    }

    private void ShowHitBox(Vector3 center, Vector3 size, Quaternion rot, float time, Color color)
    {
        var obj = new GameObject("AttackHitBoxVisualizer");
        var vis = obj.AddComponent<AttackHitBoxVisualizer>();
        vis.Init(center, size, rot, time, color);
    }
}*/




