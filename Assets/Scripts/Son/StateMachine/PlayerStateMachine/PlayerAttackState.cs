using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;

    // �q�~�L�T�[�i�풓�j�FA/B �Œi���N���X�t�F�[�h
    private AnimationMixerPlayable actionMixer; // �Q�Ƃ����B���̂� PlayerMovement ���ɏ풓
    private AnimationClipPlayable playableA;
    private AnimationClipPlayable playableB;
    private int activeSlot; // 0:A / 1:B

    private ComboAction currentAction;
    private int currentComboIndex;
    private double actionDuration;
    private double elapsedTime;

    private bool queuedNext;
    private bool forceReset;
    private float inputBufferTime = 0.2f;
    private float inputBufferedTimer;

    private bool hasCheckedHit;
    private bool hasSpawnedAttackVFX;

    // ��w�́u��s�t�F�[�h�A�E�g�v����x�����J�n����t���O
    private bool mainExitStarted;

    private const float HIT_VFX_TOWARD_PLAYER = 1f;
    private const float HIT_VFX_SEQUENCE_DELAY = 0.1f;
    private DamageData damageData = new DamageData(1);

    private static readonly Collider[] hitBuffer = new Collider[32];
    private static readonly System.Collections.Generic.HashSet<Enemy> uniqueEnemyHits = new System.Collections.Generic.HashSet<Enemy>();

    private WeaponInstance weapon;

    public PlayerAttackState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {
        PlayerEvents.OnWeaponBroke += OnWeaponBroke;

        queuedNext = false;
        mainExitStarted = false;
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

        // �q�~�L�T�[�Q�Ɓi�풓�j
        actionMixer = _player.GetActionSubMixer();

        // A/B �������iA �Ɏ�i�����O���U 0�d�݂ŗ\A=1 �Ɏ����グ�j
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null);
        actionMixer.SetInputWeight(0, 0f);
        actionMixer.SetInputWeight(1, 0f);

        // 0�d�݂Ŏ�?�����Ă��玝���グ��i�p���W�����v�h�~�j
        playableA.SetTime(0);
        _player.EvaluateGraphOnce();
        actionMixer.SetInputWeight(0, 1f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;

        // ��w�� Action �փN���X�t�F�[�h�i�K�� Action ���ɗL���N���b�v�������Ă���j
        float enterDur = _player.ResolveBlendDuration(_player.lastBlendState, PlayerState.Attack);
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);

        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }

    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;

        // ��w�̒W�o�́u���͑�������s�J�n�v�ōς�ł���z��B
        // �܂��J�n���Ă��Ȃ���΂����ōŏI�I�� Locomotion �փt�F�[�h�B
        float rem = (float)(actionDuration - elapsedTime);
        float gate = Mathf.Max(0f, currentAction.blendToNext);

        if (!queuedNext && !mainExitStarted && rem <= gate + 1e-4f)
        {
            var nextSlot = _player.HasMoveInput() ? PlayerMovement.MainLayerSlot.Move
                                                  : PlayerMovement.MainLayerSlot.Idle;

            float desired = _player.ResolveBlendDuration(PlayerState.Attack,
                            _player.HasMoveInput() ? PlayerState.Move : PlayerState.Idle);
            float exitDur = Mathf.Min(desired, rem);

            _player.BlendToMainSlot(nextSlot, exitDur);
            mainExitStarted = true;
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (currentAction == null) return;

        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;
        float norm = (float)(elapsedTime / actionDuration);

        // ���͑����F��������/�o�b�t�@�Ŏ��i�v��
        if (!queuedNext && !forceReset && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // �����������_�Łu�������i�Ȃ��v���m��  ��w���s�t�F�[�h�A�E�g�J�n�i�d�Ȃ莞�Ԃ��m�ہj
        if (!queuedNext && !mainExitStarted && HasInputWindowClosed(norm, currentAction))
        {
            var nextSlot = _player.HasMoveInput() ? PlayerMovement.MainLayerSlot.Move : PlayerMovement.MainLayerSlot.Idle;
            float exitDur = _player.ResolveBlendDuration(PlayerState.Attack,
                _player.HasMoveInput() ? PlayerState.Move : PlayerState.Idle);
            _player.BlendToMainSlot(nextSlot, exitDur);
            mainExitStarted = true;
        }

        // �U��VFX�i�^�C�~���O���B�ň��j
        if (!hasSpawnedAttackVFX && currentAction.attackVFXPrefab != null && elapsedTime >= currentAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // �q�b�g����i�w�莞���j
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // End �����ł̑J��
        double endTime = Mathf.Clamp01(currentAction.endNormalizedTime) * actionDuration;
        if (!forceReset && queuedNext && HasNextCombo() && elapsedTime >= endTime)
        {
            CrossfadeToNext();
            return;
        }

        if (elapsedTime >= actionDuration)
        {
            if (!forceReset && queuedNext && HasNextCombo()) CrossfadeToNext();
            else _player.ToIdle(); // Attack -> Idle�i�܂��� Move ��FSM�����͂őJ�ځj
        }
    }

    // ===== �q�~�L�T�[�i�ԃN���X�t�F�[�h =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);

        // 0�d�݂Ŏ�?��?���Ă���グ��
        _player.EvaluateGraphOnce();

        nextPlayable.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed));

        float blend = Mathf.Max(0f, currentAction.blendToNext);
        _player.StartCoroutine(CrossfadeCoroutine(activeSlot, nextSlot, blend));

        currentAction = nextAction;
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;
        queuedNext = false;
        activeSlot = nextSlot;

        if (currentAction.swingSFX)
        {
            switch (currentAction.actionType)
            {
                case ATKActType.BasicCombo: _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX); break;
                case ATKActType.ComboEnd: _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.LHand, currentAction.swingSFX); break;
            }
        }
    }

    // ===== �N���b�v�����ւ��i���O���U�j =====
    private void CreateOrReplacePlayable(int slot, AnimationClip clip)
    {
        if (slot == 0)
        {
            if (playableA.IsValid())
            {
                actionMixer.DisconnectInput(0);
                playableA.Destroy();
            }
            playableA = (clip != null) ? AnimationClipPlayable.Create(_player.playableGraph, clip)
                                       : AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
            // IK �n�͎g��Ȃ��O��œ���
            playableA.SetApplyFootIK(false);
            playableA.SetApplyPlayableIK(false);
            playableA.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed));

            actionMixer.ConnectInput(0, playableA, 0, 0f);
        }
        else
        {
            if (playableB.IsValid())
            {
                actionMixer.DisconnectInput(1);
                playableB.Destroy();
            }
            playableB = (clip != null) ? AnimationClipPlayable.Create(_player.playableGraph, clip)
                                       : AnimationClipPlayable.Create(_player.playableGraph, new AnimationClip());
            playableB.SetApplyFootIK(false);
            playableB.SetApplyPlayableIK(false);
            playableB.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed));

            actionMixer.ConnectInput(1, playableB, 0, 0f);
        }
    }

    private System.Collections.IEnumerator CrossfadeCoroutine(int fromSlot, int toSlot, float duration)
    {
        if (duration <= 0f)
        {
            actionMixer.SetInputWeight(fromSlot, 0f);
            actionMixer.SetInputWeight(toSlot, 1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);
            actionMixer.SetInputWeight(fromSlot, 1f - w);
            actionMixer.SetInputWeight(toSlot, w);
            yield return null;
        }
        actionMixer.SetInputWeight(fromSlot, 0f);
        actionMixer.SetInputWeight(toSlot, 1f);
    }

    // ===== ���͑� =====
    private bool IsInInputWindow(float normalizedTime, ComboAction action)
    {
        float s = Mathf.Clamp01(action.inputWindowStart);
        float e = Mathf.Clamp01(Mathf.Max(action.inputWindowStart, action.inputWindowEnd));
        return normalizedTime >= s && normalizedTime <= e;
    }
    private bool HasInputWindowClosed(float normalizedTime, ComboAction action)
    {
        float e = Mathf.Clamp01(Mathf.Max(action.inputWindowStart, action.inputWindowEnd));
        return normalizedTime > e;
    }

    private bool HasNextCombo()
    {
        var list = weapon?.template?.mainWeaponCombo;
        if (list == null) return false;
        if (currentComboIndex >= list.Count - 1) return false;
        if (currentAction.actionType == ATKActType.ComboEnd) return false;
        return true;
    }

    // ====== �q�b�g���� ======
    private void DoAttackHitCheck()
    {
        Vector3 worldCenter = _player.transform.TransformPoint(currentAction.hitBoxCenter);
        Vector3 halfExtents = currentAction.hitBoxSize * 0.5f;
        Quaternion rot = _player.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, hitBuffer, rot, ~0, QueryTriggerInteraction.Ignore);

        if (_player.isHitboxVisible) ShowHitBox(worldCenter, currentAction.hitBoxSize, rot, 0.1f, Color.red);

        uniqueEnemyHits.Clear();
        bool anyHit = false;

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

                Vector3 enemyCenter = col.bounds.center;
                Vector3 toPlayer = _player.transform.position - enemyCenter;

                Vector3 fxPos = (toPlayer.sqrMagnitude > 1e-6f)
                    ? enemyCenter + toPlayer.normalized * HIT_VFX_TOWARD_PLAYER
                    : enemyCenter + Vector3.up * 0.1f;

                vfxPositions.Add(fxPos);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Enemy.TakeDamage threw: {e.Message}");
            }
        }

        if (anyHit && weapon?.template != null)
            _player.weaponInventory.ConsumeDurability(HandType.Main, currentAction.durabilityCost);

        if (vfxPositions.Count > 0)
        {
            vfxPositions.Sort((a, b) =>
            {
                float da = (a - _player.transform.position).sqrMagnitude;
                float db = (b - _player.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            _player.StartCoroutine(SpawnHitVFXSequence(vfxPositions, HIT_VFX_SEQUENCE_DELAY));
        }

        for (int i = 0; i < count; ++i) hitBuffer[i] = null;
    }

    private System.Collections.IEnumerator SpawnHitVFXSequence(System.Collections.Generic.List<Vector3> positions, float interval)
    {
        for (int i = 0; i < positions.Count; ++i)
        {
            SpawnHitVFXAt(positions[i]);
            if (i < positions.Count - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnAttackVFX()
    {
        if (currentAction.attackVFXPrefab == null) return;
        Vector3 worldCenter = _player.transform.position;
        Quaternion rot = _player.transform.rotation;
        var go = Object.Instantiate(currentAction.attackVFXPrefab, worldCenter, rot);
        Object.Destroy(go, 3f);
    }

    public void AnimEvent_DoHitCheck()
    {
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

    private void SpawnHitVFXAt(Vector3 pos)
    {
        var prefab = weapon?.template?.hitVFXPrefab;
        if (prefab == null) return;
        Object.Instantiate(prefab, pos, Quaternion.identity);
    }

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
    private const float HIT_VFX_SEQUENCE_DELAY = 0.15f;
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

        // --- �q�~�L�T�[�\�z�i�������Ȃ�쐬�j ---
        _player.EnsureMixerInputCount(6);
        if (!attackSubMixer.IsValid())
        {
            // ���{��F�U���p2���̓~�L�T�[�iA/B�j
            attackSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 2);
            attackSubMixer.SetInputCount(2);
        }

        // ���{��F�e�~�L�T�[��Action�X���b�g�֎q�~�L�T�[��ڑ��i��ɕ����ڑ�����������j
        int actionSlot = (int)PlayerMovement.MainLayerSlot.Action;
        _player.mixer.DisconnectInput(actionSlot);
        _player.mixer.ConnectInput(actionSlot, attackSubMixer, 0, 1f);

        _player.SnapToMainSlot(PlayerMovement.MainLayerSlot.Action);

        // ���{��FA/B�̏������i�����ŏ��i�����ۂɓ���Ă����j
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

        // �� ���{��F�Ō�Ƀ��C���w��Action�֋ɒZ�N���X�t�F�[�h�i�S�g�ڊǁj
        float enterDur = 0.12f;//Mathf.Max(0.0f, _player.ResolveBlendDuration(_player.lastBlendState,PlayerState.Attack));
        // ���{��F�S�g�ڊǂ̂��߁A�����͔��ɒZ���l�𐄏��i��F0.02�`0.05�j
        //if (enterDur > 0.06f) enterDur = 0.03f; // ���{��F�ی��iInspector���ݒ�Ȃ�Z�k�j

        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);

        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }
    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;
        _player.SnapToMainSlot(PlayerMovement.MainLayerSlot.Idle);
        _player.ReconnectActionPlaceholder();

        if (playableA.IsValid()) playableA.Destroy();
        if (playableB.IsValid()) playableB.Destroy();
        float exitDur = 0.3f;//Mathf.Max(0.0f, _player.ResolveBlendDuration(PlayerState.Attack, PlayerState.Idle));
        //if (exitDur > 0.06f) exitDur = 0.03f; // ���{��F�ی��i�Z���j
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Idle, exitDur);
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
            playableA.SetApplyFootIK(false);
            playableA.SetApplyPlayableIK(false);
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
            playableB.SetApplyFootIK(false);
            playableB.SetApplyPlayableIK(false);
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
        // ���[�J�����S�����[���h
        Vector3 worldCenter = _player.transform.TransformPoint(currentAction.hitBoxCenter);
        Vector3 halfExtents = currentAction.hitBoxSize * 0.5f;
        Quaternion rot = _player.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(
            worldCenter, halfExtents, hitBuffer, rot, ~0, QueryTriggerInteraction.Ignore
        );

        if(_player.isHitboxVisible)ShowHitBox(worldCenter, currentAction.hitBoxSize, rot, 0.1f, Color.red);

        uniqueEnemyHits.Clear();
        bool anyHit = false;

        // VFX�����ʒu�̈ꎞ���X�g�i�������ɕ��ׂ邽�߁j
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
                    // �[�������̏ꍇ�͏�����ɓ�����
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
            // �v���C���[����̋����ŏ����\�[�g�i�߂��G�������G�j
            vfxPositions.Sort((a, b) =>
            {
                float da = (a - _player.transform.position).sqrMagnitude;
                float db = (b - _player.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            _player.StartCoroutine(SpawnHitVFXSequence(vfxPositions, HIT_VFX_SEQUENCE_DELAY));
            // �K�v�Ȃ炱���� SFX ���܂Ƃ߂Ė炷/�e�������ɖ炷�i���̃R���[�`�����ŌĂԁj
        }

        // �o�b�t�@��Еt��
        for (int i = 0; i < count; ++i) hitBuffer[i] = null;
    }

    // ====== �q�b�gVFX��������������R���[�`�� ======
    private System.Collections.IEnumerator SpawnHitVFXSequence(System.Collections.Generic.List<Vector3> positions, float interval)
    {
        // �e�G�ɑ΂��� 0.15s �Ԋu�� VFX ���o��
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
*/