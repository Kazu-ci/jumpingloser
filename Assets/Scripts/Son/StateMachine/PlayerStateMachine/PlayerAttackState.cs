using UnityEngine;
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
        // �� ���Ȃ��̌��R�[�h�����̂܂ܗ��p�i�T�C�Y���͕K�v�ɉ����ăp�����^���j
        float attackRange = 2.0f;
        float boxWidth = 3f;
        float boxHeight = 4f;

        Vector3 center = _player.transform.position
                         + _player.transform.forward * (attackRange * 0.5f)
                         + Vector3.up * 0.5f;

        Vector3 halfExtents = new Vector3(boxWidth * 0.5f, boxHeight * 0.5f, attackRange * 0.5f);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        ShowHitBox(center, halfExtents * 2, _player.transform.rotation, 0.5f, Color.red);

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
}





/*using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;
    private AnimationClipPlayable attackPlayable;
    private double attackDuration;
    private double elapsedTime;
    private bool hasCheckedHit;
    private DamageData damageData = new DamageData(1); // ���̃_���[�W�f�[�^
    private WeaponInstance weapon;

    public PlayerAttackState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        Debug.Log("Enter Attack");

        // ���C������擾
        weapon = _player.GetMainWeapon();
        *//*if (weapon == null || weapon.template.mainWeaponCombo.Count == 0)
        {
            Debug.LogWarning("Weapon or attack clips not found.");
            _player.ToIdle();
            return;
        }*//*

        if(weapon == null)
        {
            weapon = _player?.fist;
            if(weapon == null)
            {
                Debug.LogWarning("No weapon equipped and no fist attack available.");
                return;
            }
        }

        // �ŏ���ComboAction�擾�i�����ł͒P���ɐ擪�Ƃ���j
        ComboAction action = weapon.template.mainWeaponCombo[0];
        damageData = new DamageData(weapon.template.attackPower);

        // ClipPlayable�쐬
        attackPlayable = AnimationClipPlayable.Create(_player.playableGraph, action.animation);
        attackPlayable.SetDuration(action.animation.length);

        // Mixer��Input2�ɐڑ�����iIndex 2��Attack��p�ɂ���z��j
        _player.EnsureMixerInputCount(3);
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackPlayable, 0, 1f);

        // Weight�ݒ�
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack

        // Graph�X�V
        _player.playableGraph.Evaluate();

        attackDuration = action.animation.length;
        elapsedTime = 0.0;

        
        hasCheckedHit = false;
        if (action.swingSFX != null)
        {
            switch (action.actionType)
            {
                case ATKActType.BasicCombo:
                    _player.audioManager.PlayClipOnAudioPart(PlayerAudioPart.RHand, action.swingSFX);
                    break;
                case ATKActType.ComboEnd:
                    _player.audioManager.PlayClipOnAudioPart(PlayerAudioPart.LHand, action.swingSFX);
                    break;
            }
        }
        
    }

    public void OnExit()
    {
        Debug.Log("Exit Attack");

        _player.mixer.SetInputWeight(2, 0f);

        if (attackPlayable.IsValid())
        {
            attackPlayable.Destroy();
        }
    }

    public void OnUpdate(float deltaTime)
    {
        elapsedTime += deltaTime;

        // ����0.2�b��ɔ��肷��i���AnimationEvent�ɒu�������j
        if (!hasCheckedHit && elapsedTime >= 0.2f)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        if (elapsedTime >= attackDuration)
        {
            _player.ToIdle();
        }
    }

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

        ShowHitBox(center, halfExtents * 2, _player.transform.rotation, 0.5f, Color.red);

        bool hitEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"Enemy hit: {hit.gameObject.name}");
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

        if (hitEnemy)
        {
            ComboAction action = weapon.template.mainWeaponCombo[0];
            _player.weaponInventory.ConsumeDurability(HandType.Main, action.durabilityCost);
        }
        else
        {
            Debug.Log("No enemy hit.");
        }
    }

    private void ShowHitBox(Vector3 center, Vector3 size, Quaternion rot, float time, Color color)
    {
        var obj = new GameObject("AttackHitBoxVisualizer");
        var vis = obj.AddComponent<AttackHitBoxVisualizer>();
        vis.Init(center, size, rot, time, color);
    }
}*/