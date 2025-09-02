using UnityEngine;
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
    private const float HIT_VFX_SEQUENCE_DELAY = 0.05f;
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

        // --- 子ミキサー構築（初回のみ作成・以降使い回し） ---
        _player.EnsureMixerInputCount(3);
        if (!attackSubMixer.IsValid())
        {
            attackSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 2);
            attackSubMixer.SetInputCount(2);
        }

        // 親ミキサーの攻撃入力を差し替え
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackSubMixer, 0, 1f);

        // 初段ロード
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

        // 親ミキサーの重み
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack(=子ミキサー)

        _player.playableGraph.Evaluate();

        // SFX（任意）
        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }

    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;

        // 攻撃レイヤーを 0 に（子ミキサーは温存）
        _player.mixer.SetInputWeight(2, 0f);

        // A/B のクリップだけ破棄
        if (playableA.IsValid()) playableA.Destroy();
        if (playableB.IsValid()) playableB.Destroy();
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
        // 日本語：ローカル中心→ワールド
        Vector3 worldCenter = _player.transform.TransformPoint(currentAction.hitBoxCenter);
        Vector3 halfExtents = currentAction.hitBoxSize * 0.5f;
        Quaternion rot = _player.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(
            worldCenter, halfExtents, hitBuffer, rot, ~0, QueryTriggerInteraction.Ignore
        );

        ShowHitBox(worldCenter, currentAction.hitBoxSize, rot, 0.1f, Color.red);

        uniqueEnemyHits.Clear();
        bool anyHit = false;

        // 日本語：VFX発生位置の一時リスト（距離順に並べるため）
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
                    // 日本語：ゼロ距離の場合は少し上に逃がす
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
            // 日本語：プレイヤーからの距離で昇順ソート（近い敵→遠い敵）
            vfxPositions.Sort((a, b) =>
            {
                float da = (a - _player.transform.position).sqrMagnitude;
                float db = (b - _player.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            _player.StartCoroutine(SpawnHitVFXSequence(vfxPositions, HIT_VFX_SEQUENCE_DELAY));
            // 必要ならここで SFX をまとめて鳴らす/各発生時に鳴らす（下のコルーチン内で呼ぶ）
        }

        // 日本語：バッファ後片付け
        for (int i = 0; i < count; ++i) hitBuffer[i] = null;
    }

    // ====== ヒットVFXを順次生成するコルーチン ======
    private System.Collections.IEnumerator SpawnHitVFXSequence(System.Collections.Generic.List<Vector3> positions, float interval)
    {
        // 日本語：各敵に対して 0.15s 間隔で VFX を出す
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




/*using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using static EventBus;

public class PlayerAttackState : IState
{
    private PlayerMovement _player;



    // === 攻撃レイヤー用：子ミキサー（2入力でクロスフェード） ===
    private AnimationMixerPlayable attackSubMixer; // ← これを _player.mixer の Input[2] に接続する
    private AnimationClipPlayable playableA;
    private AnimationClipPlayable playableB;
    private int activeSlot; // 0:A が再生中 / 1:B が再生中

    // 現在段
    private ComboAction currentAction;
    private int currentComboIndex;
    private double actionDuration;
    private double elapsedTime;

    // --- 入力キュー/バッファ ---
    private bool queuedNext;      // 次段へ行く要求があるか
    private bool forceReset;      // 武器破壊等で強制的に連段終了
    private float inputBufferTime = 0.2f; // 押下を先行受付するバッファ（秒）
    private float inputBufferedTimer;

    // --- 当たり判定 ---
    private bool hasCheckedHit;
    private DamageData damageData = new DamageData(1);

    // --- 武器参照 ---
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

        // --- 子ミキサー構築：一度だけ生成し、以降は A/B を使い回す ---
        // ※ まだ未作成なら作る
        _player.EnsureMixerInputCount(3);
        if (!attackSubMixer.IsValid())
        {
            attackSubMixer = AnimationMixerPlayable.Create(_player.playableGraph, 2);
            // いったん両方 0 重み
            attackSubMixer.SetInputCount(2);
        }

        // 既存の Attack 入力を切断して子ミキサーを差す
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackSubMixer, 0, 1f);

        // 初段再生
        activeSlot = 0;
        CreateOrReplacePlayable(0, weapon.template.mainWeaponCombo[0].animation);
        CreateOrReplacePlayable(1, null); // 使わない側は空
        attackSubMixer.SetInputWeight(0, 1f);
        attackSubMixer.SetInputWeight(1, 0f);

        currentAction = weapon.template.mainWeaponCombo[0];
        actionDuration = currentAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        queuedNext = false;

        // 上位ミキサーの各重み
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack(=子ミキサー)

        _player.playableGraph.Evaluate();

        // SFX（任意）
        if (currentAction.swingSFX) _player.audioManager?.PlayClipOnAudioPart(PlayerAudioPart.RHand, currentAction.swingSFX);
    }

    public void OnExit()
    {
        PlayerEvents.OnWeaponBroke -= OnWeaponBroke;

        // 攻撃レイヤーを 0 に（子ミキサー自体は使い回ししたいので破棄しない）
        _player.mixer.SetInputWeight(2, 0f);

        // A/B のクリップだけは都度破棄
        if (playableA.IsValid()) playableA.Destroy();
        if (playableB.IsValid()) playableB.Destroy();
    }

    public void OnUpdate(float deltaTime)
    {
        if (currentAction == null) return;

        // バッファ更新
        if (_player.attackPressedThisFrame) inputBufferedTimer = inputBufferTime;
        else if (inputBufferedTimer > 0f) inputBufferedTimer -= deltaTime;

        elapsedTime += deltaTime;

        // 入力受付
        float norm = (float)(elapsedTime / actionDuration);
        if (!queuedNext && !forceReset && IsInInputWindow(norm, currentAction))
        {
            if (_player.attackHeld || inputBufferedTimer > 0f)
            {
                queuedNext = true;
                inputBufferedTimer = 0f;
            }
        }

        // ヒット判定
        if (!hasCheckedHit && currentAction.hitCheckTime >= 0f && elapsedTime >= currentAction.hitCheckTime)
        {
            DoAttackHitCheck();
            hasCheckedHit = true;
        }

        // ★ End 時刻での切替（ブレンドあり）
        double endTime = Mathf.Clamp01(currentAction.endNormalizedTime) * actionDuration;

        // ・次段がキュー済み、かつ End 到達、かつ 続きがある → いまクロスフェード開始
        if (!forceReset && queuedNext && HasNextCombo() && elapsedTime >= endTime)
        {
            CrossfadeToNext(); // ← 立即?入下一段，并做 blend
            return;
        }

        // ・段が完全終了（保険）：進む or Idle
        if (elapsedTime >= actionDuration)
        {
            if (!forceReset && queuedNext && HasNextCombo())
            {
                CrossfadeToNext(); // ほぼ到達しないが安全のため
            }
            else
            {
                _player.ToIdle();
            }
        }
    }

    // ===== 子ミキサー：次段へクロスフェード =====
    private void CrossfadeToNext()
    {
        currentComboIndex++;

        // 次段のアクション
        var list = weapon.template.mainWeaponCombo;
        var nextAction = list[currentComboIndex];

        // 非アクティブ側のスロットに差し替え
        int nextSlot = 1 - activeSlot;
        CreateOrReplacePlayable(nextSlot, nextAction.animation);

        // 時間と重みの初期化
        var nextPlayable = (nextSlot == 0) ? playableA : playableB;
        nextPlayable.SetTime(0);
        nextPlayable.SetSpeed(1);

        // ブレンド
        float blend = Mathf.Max(0f, currentAction.blendToNext);
        // 日本語コメント：手動補間。Evaluate ベースで徐々に重みを入れ替える簡易版。
        // （必要なら DOTween/カーブ制御や CustomPlayableBehaviour に置き換え可能）
        _player.StartCoroutine(CrossfadeCoroutine(activeSlot, nextSlot, blend));

        // 状態更新
        currentAction = nextAction;
        actionDuration = nextAction.animation.length;
        elapsedTime = 0.0;
        hasCheckedHit = false;
        queuedNext = false;
        activeSlot = nextSlot;

        // SFX（任意の左右出し分け）
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
        // 日本語コメント：slot==0 は A、1 は B。既存があれば一度切断→Destroy→新規作成し再接続。
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

    // ===== 重みフェード用コルーチン（簡易） =====
    private System.Collections.IEnumerator CrossfadeCoroutine(int fromSlot, int toSlot, float duration)
    {
        // 日本語コメント：duration=0 の場合は即切替
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

 
    // ====== ヒット判定＆耐久消費 ======
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

        // ・当たったら耐久消費（段ごと）
        if (hitEnemy && weapon != null && weapon.template != null)
        {
            _player.weaponInventory.ConsumeDurability(HandType.Main, currentAction.durabilityCost);
        }
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
}*/




