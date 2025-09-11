using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;

    // 子ミキサー（常駐）：A/B で段内クロスフェード
    private AnimationMixerPlayable actionMixer; // 参照だけ。実体は PlayerMovement 側に常駐
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

    // 主層の「先行フェードアウト」を一度だけ開始するフラグ
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

        // 子ミキサー参照（常駐）
        actionMixer = _player.GetActionSubMixer();

        // A/B 初期化（A に首段を事前装填 0重みで予A=1 に持ち上げ）
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null);
        actionMixer.SetInputWeight(0, 0f);
        actionMixer.SetInputWeight(1, 0f);

        // 0重みで首?をしてから持ち上げる（姿勢ジャンプ防止）
        playableA.SetTime(0);
        _player.EvaluateGraphOnce();
        actionMixer.SetInputWeight(0, 1f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        hasSpawnedAttackVFX = false;

        // 主層を Action へクロスフェード（必ず Action 槽に有効クリップが入ってから）
        float enterDur = _player.ResolveBlendDuration(_player.lastBlendState, PlayerState.Attack);
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Action, enterDur);

        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }

    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;

        // 主層の淡出は「入力窓閉じ→先行開始」で済んでいる想定。
        // まだ開始していなければここで最終的に Locomotion へフェード。
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

        // 入力窓内：押しっぱ/バッファで次段要求
        if (!queuedNext && !forceReset && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // 窓が閉じた時点で「もう次段なし」が確定  主層を先行フェードアウト開始（重なり時間を確保）
        if (!queuedNext && !mainExitStarted && HasInputWindowClosed(norm, currentAction))
        {
            var nextSlot = _player.HasMoveInput() ? PlayerMovement.MainLayerSlot.Move : PlayerMovement.MainLayerSlot.Idle;
            float exitDur = _player.ResolveBlendDuration(PlayerState.Attack,
                _player.HasMoveInput() ? PlayerState.Move : PlayerState.Idle);
            _player.BlendToMainSlot(nextSlot, exitDur);
            mainExitStarted = true;
        }

        // 攻撃VFX（タイミング到達で一回）
        if (!hasSpawnedAttackVFX && currentAction.attackVFXPrefab != null && elapsedTime >= currentAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // ヒット判定（指定時刻）
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // End 時刻での遷移
        double endTime = Mathf.Clamp01(currentAction.endNormalizedTime) * actionDuration;
        if (!forceReset && queuedNext && HasNextCombo() && elapsedTime >= endTime)
        {
            CrossfadeToNext();
            return;
        }

        if (elapsedTime >= actionDuration)
        {
            if (!forceReset && queuedNext && HasNextCombo()) CrossfadeToNext();
            else _player.ToIdle(); // Attack -> Idle（または Move はFSM側入力で遷移）
        }
    }

    // ===== 子ミキサー段間クロスフェード =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);

        // 0重みで首?采?してから上げる
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

    // ===== クリップ差し替え（事前装填） =====
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
            // IK 系は使わない前提で統一
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

    // ====== ヒット判定 ======
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

    // === 攻撃レイヤー用：子ミキサー（2入力でクロスフェード） ===
    private AnimationMixerPlayable attackSubMixer; // _player.mixer の Input[2] に接続
    private AnimationClipPlayable playableA;
    private AnimationClipPlayable playableB;
    private int activeSlot; // 0:A 再生中 / 1:B 再生中

    // 現在段
    private ComboAction currentAction;
    private int currentComboIndex;
    private double actionDuration;
    private double elapsedTime;

    // --- 入力キュー/バッファ ---
    private bool queuedNext;                 // 次段へ行く要求があるか
    private bool forceReset;                 // 武器破壊等で強制的に連段終了
    private float inputBufferTime = 0.2f;    // 先行受付バッファ（秒）
    private float inputBufferedTimer;

    // --- 当たり判定 / VFX ---
    private bool hasCheckedHit;              // 段内のヒット判定を行ったか
    private bool hasSpawnedAttackVFX;        // 段内の攻撃VFXを生成したか
    private const float HIT_VFX_TOWARD_PLAYER = 1f;
    private const float HIT_VFX_SEQUENCE_DELAY = 0.15f;
    private DamageData damageData = new DamageData(1);

    // 0GC 用の一時バッファ（必要に応じてサイズ調整）
    private static readonly Collider[] hitBuffer = new Collider[32];
    // ★ 同一敵の複数コライダ重複を排除するためのセット
    private static readonly System.Collections.Generic.HashSet<Enemy> uniqueEnemyHits = new System.Collections.Generic.HashSet<Enemy>();

    // --- 武器参照 ---
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

        // --- 子ミキサー構築（未生成なら作成） ---
        _player.EnsureMixerInputCount(6);
        if (!attackSubMixer.IsValid())
        {
            // 日本語：攻撃用2入力ミキサー（A/B）
            attackSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 2);
            attackSubMixer.SetInputCount(2);
        }

        // 日本語：親ミキサーのActionスロットへ子ミキサーを接続（先に物理接続を完了する）
        int actionSlot = (int)PlayerMovement.MainLayerSlot.Action;
        _player.mixer.DisconnectInput(actionSlot);
        _player.mixer.ConnectInput(actionSlot, attackSubMixer, 0, 1f);

        _player.SnapToMainSlot(PlayerMovement.MainLayerSlot.Action);

        // 日本語：A/Bの初期化（ここで初段を実際に入れておく）
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

        // ★ 日本語：最後にメイン層をActionへ極短クロスフェード（全身接管）
        float enterDur = 0.12f;//Mathf.Max(0.0f, _player.ResolveBlendDuration(_player.lastBlendState,PlayerState.Attack));
        // 日本語：全身接管のため、ここは非常に短い値を推奨（例：0.02～0.05）
        //if (enterDur > 0.06f) enterDur = 0.03f; // 日本語：保険（Inspector未設定なら短縮）

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
        //if (exitDur > 0.06f) exitDur = 0.03f; // 日本語：保険（短く）
        _player.BlendToMainSlot(PlayerMovement.MainLayerSlot.Idle, exitDur);
    }

    public void OnUpdate(float deltaTime)
    {
        if (currentAction == null) return;

        // 入力バッファ更新
        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;
        float norm = (float)(elapsedTime / actionDuration);

        // 入力ウィンドウ内：押しっぱ or バッファ → 次段要求
        if (!queuedNext && !forceReset && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // 攻撃VFX（施放型）：タイミング到達で一度だけ生成
        if (!hasSpawnedAttackVFX && currentAction.attackVFXPrefab != null && elapsedTime >= currentAction.attackVFXTime)
        {
            SpawnAttackVFX();
            hasSpawnedAttackVFX = true;
        }

        // ヒット判定（時間指定が有効なら）
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // End 時刻での切替（キュー済み & まだ続く）
        double endTime = Mathf.Clamp01(currentAction.endNormalizedTime) * actionDuration;
        if (!forceReset && queuedNext && HasNextCombo() && elapsedTime >= endTime)
        {
            CrossfadeToNext();
            return;
        }

        // フォールバック：段の完全終了
        if (elapsedTime >= actionDuration)
        {
            if (!forceReset && queuedNext && HasNextCombo()) CrossfadeToNext();
            else _player.ToIdle();
        }
    }

    // ===== 子ミキサー：次段へクロスフェード =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        // 再生パラメータ
        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);
        nextPlayable.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed)); // ★ 武器速度で再生速度を補正

        // ブレンド
        float blend = Mathf.Max(0f, currentAction.blendToNext);
        _player.StartCoroutine(CrossfadeCoroutine(activeSlot, nextSlot, blend));

        // 段の更新
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

    // ===== A/B のクリップ差し替え（存在しなければ生成） =====
    private void CreateOrReplacePlayable(int slot, AnimationClip clip)
    {

        // 日本語：slot==0 は A、1 は B。既存があれば一度切断→Destroy→新規作成→再接続
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
            playableA.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed)); // ★ 速度補正
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
            playableB.SetSpeed(Mathf.Max(0.0001f, weapon.template.attackSpeed)); // ★ 速度補正
            attackSubMixer.ConnectInput(1, playableB, 0, 0f);
        }
    }

    // ===== 重みフェード用コルーチン（簡易） =====
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

    // ===== 入力ウィンドウ判定 =====
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

    // ====== ヒット判定（各敵ごとにVFX/SFXを出す & 重複排除・位置補正） ======
    private void DoAttackHitCheck()
    {
        // ローカル中心→ワールド
        Vector3 worldCenter = _player.transform.TransformPoint(currentAction.hitBoxCenter);
        Vector3 halfExtents = currentAction.hitBoxSize * 0.5f;
        Quaternion rot = _player.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(
            worldCenter, halfExtents, hitBuffer, rot, ~0, QueryTriggerInteraction.Ignore
        );

        if(_player.isHitboxVisible)ShowHitBox(worldCenter, currentAction.hitBoxSize, rot, 0.1f, Color.red);

        uniqueEnemyHits.Clear();
        bool anyHit = false;

        // VFX発生位置の一時リスト（距離順に並べるため）
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

                // === VFX 位置算出：敵中心→プレイヤー方向へ 0.5f シフト ===
                Vector3 enemyCenter = col.bounds.center;
                Vector3 toPlayer = _player.transform.position - enemyCenter;

                Vector3 fxPos;
                if (toPlayer.sqrMagnitude > 1e-6f)
                {
                    fxPos = enemyCenter + toPlayer.normalized * HIT_VFX_TOWARD_PLAYER;
                }
                else
                {
                    // ゼロ距離の場合は少し上に逃がす
                    fxPos = enemyCenter + Vector3.up * 0.1f;
                }

                vfxPositions.Add(fxPos);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Enemy.TakeDamage threw: {e.Message}");
            }
        }

        // 日本語：命中があれば耐久消費（段ごと）
        if (anyHit && weapon?.template != null)
        {
            _player.weaponInventory.ConsumeDurability(HandType.Main, currentAction.durabilityCost);
        }

        // === VFX を距離の近い順に並べ、0.15秒間隔で順次生成 ===
        if (vfxPositions.Count > 0)
        {
            // プレイヤーからの距離で昇順ソート（近い敵→遠い敵）
            vfxPositions.Sort((a, b) =>
            {
                float da = (a - _player.transform.position).sqrMagnitude;
                float db = (b - _player.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            _player.StartCoroutine(SpawnHitVFXSequence(vfxPositions, HIT_VFX_SEQUENCE_DELAY));
            // 必要ならここで SFX をまとめて鳴らす/各発生時に鳴らす（下のコルーチン内で呼ぶ）
        }

        // バッファ後片付け
        for (int i = 0; i < count; ++i) hitBuffer[i] = null;
    }

    // ====== ヒットVFXを順次生成するコルーチン ======
    private System.Collections.IEnumerator SpawnHitVFXSequence(System.Collections.Generic.List<Vector3> positions, float interval)
    {
        // 各敵に対して 0.15s 間隔で VFX を出す
        for (int i = 0; i < positions.Count; ++i)
        {
            SpawnHitVFXAt(positions[i]);
            //PlayHitSFX(); // 必要なら個別再生。全体で1回だけならここを最初のみにする
            if (i < positions.Count - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }
    }

    // ====== 攻撃VFX（施放型）：ヒット可否に関係なく定時に出す ======
    private void SpawnAttackVFX()
    {
        // 日本語：ヒットボックス中心（世界座標）に生成。トレイル等は武器ソケットに付け替えても良い
        if (currentAction.attackVFXPrefab == null) return;

        Vector3 worldCenter = _player.transform.position;
        Quaternion rot = _player.transform.rotation;
        var go = Object.Instantiate(currentAction.attackVFXPrefab, worldCenter, rot);
        // 日本語：VFX の自己破棄に頼る。なければ一定時間後に破棄
        Object.Destroy(go, 3f);
    }

    // ====== AnimationEvent から呼べる公開フック（任意） ======
    public void AnimEvent_DoHitCheck()
    {
        // 日本語：AnimationEvent 用。hitCheckTime<0 のとき等、手動で呼ぶ
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
    // ====== 命中VFX（武器のデフォルトを使用。必要ならアクション別に拡張可） ======
    private void SpawnHitVFXAt(Vector3 pos)
    {
        // 日本語：WeaponItem 側のVFXを各敵ごとに生成
        var prefab = weapon?.template?.hitVFXPrefab;
        if (prefab == null) return;
        Object.Instantiate(prefab, pos, Quaternion.identity);
    }

    // ====== 命中SFX（任意） ======
    private void PlayHitSFX()
    {
        var sfx = weapon?.template?.hitSFX;
        if (sfx != null) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.LHand, sfx);
    }

    // ====== 武器破壊イベント：連段を強制終了する ======
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