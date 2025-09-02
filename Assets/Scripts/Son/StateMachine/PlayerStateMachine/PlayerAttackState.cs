using UnityEngine;
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
        // ※ あなたの元コードをそのまま利用（サイズ等は必要に応じてパラメタ化）
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
    private DamageData damageData = new DamageData(1); // 仮のダメージデータ
    private WeaponInstance weapon;

    public PlayerAttackState(PlayerMovement player)
    {
        _player = player;
    }

    public void OnEnter()
    {
        Debug.Log("Enter Attack");

        // メイン武器取得
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

        // 最初のComboAction取得（ここでは単純に先頭とする）
        ComboAction action = weapon.template.mainWeaponCombo[0];
        damageData = new DamageData(weapon.template.attackPower);

        // ClipPlayable作成
        attackPlayable = AnimationClipPlayable.Create(_player.playableGraph, action.animation);
        attackPlayable.SetDuration(action.animation.length);

        // MixerのInput2に接続する（Index 2をAttack専用にする想定）
        _player.EnsureMixerInputCount(3);
        _player.mixer.DisconnectInput(2);
        _player.mixer.ConnectInput(2, attackPlayable, 0, 1f);

        // Weight設定
        _player.mixer.SetInputWeight(0, 0f); // Idle
        _player.mixer.SetInputWeight(1, 0f); // Move
        _player.mixer.SetInputWeight(2, 1f); // Attack

        // Graph更新
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

        // 仮で0.2秒後に判定する（後でAnimationEventに置き換え可）
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