using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using static EventBus;

public enum PlayerState
{
    Idle,        // 待機
    Move,        // 歩き
    Hit,         // 被弾
    Pickup,      // ピックアップ
    SwitchWeapon,// 武器切り替え
    Attack,      // 攻撃
    Skill,       // スキル
    Dash,        // ダッシュ
    Dead,        // 死亡
    Falling      // 落下
}

public enum PlayerTrigger
{
    MoveStart,
    MoveStop,
    GetHit,
    StartPickup,
    EndPickup,
    SwitchWeapon,
    AttackInput,
    AttackUp,
    Hold,
    DashInput,
    Die,
    NoGround,
    Grounded,
    SkillInput
}



[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ====== 入力系 ======
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    [HideInInspector] public bool attackHeld;                // 押しっぱなしフラグ
    [HideInInspector] public bool attackPressedThisFrame;    // そのフレームに押下されたか

    [Header("攻撃入力（長押し判定）")]
    [Tooltip("この秒数以上ならスキルとして扱う")]
    public float skillHoldThreshold = 0.35f;   // 例：0.35秒
    private double attackPressStartTime = -1.0; // 押下時刻（Time.timeAsDouble）

    [Header("ステータス")]
    public float maxHealth = 50;
    private float currentHealth = 50;
    public float CurrentHealth => currentHealth;


    // ====== 移動設定 ======
    [Header("移動設定")]
    public float moveSpeed = 5f;           // 移動速度
    public float gravity = -9.81f;         // 重力加速度
    public float rotationSpeed = 360f;     // 回転速度（度/秒）

    [Header("ダッシュ設定")]
    public float dashSpeed = 10f;           // ダッシュ速度
    public float dashDistance = 3f;       // ダッシュ距離
    public float dashCooldown = 1.5f;     // ダッシュのクールタイム
    private float dashCooldownTimer = -Mathf.Infinity; // ダッシュのクールタイム管理用タイマー
    public float dashInvincibilityTime = 0.3f; // ダッシュ無敵時間
    private float dashTimer = 0f;        // 無敵時間管理用タイマー
    public bool IsDashInvincible => dashTimer > 0f;
    public float dashFreezeAtSeconds = 0.02f;

    [Header("被弾設定")]
    public float hitInvincibilityTime = 0.8f; // 被弾無敵時間
    private float hitTimer = 0f;         // 被弾無敵時間管理用タイマー
    public bool IsHitInvincible => hitTimer > 0f;
    public bool IsInvincible => IsDashInvincible || IsHitInvincible;

    public GameObject hitEffectPrefab; // 被弾エフェクト


    // ====== 接地判定 ======
    [Header("接地判定")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // ====== 武器・モデル ======
    [Header("武器関連")]
    public GameObject weaponBoxR;          // 右手の武器親
    public GameObject weaponBoxL;          // 左手の武器親
    public WeaponInstance fist;            // 素手
    public GameObject wpBrokeEffect;      // 武器破壊エフェクト
    public bool isHitboxVisible = true; // ヒットボックス可視化

    // 武器インベントリ
    public PlayerWeaponInventory weaponInventory = new PlayerWeaponInventory();

    [Header("プレイヤーモデル")]
    public GameObject playerModel;
    public Animator playerAnimator;

    // ====== アニメーション（PlayableGraph）======
    [Header("アニメーションクリップ")]
    public AnimationClip idleClip;
    public AnimationClip moveClip;
    public AnimationClip dashClip;
    public AnimationClip fallClip;
    public AnimationClip hitClip;
    public AnimationClip dieClip;
    public PlayableGraph playableGraph;
    public AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable movePlayable;
    private AnimationMixerPlayable actionSubMixer;

    // メインレイヤー用の追加Playable
    private AnimationClipPlayable fallPlayable;
    private AnimationClipPlayable hitPlayable;
    private AnimationClipPlayable deadPlayable;
    private AnimationClipPlayable dashPlayable;
    // アクションスロットのダミー（未接続時の穴埋め）
    private AnimationClipPlayable actionPlaceholder;

    // メインレイヤーのフェード用コルーチン
    private Coroutine mainLayerFadeCo;


    // ================== 状態間ブレンド設定 ==================
    // 個別(from→to)のブレンド時間を上書きするエントリ
    // メインレイヤーのスロット番号（Mixer入力の固定割り当て）
    public enum MainLayerSlot
    {
        Idle = 0, // アイドル
        Move = 1, // 移動
        Action = 2, // 攻撃/スキル
        Falling = 3, // 落下
        Hit = 4, // 被弾
        Dead = 5,  // 死亡
        Dash = 6
    }
    [System.Serializable]
    public class StateBlendEntry
    {
        public PlayerState from;                 // 例：Move
        public PlayerState to;                   // 例：Hit
        [Min(0f)] public float duration = 0.12f; // 例：0.06f（被弾は速く）
    }

    //「to状態」単位のデフォルトブレンド（from未一致時のフォールバック）
    [System.Serializable]
    public class StateDefaultBlend
    {
        public PlayerState to;                   // 例：Hit
        [Min(0f)] public float duration = 0.12f; // 例：0.06f
    }

    [Header("状態間ブレンド（可変）")]
    [Tooltip("全体の規定値")]
    public float defaultStateCrossfade = 0.12f;

    [Header("Dash/Hit割り込みブレンド")]
    public float interruptCrossfade = 0.05f;

    [Tooltip("特定のfrom→toで上書きしたいブレンド時間（任意件）")]
    public List<StateBlendEntry> blendOverrides = new List<StateBlendEntry>();

    [Tooltip("to状態ごとの既定ブレンド（from未一致時に使用）")]
    public List<StateDefaultBlend> toStateDefaults = new List<StateDefaultBlend>();

    // 日本語：直近の論理状態（ブレンド計算用）
    public PlayerState lastBlendState = PlayerState.Idle;

    [Header("サウンド")]
    public PlayerAudioManager audioManager;

    // ====== 内部状態 ======


    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private Transform mainCam;
    private StateMachine<PlayerState, PlayerTrigger> _fsm;
    private GameObject lockOnTarget = null; // ロックオン対象
    public GameObject GetLockOnTarget => lockOnTarget;

    // ====== ライフサイクル ======
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        PlayerEvents.GetPlayerObject = () => this.gameObject; // プレイヤーオブジェクトの提供
    }

    private void OnEnable()
    {
        // --- 入力購読 ---
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // 攻撃キー：押下時は時刻を記録、離した瞬間に短押し/長押しを分岐する
        inputActions.Player.Attack.performed += ctx =>
        {
            //押しっぱ開始。ここでは発動しない（確定は離した瞬間）
            attackHeld = true;
            attackPressedThisFrame = false;                  // このフレームでの先行受付はしない
            attackPressStartTime = Time.timeAsDouble;        // 押下時刻を記録
        };

        inputActions.Player.Attack.canceled += ctx =>
        {
            //ボタンを離した。押下継続時間で通常攻撃/スキルを分岐
            attackHeld = false;
            double held = (attackPressStartTime < 0.0) ? 0.0 : (Time.timeAsDouble - attackPressStartTime);
            attackPressStartTime = -1.0;

            // 死亡/落下など上位優先状態では無視
            if (_fsm.CurrentState == PlayerState.Dead) return;

            if (held >= skillHoldThreshold)
            {
                //長押し→スキル入力トリガー
                WeaponInstance wp = GetMainWeapon();
                //if(wp!=null)_fsm.ExecuteTrigger(PlayerTrigger.SkillInput);
            }
            else
            {
                attackPressedThisFrame = true;
                _fsm.ExecuteTrigger(PlayerTrigger.AttackInput);
            }
        };
        inputActions.Player.SwitchWeapon.performed += ctx => OnSwitchWeaponInput();
        inputActions.Player.SwitchWeapon2.performed += ctx => OnSwitchWeaponInput2();
        inputActions.Player.SwitchHitbox.performed += ctx => { isHitboxVisible = !isHitboxVisible; };
        inputActions.Player.Dash.performed += ctx => OnDashInput();

        // --- UIEvents の購読：装備切替/破壊/耐久 ---
        UIEvents.OnRightWeaponSwitch += HandleRightWeaponSwitch;   // (weapons, from, to)

        PlayerEvents.OnAimTargetChanged += (newTarget) =>
        {
            lockOnTarget = newTarget;
        };

    }

    private void OnDisable()
    {
        // 入力解除
        inputActions?.Disable();

        // 購読解除
        UIEvents.OnRightWeaponSwitch -= HandleRightWeaponSwitch;

        if (PlayerEvents.GetPlayerObject != null)
        {
            PlayerEvents.GetPlayerObject = null;
        }


    }

    private void Start()
    {
        mainCam = Camera.main.transform;

        // --- PlayableGraph 初期化 ---
        playableGraph = PlayableGraph.Create("PlayerGraph");

        // 日本語：ルートモーションは制御コード側で扱うため無効化
        playerAnimator.applyRootMotion = false;

        // 日本語：各クリップのラップモード（落下は最終フレーム保持）
        idleClip.wrapMode = WrapMode.Loop;
        moveClip.wrapMode = WrapMode.Loop;
        fallClip.wrapMode = WrapMode.ClampForever;    // ★ 変更：Loop→ClampForever
        hitClip.wrapMode = WrapMode.ClampForever;
        dieClip.wrapMode = WrapMode.ClampForever;
        dashClip.wrapMode = WrapMode.ClampForever;

        idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
        movePlayable = AnimationClipPlayable.Create(playableGraph, moveClip);
        fallPlayable = AnimationClipPlayable.Create(playableGraph, fallClip);
        hitPlayable = AnimationClipPlayable.Create(playableGraph, hitClip);
        deadPlayable = AnimationClipPlayable.Create(playableGraph, dieClip);
        dashPlayable = AnimationClipPlayable.Create(playableGraph, dashClip);

        // 日本語：メインミキサーは6入力（Idle/Move/Action/Falling/Hit/Dead）
        mixer = AnimationMixerPlayable.Create(playableGraph, 7);

        // 0:Idle, 1:Move
        mixer.ConnectInput((int)MainLayerSlot.Idle, idlePlayable, 0, 1f);
        mixer.ConnectInput((int)MainLayerSlot.Move, movePlayable, 0, 0f);

        // 2:Action（初期はダミーを接続。攻撃/スキル時に子ミキサーへ差し替え）
        actionSubMixer = AnimationMixerPlayable.Create(playableGraph, 2);
        actionSubMixer.SetInputCount(2);
        mixer.ConnectInput((int)MainLayerSlot.Action, actionSubMixer, 0, 0f);

        // 3:Fall, 4:Hit, 5:Dead
        mixer.ConnectInput((int)MainLayerSlot.Falling, fallPlayable, 0, 0f);
        mixer.ConnectInput((int)MainLayerSlot.Hit, hitPlayable, 0, 0f);
        mixer.ConnectInput((int)MainLayerSlot.Dead, deadPlayable, 0, 0f);
        mixer.ConnectInput((int)MainLayerSlot.Dash, dashPlayable, 0, 0f);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);
        playableOutput.SetSourcePlayable(mixer);
        playableGraph.Play();

        // --- FSM 初期化 ---
        SetupStateMachine();

        // --- ライフ初期化 ---
        currentHealth = maxHealth;
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }

    private void Update()
    {
        // 接地判定
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // 地上では垂直速度を小さな負値に固定し、接地安定を確保
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -4f; // ★ これにより微小な押し付けで浮遊を防止
        }

        // 離地検出 → Fallingへ（Dead/Falling を除外）
        if (!isGrounded && _fsm.CurrentState != PlayerState.Dead && _fsm.CurrentState != PlayerState.Falling)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.NoGround);
        }

        if (isGrounded && _fsm.CurrentState == PlayerState.Falling)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.Grounded);
        }

        // 地面かつ Idle/Move 時は移動入力で確実に遷移を駆動（保険）
        if (isGrounded && (_fsm.CurrentState == PlayerState.Idle || _fsm.CurrentState == PlayerState.Move))
        {
            if (moveInput.sqrMagnitude > 0.01f)
                _fsm.ExecuteTrigger(PlayerTrigger.MoveStart);
            else
                _fsm.ExecuteTrigger(PlayerTrigger.MoveStop);
        }

        // 重力/移動（重力は常時加算）
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 無敵時間更新
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer < 0f) dashTimer = 0f;
        }
        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer < 0f) hitTimer = 0f;
        }

        // ステート更新
        bool pressedNow = attackPressedThisFrame;
        _fsm.Update(Time.deltaTime);
        attackPressedThisFrame = false && pressedNow;
    }

    // ====== FSM 構築 ======
    private void SetupStateMachine()
    {
        _fsm = new StateMachine<PlayerState, PlayerTrigger>(this, PlayerState.Idle);

        _fsm.RegisterState(PlayerState.Idle, new PlayerIdleState(this));
        _fsm.RegisterState(PlayerState.Move, new PlayerMoveState(this));
        _fsm.RegisterState(PlayerState.Attack, new PlayerAttackState(this));
        _fsm.RegisterState(PlayerState.Skill, new PlayerSkillState(this));
        _fsm.RegisterState(PlayerState.Falling, new PlayerFallingState(this));
        _fsm.RegisterState(PlayerState.Hit, new PlayerHitState(this));
        _fsm.RegisterState(PlayerState.Dead, new PlayerDeadState(this));
        _fsm.RegisterState(PlayerState.Dash, new PlayerDashState(this));

        // Locomotion
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Move, PlayerTrigger.MoveStart);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Idle, PlayerTrigger.MoveStop);

        // Attack / Skill
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Attack, PlayerTrigger.AttackInput);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Attack, PlayerTrigger.AttackInput);
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Skill, PlayerTrigger.SkillInput);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Skill, PlayerTrigger.SkillInput);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Idle, PlayerTrigger.MoveStop);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Move, PlayerTrigger.MoveStart);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Skill, PlayerTrigger.SkillInput);

        // Falling（Dead 以外 → Falling）
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Skill, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Hit, PlayerState.Falling, PlayerTrigger.NoGround);

        // Falling → Idle
        _fsm.AddTransition(PlayerState.Falling, PlayerState.Idle, PlayerTrigger.Grounded);

        // Hit（どの状態からでも被弾可能。無敵時はTakeDamage側で弾く）
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Skill, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Falling, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Dash, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Hit, PlayerState.Idle, PlayerTrigger.MoveStop);
        _fsm.AddTransition(PlayerState.Hit, PlayerState.Move, PlayerTrigger.MoveStart);

        // Dash（任意の状態からダッシュ可。実処理はDashステート）
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Dash, PlayerTrigger.DashInput);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Dash, PlayerTrigger.DashInput);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Dash, PlayerTrigger.DashInput);
        _fsm.AddTransition(PlayerState.Skill, PlayerState.Dash, PlayerTrigger.DashInput);
        _fsm.AddTransition(PlayerState.Dash, PlayerState.Idle, PlayerTrigger.MoveStop);
        _fsm.AddTransition(PlayerState.Dash, PlayerState.Move, PlayerTrigger.MoveStart);

        // Dead（★唯一：Hit → Dead）
        _fsm.AddTransition(PlayerState.Hit, PlayerState.Dead, PlayerTrigger.Die);
    }

    // ====== 入力ハンドラ ======
    public void HandleMovement(float deltaTime)
    {
        float inputX = moveInput.x;
        float inputZ = moveInput.y;

        Vector3 camForward = mainCam.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = mainCam.right; camRight.y = 0f; camRight.Normalize();

        Vector3 moveDir = camRight * inputX + camForward * inputZ;
        moveDir = (moveDir.sqrMagnitude > 1e-4f) ? moveDir.normalized : Vector3.zero;

        if (moveDir.sqrMagnitude > 0f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * deltaTime);
            controller.Move(moveDir * moveSpeed * deltaTime);
        }
    }
    public void CheckMoveInput()
    {
        // 旧 Input 系を残している場合の互換チェック（必要なら削除）
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.MoveStart);
        }
    }
    public void CheckMoveStop()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(inputX) < 0.1f && Mathf.Abs(inputZ) < 0.1f)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.MoveStop);
        }
    }
    private void OnSwitchWeaponInput()
    {
        // 右手優先の切替。攻撃中は不可
        if (_fsm.CurrentState == PlayerState.Attack)
        {
            Debug.Log("Cannot switch weapon while attacking!");
            return;
        }
        // --- 直接インベントリに命じる。装備・モデル同期はイベントで反映 ---
        if (!weaponInventory.TrySwitchRight())
        {
            Debug.Log("No usable weapon for right hand.");
        }
    }
    private void OnSwitchWeaponInput2()
    {
        // 攻撃中は不可
        if (_fsm.CurrentState == PlayerState.Attack)
        {
            Debug.Log("Cannot switch weapon while attacking!");
            return;
        }
        // --- 直接インベントリに命じる。装備・モデル同期はイベントで反映 ---
        if (!weaponInventory.TrySwitchRight(-1))
        {
            Debug.Log("No usable weapon for left hand.");
        }
    }
    private void OnDashInput()
    {
        if(IsDashCooldownReady()) _fsm.ExecuteTrigger(PlayerTrigger.DashInput);
    }

    // ====== モデル同期（イベントドリブン） ======



    // 右手の切替イベント
    private void HandleRightWeaponSwitch(List<WeaponInstance> list, int from, int to)
    {
        // ・from->to の変化を受けて、右手の武器モデルを入れ替える
        if (from == to) return; // 変化なし
        ApplyHandModel(HandType.Main, list, to);
    }

    // 指定手の武器モデルを更新
    private void ApplyHandModel(HandType hand, List<WeaponInstance> list, int toIndex)
    {
        // ・現行の子を全削除し、toIndex が有効なら新しいプレハブをインスタンス化する
        GameObject box = (hand == HandType.Main) ? weaponBoxR : weaponBoxL;

        // 既存子オブジェクト破棄
        for (int i = box.transform.childCount - 1; i >= 0; --i)
        {
            Transform c = box.transform.GetChild(i);
            if (c) Destroy(c.gameObject);
        }

        if (toIndex < 0 || toIndex >= list.Count) return;
        var inst = list[toIndex];
        if (inst == null || inst.template == null || inst.template.modelPrefab == null) return;

        // 新規インスタンス化
        GameObject newWeapon = Instantiate(inst.template.modelPrefab, box.transform);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        newWeapon.transform.localScale = Vector3.one;
    }

    // ====== ユーティリティ ======

    public void HandleFalling()
    {
        _fsm.ExecuteTrigger(PlayerTrigger.NoGround);
    }

    public WeaponInstance GetMainWeapon()
    {
        return weaponInventory.GetWeapon(HandType.Main);
    }

    public void ToIdle()
    {
        _fsm.ExecuteTrigger(PlayerTrigger.MoveStop);
    }


    // === 物拾い ===
    public void PickUpWeapon(WeaponItem weapon)
    {
        if (weapon == null) return;
        weaponInventory.AddWeapon(weapon);
        Debug.Log("PickUp: " + weapon.weaponName);

        // 初回装備の自動化（右手が空なら右手に装備 etc）
        if (weaponInventory.GetWeapon(HandType.Main) == null)
        {
            weaponInventory.TrySwitchRight(); // イベントで右手モデルが生成される
        }
        else if (weaponInventory.GetWeapon(HandType.Sub) == null)
        {
            weaponInventory.TrySwitchLeft(); // イベントで左手モデルが生成される
        }
    }

    // === ダメージ・回復 ===
    public void TakeDamage(DamageData damage)
    {
        if (damage.damageAmount <= 0) return;
        if (_fsm.CurrentState == PlayerState.Dead) return; // 死亡後は無視

        // 無敵中は被弾を無効化
        if (IsInvincible) return;

        currentHealth = Mathf.Max(0, currentHealth - damage.damageAmount);
        Debug.Log($"Player took {damage.damageAmount} damage. Current HP: {currentHealth}/{maxHealth}");

        // 死亡判定は被弾モーションの終盤で行う（ここでは即Deadにしない）
        if (_fsm.CurrentState != PlayerState.Hit)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.GetHit);
            hitTimer = hitInvincibilityTime; // 被弾無敵時間リセット
            // 被弾エフェクト
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
            }
        }
    }

    // ====== アニメーション制御 ======
    // メインレイヤーの指定スロットへクロスフェード（他の入力は0へ）
    public void BlendToMainSlot(MainLayerSlot target, float duration)
    {
        if (mainLayerFadeCo != null) StopCoroutine(mainLayerFadeCo);
        mainLayerFadeCo = StartCoroutine(CoBlendToMainSlot(target, duration));
    }

    private System.Collections.IEnumerator CoBlendToMainSlot(MainLayerSlot target, float duration)
    {
        int inputCount = mixer.GetInputCount();
        if (duration <= 0f)
        {
            for (int i = 0; i < inputCount; ++i)
                mixer.SetInputWeight(i, (i == (int)target) ? 1f : 0f);
            yield break;
        }

        // 現在重みのスナップショット
        float[] start = new float[inputCount];
        for (int i = 0; i < inputCount; ++i) start[i] = mixer.GetInputWeight(i);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);
            for (int i = 0; i < inputCount; ++i)
            {
                float dst = (i == (int)target) ? 1f : 0f;
                mixer.SetInputWeight(i, Mathf.Lerp(start[i], dst, w));
            }
            yield return null;
        }
        for (int i = 0; i < inputCount; ++i)
            mixer.SetInputWeight(i, (i == (int)target) ? 1f : 0f);
    }
    // 
    private MainLayerSlot MapStateToSlot(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Idle: return MainLayerSlot.Idle;
            case PlayerState.Move: return MainLayerSlot.Move;
            case PlayerState.Attack: return MainLayerSlot.Action; // 子ミキサー
            case PlayerState.Skill: return MainLayerSlot.Action; // 子ミキサー
            case PlayerState.Falling: return MainLayerSlot.Falling;
            case PlayerState.Hit: return MainLayerSlot.Hit;
            case PlayerState.Dead: return MainLayerSlot.Dead;
            case PlayerState.Dash: return MainLayerSlot.Dash;
            default: return MainLayerSlot.Idle;
        }
    }

    // ブレンド時間の解決（1.個別from to > 2.to既定 > 3.全体既定）
    public float ResolveBlendDuration(PlayerState from, PlayerState to)
    {

        if (to == PlayerState.Hit || to == PlayerState.Dash)
            return Mathf.Max(0f, interruptCrossfade);
        // 1) 個別オーバーライド検索
        for (int i = 0; i < blendOverrides.Count; ++i)
        {
            var e = blendOverrides[i];
            if (e != null && e.from == from && e.to == to) return Mathf.Max(0f, e.duration);
        }
        // 2) to状態の既定
        for (int i = 0; i < toStateDefaults.Count; ++i)
        {
            var d = toStateDefaults[i];
            if (d != null && d.to == to) return Mathf.Max(0f, d.duration);
        }
        // 3) 全体既定
        return Mathf.Max(0f, defaultStateCrossfade);
    }

    // 論理状態へブレンド（呼び出し側はto状態だけ渡せばよい）
    public void BlendToState(PlayerState toState)
    {
        var slot = MapStateToSlot(toState);
        float dur = ResolveBlendDuration(lastBlendState, toState);

        // 既存のメイン層フェードAPIを使用
        BlendToMainSlot(slot, dur);

        // 次回のために記録
        lastBlendState = toState;
    }

    public AnimationMixerPlayable GetActionSubMixer() => actionSubMixer;
    public void EvaluateGraphOnce() { if (playableGraph.IsValid()) playableGraph.Evaluate(0f); }
    public bool HasMoveInput() => moveInput.sqrMagnitude > 0.01f;
    public void ExecuteTriggerExternal(PlayerTrigger t)
    {
        _fsm.ExecuteTrigger(t);
    }
    // Hit クリップの Playable を 0 秒から再生し直す
    public void ResetHitClipPlayable()
    {
        if (hitPlayable.IsValid())
        {
            hitPlayable.SetTime(0.0);
            hitPlayable.SetSpeed(1.0);
            hitPlayable.Play();
            EvaluateGraphOnce(); // 0秒戻しの見た目を即反映
        }
    }

    // Dead クリップの Playable を 0 秒から再生し直す
    public void ResetDeadClipPlayable()
    {
        if (deadPlayable.IsValid())
        {
            deadPlayable.SetTime(0.0);
            deadPlayable.SetSpeed(1.0);
            deadPlayable.Play();
            EvaluateGraphOnce(); // 即座に初期フレームへ
        }
    }

    // 日本語：ダッシュ用のアニメを 0 秒から再生し直す
    public void ResetDashClipPlayable()
    {
        if (dashPlayable.IsValid())
        {
            dashPlayable.SetTime(0.0);
            dashPlayable.SetSpeed(1.0);
            dashPlayable.Play();
            EvaluateGraphOnce();
        }
    }

    // 日本語：ダッシュの無敵を開始（ステートから呼ぶ）
    public void StartDashInvincibility()
    {
        // タイマー方式：Update で減衰させる
        dashTimer = dashInvincibilityTime;
    }

    // 日本語：ダッシュの無敵を終了（ステートから呼ぶ）
    public void EndDashInvincibility()
    {
        dashTimer = 0f;
    }

    // 日本語：ダッシュのクールタイム記録（終了時に呼ぶ）
    public void MarkDashCooldown()
    {
        dashCooldownTimer = Time.time;
        Invoke(nameof(EnableDashUI), dashCooldown);
    }
    private void EnableDashUI()
    {
        UIEvents.OnDashUIChange?.Invoke(true);
    }

    // 日本語：ダッシュ可能か（入力段階のガードに使用）
    public bool IsDashCooldownReady()
    {
        return Time.time >= (dashCooldownTimer + dashCooldown);
    }

    // 日本語：カメラ相対の移動入力方向。小入力なら正面を返す
    public Vector3 ResolveDashDirectionWorld(float minInputSqr = 0.01f)
    {
        Vector3 dir;
        if (this.moveInput.sqrMagnitude >= minInputSqr)
        {
            Vector3 camF = mainCam.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = mainCam.right; camR.y = 0f; camR.Normalize();
            dir = (camR * moveInput.x + camF * moveInput.y);
            if (dir.sqrMagnitude < 1e-6f) dir = transform.forward; else dir.Normalize();
        }
        else
        {
            dir = transform.forward;
        }
        return dir;
    }
    // Dash クリップの長さを返す（未設定時はフェイルセーフ）
    public double GetDashClipLength()
    {
        return (dashClip != null) ? (double)dashClip.length : 0.2;
    }

    // Dash クリップの現在時刻
    public double GetDashPlayableTime()
    {
        return dashPlayable.IsValid() ? dashPlayable.GetTime() : 0.0;
    }

    // Dash クリップの時刻と速度の設定
    public void SetDashPlayableTime(double t) { if (dashPlayable.IsValid()) { dashPlayable.SetTime(t); } }
    public void SetDashPlayableSpeed(double s) { if (dashPlayable.IsValid()) { dashPlayable.SetSpeed(s); } }


}

