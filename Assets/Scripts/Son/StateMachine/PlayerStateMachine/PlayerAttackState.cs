using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;

    // 子ミキサー（常駐・PlayerMovement側作成）：A/B 段間クロスフェード
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

    // 主層の先行フェード開始済み
    private bool mainExitStarted;

    private const float HIT_VFX_TOWARD_PLAYER = 1f;
    private const float HIT_VFX_SEQUENCE_DELAY = 0.1f;
    private DamageData damageData = new DamageData(1);

    private static readonly Collider[] hitBuffer = new Collider[32];
    private static readonly System.Collections.Generic.HashSet<Enemy> uniqueEnemyHits
        = new System.Collections.Generic.HashSet<Enemy>();

    private bool lungeInvoked;                  // この段で突進をもう呼んだか
    private const float LUNGE_INPUT_MIN = 0.2f; // 「十分な入力」とみなす強さ（0.2）※必要なら Inspector 化してもよい
    private readonly AnimationCurve defaultLungeCurve = null; // 必要なら後で差し替え


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

        // 子ミキサー参照（常駐接続）
        actionMixer = _player.GetActionSubMixer();

        // A/B 初期化：A に首段を事前装填、いったん 0 重みでポーズ確定→持ち上げ
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null);
        actionMixer.SetInputWeight(0, 0f);
        actionMixer.SetInputWeight(1, 0f);

        // 0 重み状態で time=0 にセットし、グラフを1回評価して姿勢ジャンプを防ぐ
        playableA.SetTime(0);
        _player.EvaluateGraphOnce();
        actionMixer.SetInputWeight(0, 1f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;

        // メイン層を Action へフェード（攻撃は全身を占有）
        float enterDur = _player.ResolveBlendDuration(_player.lastBlendState, PlayerState.Attack);
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);

        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
        lungeInvoked = false;
    }

    public void OnExit()
    {

        // ★ 安全策：攻撃サブミキサーの重みを即ゼロ化
        if (actionMixer.IsValid())
        {
            actionMixer.SetInputWeight(0, 0f);
            actionMixer.SetInputWeight(1, 0f);
        }

        // 末尾付近だけ Idle/Move 先行フェードの保険
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

        // 入力バッファ
        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;
        float norm = (float)(elapsedTime / actionDuration);

        // 入力窓内：押しっぱ／バッファで次段を予約
        if (!queuedNext && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // ===== 先行フェード（「整段末尾」基準）：連撃無しのときだけ実施 =====
        // 連撃に使う chainEnd は「endNormalizedTime * length」
        double chainEndTime = ((currentAction.endNormalizedTime > 0f)
                                ? Mathf.Clamp01(currentAction.endNormalizedTime)
                                : 1f) * actionDuration;

        // Locomotion に戻る判断は「クリップ末尾」基準（整段末尾）
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

        // 攻撃VFX（時刻到達で一回）
        if (!hasSpawnedAttackVFX && currentAction.attackVFXPrefab != null && elapsedTime >= currentAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // ヒット判定（時刻指定）
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }
        // ==== ★ 突進のトリガ（1回だけ / 時刻到達で実行） ====
        if (!lungeInvoked && currentAction.lungeDistance > 0f && elapsedTime >= currentAction.lungeTime)
        {
            DoLungeForCurrentAction();
            lungeInvoked = true;
        }

        // 段間切替（連撃）：chainEndTime 到達で次段へ
        if ( queuedNext && HasNextCombo() && elapsedTime >= chainEndTime)
        {
            CrossfadeToNext();
            return;
        }

        // 整段終了：FSM を Locomotion 側へ
        if (elapsedTime >= clipEndTime)
        {
            if ( queuedNext && HasNextCombo()) CrossfadeToNext();
            else _player.ToIdle();
        }
    }

    // ===== 段間クロスフェード（A/B） =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);

        // 0重みで姿勢を確定してから持ち上げる
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

    // ===== クリップ差し替え（A/B 事前装填） =====
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
            // IK 系は使用しない前提で統一
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

    // ===== 入力窓 =====
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

    // ===== ヒット判定 =====
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
    // === 突進実行（優先度: 入力方向 > ロックオン対象 > 現在正面） ===
    private void DoLungeForCurrentAction()
    {
        float distance = Mathf.Max(0f, currentAction.lungeDistance);
        if (distance <= 0f) return; // 0 は「突進しない」

        float speed = Mathf.Max(0.01f, currentAction.lungeSpeed);
        AnimationCurve curve = defaultLungeCurve; // 必要なら currentAction に追加して使い分け可能

        // 1) 強い移動入力があるか？
        Vector3 dir;
        bool hasStrongInput = _player.TryGetMoveDirectionWorld(LUNGE_INPUT_MIN * LUNGE_INPUT_MIN, out dir);
        if (hasStrongInput)
        {
            // 入力方向へ即時回頭 → 前方突進（CustomDir 指定）
            _player.RotateYawOverTime(dir, 0f); // 即時スナップ
            EventBus.PlayerEvents.LungeByDistance?.Invoke(
                LungeManager.LungeAim.CustomDir,
                Vector3.zero,     // ToTarget 未使用
                dir,              // カスタム方向
                speed,
                distance,
                curve
            );
            return;
        }

        // 2) ロックオン対象があるか？
        if (_player.TryGetLockOnHorizontalDirection(out dir))
        {
            // 敵方向へ即時回頭 → 前方突進
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

        // 3) どちらも無い → 回頭せず現在の正面へ
        EventBus.PlayerEvents.LungeByDistance?.Invoke(
            LungeManager.LungeAim.Forward,
            Vector3.zero,
            Vector3.zero, // 未使用
            speed,
            distance,
            curve
        );
    }
}



