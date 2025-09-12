using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;

    // �q�~�L�T�[�i�풓�EPlayerMovement���쐬�j�FA/B �i�ԃN���X�t�F�[�h
    private AnimationMixerPlayable actionMixer;
    private AnimationClipPlayable playableA;
    private AnimationClipPlayable playableB;
    private int activeSlot; // 0:A / 1:B

    private ComboAction currentAction;
    private int currentComboIndex;
    private double actionDuration;
    private double elapsedTime;

    private bool queuedNext;
    private float inputBufferTime = 0.2f;
    private float inputBufferedTimer;

    private bool hasCheckedHit;
    private bool hasSpawnedAttackVFX;

    // ��w�̐�s�t�F�[�h�J�n�ς�
    private bool mainExitStarted;

    private const float HIT_VFX_TOWARD_PLAYER = 1f;
    private const float HIT_VFX_SEQUENCE_DELAY = 0.1f;
    private DamageData damageData = new DamageData(1);

    private static readonly Collider[] hitBuffer = new Collider[32];
    private static readonly System.Collections.Generic.HashSet<Enemy> uniqueEnemyHits
        = new System.Collections.Generic.HashSet<Enemy>();

    private bool lungeInvoked;                  // ���̒i�œːi�������Ă񂾂�
    private const float LUNGE_INPUT_MIN = 0.2f; // �u�\���ȓ��́v�Ƃ݂Ȃ������i0.2�j���K�v�Ȃ� Inspector �����Ă��悢
    private readonly AnimationCurve defaultLungeCurve = null; // �K�v�Ȃ��ō����ւ�


    private WeaponInstance weapon;

    public PlayerAttackState(PlayerMovement player) { _player = player; }

    public void OnEnter()
    {

        queuedNext = false;
        mainExitStarted = false;
        currentComboIndex = 0;

        weapon = _player.GetMainWeapon() ?? _player.fist;
        if (weapon == null || weapon.template == null || weapon.template.mainWeaponCombo == null || weapon.template.mainWeaponCombo.Count == 0)
        {
            Debug.LogWarning("No attack combo found.");
            _player.ToIdle();
            return;
        }

        damageData = new DamageData(weapon.template.attackPower);

        // �q�~�L�T�[�Q�Ɓi�풓�ڑ��j
        actionMixer = _player.GetActionSubMixer();

        // A/B �������FA �Ɏ�i�����O���U�A�������� 0 �d�݂Ń|�[�Y�m�聨�����グ
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null);
        actionMixer.SetInputWeight(0, 0f);
        actionMixer.SetInputWeight(1, 0f);

        // 0 �d�ݏ�Ԃ� time=0 �ɃZ�b�g���A�O���t��1��]�����Ďp���W�����v��h��
        playableA.SetTime(0);
        _player.EvaluateGraphOnce();
        actionMixer.SetInputWeight(0, 1f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;

        // ���C���w�� Action �փt�F�[�h�i�U���͑S�g���L�j
        float enterDur = _player.ResolveBlendDuration(_player.lastBlendState, PlayerState.Attack);
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);

        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
        lungeInvoked = false;
    }

    public void OnExit()
    {

        // �� ���S��F�U���T�u�~�L�T�[�̏d�݂𑦃[����
        if (actionMixer.IsValid())
        {
            actionMixer.SetInputWeight(0, 0f);
            actionMixer.SetInputWeight(1, 0f);
        }

        // �����t�߂��� Idle/Move ��s�t�F�[�h�̕ی�
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

        // ���̓o�b�t�@
        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;
        float norm = (float)(elapsedTime / actionDuration);

        // ���͑����F�������ρ^�o�b�t�@�Ŏ��i��\��
        if (!queuedNext && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // ===== ��s�t�F�[�h�i�u���i�����v��j�F�A�������̂Ƃ��������{ =====
        // �A���Ɏg�� chainEnd �́uendNormalizedTime * length�v
        double chainEndTime = ((currentAction.endNormalizedTime > 0f)
                                ? Mathf.Clamp01(currentAction.endNormalizedTime)
                                : 1f) * actionDuration;

        // Locomotion �ɖ߂锻�f�́u�N���b�v�����v��i���i�����j
        double clipEndTime = actionDuration;

        float remToClipEnd = Mathf.Max(0f, (float)(clipEndTime - elapsedTime));
        float gate = Mathf.Max(0f, currentAction.blendToNext);

        if (!queuedNext && !mainExitStarted && remToClipEnd <= gate + 1e-4f)
        {
            var nextSlot = _player.HasMoveInput()
                ? PlayerMovement.MainLayerSlot.Move
                : PlayerMovement.MainLayerSlot.Idle;

            float desired = _player.ResolveBlendDuration(PlayerState.Attack,
                             _player.HasMoveInput() ? PlayerState.Move : PlayerState.Idle);
            float exitDur = Mathf.Min(desired, remToClipEnd);

            _player.BlendToMainSlot(nextSlot, exitDur);
            mainExitStarted = true;
        }

        // �U��VFX�i�������B�ň��j
        if (!hasSpawnedAttackVFX && currentAction.attackVFXPrefab != null && elapsedTime >= currentAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // �q�b�g����i�����w��j
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }
        // ==== �� �ːi�̃g���K�i1�񂾂� / �������B�Ŏ��s�j ====
        if (!lungeInvoked && currentAction.lungeDistance > 0f && elapsedTime >= currentAction.lungeTime)
        {
            DoLungeForCurrentAction();
            lungeInvoked = true;
        }

        // �i�Ԑؑցi�A���j�FchainEndTime ���B�Ŏ��i��
        if ( queuedNext && HasNextCombo() && elapsedTime >= chainEndTime)
        {
            CrossfadeToNext();
            return;
        }

        // ���i�I���FFSM �� Locomotion ����
        if (elapsedTime >= clipEndTime)
        {
            if ( queuedNext && HasNextCombo()) CrossfadeToNext();
            else _player.ToIdle();
        }
    }

    // ===== �i�ԃN���X�t�F�[�h�iA/B�j =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);

        // 0�d�݂Ŏp�����m�肵�Ă��玝���グ��
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
        lungeInvoked = false;
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

    // ===== �N���b�v�����ւ��iA/B ���O���U�j =====
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
            // IK �n�͎g�p���Ȃ��O��œ���
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

    private bool HasNextCombo()
    {
        var list = weapon?.template?.mainWeaponCombo;
        if (list == null) return false;
        if (currentComboIndex >= list.Count - 1) return false;
        if (currentAction.actionType == ATKActType.ComboEnd) return false;
        return true;
    }

    // ===== �q�b�g���� =====
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

    private void ShowHitBox(Vector3 center, Vector3 size, Quaternion rot, float time, Color color)
    {
        var obj = new GameObject("AttackHitBoxVisualizer");
        var vis = obj.AddComponent<AttackHitBoxVisualizer>();
        vis.Init(center, size, rot, time, color);
    }
    // === �ːi���s�i�D��x: ���͕��� > ���b�N�I���Ώ� > ���ݐ��ʁj ===
    private void DoLungeForCurrentAction()
    {
        float distance = Mathf.Max(0f, currentAction.lungeDistance);
        if (distance <= 0f) return; // 0 �́u�ːi���Ȃ��v

        float speed = Mathf.Max(0.01f, currentAction.lungeSpeed);
        AnimationCurve curve = defaultLungeCurve; // �K�v�Ȃ� currentAction �ɒǉ����Ďg�������\

        // 1) �����ړ����͂����邩�H
        Vector3 dir;
        bool hasStrongInput = _player.TryGetMoveDirectionWorld(LUNGE_INPUT_MIN * LUNGE_INPUT_MIN, out dir);
        if (hasStrongInput)
        {
            // ���͕����֑����� �� �O���ːi�iCustomDir �w��j
            _player.RotateYawOverTime(dir, 0f); // �����X�i�b�v
            EventBus.PlayerEvents.LungeByDistance?.Invoke(
                LungeManager.LungeAim.CustomDir,
                Vector3.zero,     // ToTarget ���g�p
                dir,              // �J�X�^������
                speed,
                distance,
                curve
            );
            return;
        }

        // 2) ���b�N�I���Ώۂ����邩�H
        if (_player.TryGetLockOnHorizontalDirection(out dir))
        {
            // �G�����֑����� �� �O���ːi
            _player.RotateYawOverTime(dir, 0f);
            EventBus.PlayerEvents.LungeByDistance?.Invoke(
                LungeManager.LungeAim.CustomDir,
                Vector3.zero,
                dir,
                speed,
                distance,
                curve
            );
            return;
        }

        // 3) �ǂ�������� �� �񓪂������݂̐��ʂ�
        EventBus.PlayerEvents.LungeByDistance?.Invoke(
            LungeManager.LungeAim.Forward,
            Vector3.zero,
            Vector3.zero, // ���g�p
            speed,
            distance,
            curve
        );
    }
}



