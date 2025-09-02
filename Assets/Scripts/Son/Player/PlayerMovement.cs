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
    Attack       // 攻撃
}

public enum PlayerTrigger
{
    MoveStart,
    MoveStop,
    GetHit,
    StartPickup,
    EndPickup,
    SwitchWeapon,
    AttackInput
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ====== 入力系 ======
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    [Header("ステータス")]
    public int maxHealth = 50;
    private int currentHealth = 50;


    // ====== 移動設定 ======
    [Header("移動設定")]
    public float moveSpeed = 5f;           // 移動速度
    public float gravity = -9.81f;         // 重力加速度
    public float rotationSpeed = 360f;     // 回転速度（度/秒）

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

    // 武器インベントリ
    public PlayerWeaponInventory weaponInventory = new PlayerWeaponInventory();

    [Header("プレイヤーモデル")]
    public GameObject playerModel;
    public Animator playerAnimator;

    // ====== アニメーション（PlayableGraph）======
    [Header("アニメーションクリップ")]
    public AnimationClip idleClip;
    public AnimationClip moveClip;
    public PlayableGraph playableGraph;
    public AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable movePlayable;

    [Header("サウンド")]
    public PlayerAudioManager audioManager;

    // ====== 内部状態 ======
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCam;
    private StateMachine<PlayerState, PlayerTrigger> _fsm;

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

        inputActions.Player.Attack.performed += ctx => OnAttackInput();
        inputActions.Player.SwitchWeapon.performed += ctx => OnSwitchWeaponInput();
        inputActions.Player.SwitchWeapon2.performed += ctx => OnSwitchWeaponInput2();

        // --- UIEvents の購読：装備切替/破壊/耐久 ---
        UIEvents.OnRightWeaponSwitch += HandleRightWeaponSwitch;   // (weapons, from, to)
        UIEvents.OnLeftWeaponSwitch += HandleLeftWeaponSwitch;    // (weapons, from, to)
        UIEvents.OnWeaponDestroyed += HandleWeaponDestroyed;     // (removedIndex, item)

        // --- PlayerEvents の購読 ---
        PlayerEvents.OnWeaponBroke += PlayrWeaponBrokeEffect; // (handType)


    }

    private void OnDisable()
    {
        // 入力解除
        inputActions?.Disable();

        // 購読解除
        UIEvents.OnRightWeaponSwitch -= HandleRightWeaponSwitch;
        UIEvents.OnLeftWeaponSwitch -= HandleLeftWeaponSwitch;
        UIEvents.OnWeaponDestroyed -= HandleWeaponDestroyed;

        PlayerEvents.OnWeaponBroke -= PlayrWeaponBrokeEffect;

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
        idleClip.wrapMode = WrapMode.Loop;
        moveClip.wrapMode = WrapMode.Loop;

        idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
        movePlayable = AnimationClipPlayable.Create(playableGraph, moveClip);

        mixer = AnimationMixerPlayable.Create(playableGraph, 2);
        mixer.ConnectInput(0, idlePlayable, 0, 1f);
        mixer.ConnectInput(1, movePlayable, 0, 0f);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);
        playableOutput.SetSourcePlayable(mixer);
        playableGraph.Play();

        // --- FSM 初期化 ---
        SetupStateMachine();

        // --- 初期装備（必要なら右手→左手の順で）---
        // weaponInventory.AddWeapon(startWeaponItem);
        // weaponInventory.TrySwitchRight();

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
        // 接地
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        // ステート更新
        _fsm.Update(Time.deltaTime);

        // 重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ====== FSM 構築 ======
    private void SetupStateMachine()
    {
        _fsm = new StateMachine<PlayerState, PlayerTrigger>(this, PlayerState.Idle);

        _fsm.RegisterState(PlayerState.Idle, new PlayerIdleState(this));
        _fsm.RegisterState(PlayerState.Move, new PlayerMoveState(this));
        _fsm.RegisterState(PlayerState.Attack, new PlayerAttackState(this));

        _fsm.AddTransition(PlayerState.Idle, PlayerState.Move, PlayerTrigger.MoveStart);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Idle, PlayerTrigger.MoveStop);

        _fsm.AddTransition(PlayerState.Idle, PlayerState.Attack, PlayerTrigger.AttackInput);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Attack, PlayerTrigger.AttackInput);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Idle, PlayerTrigger.MoveStop);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Move, PlayerTrigger.MoveStart);
    }

    // ====== 入力ハンドラ ======
    private void OnAttackInput()
    {
        var weapon = weaponInventory.GetWeapon(HandType.Main);
        _fsm.ExecuteTrigger(PlayerTrigger.AttackInput);

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
        if (!weaponInventory.TrySwitchLeft())
        {
            Debug.Log("No usable weapon for left hand.");
        }
    }

    // ====== モデル同期（イベントドリブン） ======

    private void PlayrWeaponBrokeEffect(HandType handType)
    {
        if(wpBrokeEffect != null)
        {
            GameObject box = (handType == HandType.Main) ? weaponBoxR : weaponBoxL;
            Instantiate(wpBrokeEffect, box.transform.position, Quaternion.identity);
        }
    }

    // 右手の切替イベント
    private void HandleRightWeaponSwitch(List<WeaponInstance> list, int from, int to)
    {
        // ・from->to の変化を受けて、右手の武器モデルを入れ替える
        if(from == to) return; // 変化なし
        ApplyHandModel(HandType.Main, list, to);
    }

    // 左手の切替イベント
    private void HandleLeftWeaponSwitch(List<WeaponInstance> list, int from, int to)
    {
        if (from == to) return; // 変化なし
        ApplyHandModel(HandType.Sub, list, to);
    }

    // 破壊イベント（UI向け）。モデルは切替イベントで反映されるため、ここはログ程度でOK
    private void HandleWeaponDestroyed(int removedIndex, WeaponItem item)
    {
        // ・removedIndex は削除前の番号
        // ・手元に持っている場合は、RemoveAtAndRecover 内で SetHandIndex が走り、
        //   結果として OnRightWeaponSwitch / OnLeftWeaponSwitch が飛んでくるので、
        //   見た目の同期はそちらで実施される
        Debug.Log($"Weapon destroyed @index={removedIndex} ({item?.weaponName})");
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

    public void EnsureMixerInputCount(int requiredCount)
    {
        if (mixer.GetInputCount() < requiredCount)
        {
            mixer.SetInputCount(requiredCount);
        }
    }

    public WeaponInstance GetMainWeapon()
    {
        return weaponInventory.GetWeapon(HandType.Main);
    }

    public void ToIdle()
    {
        _fsm.ExecuteTrigger(PlayerTrigger.MoveStop);
    }

    // === 物拾い（現状は単純に所持へ追加）===
    public void PickUpWeapon(WeaponItem weapon)
    {
        if (weapon == null) return;
        weaponInventory.AddWeapon(weapon);
        Debug.Log("PickUp: " + weapon.weaponName);

        // 例：初回装備の自動化（右手が空なら右手に装備 etc）
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
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}/{maxHealth}");
        //audioManager?.PlayHurtSound();
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Debug.Log("Player has died.");
    }
}









/*using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.Animations;
//using static EventBus;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.XR;


public enum PlayerState
{
    Idle,// 待機
    Move,// 歩き
    Hit,// 被弾
    Pickup,// ピックアップ
    SwitchWeapon,// 武器切り替え
    Attack //攻撃
}

public enum PlayerTrigger
{
    MoveStart, 
    MoveStop,
    GetHit,
    StartPickup,
    EndPickup,
    SwitchWeapon,
    AttackInput
}


[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    [Header("移動設定")]
    public float moveSpeed = 5f;             // 移動速度
    //public float jumpHeight = 2f;            // ジャンプの高さ（未使用）
    public float gravity = -9.81f;           // 重力加速度
    public float rotationSpeed = 360f;       // 回転速度（度／秒）

    [Header("接地判定")]
    public Transform groundCheck;            // 地面判定のためのTransform
    public float groundDistance = 0.4f;      // 接地チェックの半径
    public LayerMask groundMask;             // 地面として判定されるレイヤー

    [Header("武器関連")]
    public GameObject weaponBoxR;             // 右手武器の親オブジェクト
    public GameObject weaponBoxL;             // 左手武器の親オブジェクト
    public WeaponInstance fist;               // 素手
    private PlayerWeaponInventory weaponInventory = new PlayerWeaponInventory(); // プレイヤーの武器インベントリ

    [Header("プレイヤーモデル")]
    public GameObject playerModel;           // プレイヤーモデルのGameObject
    public Animator playerAnimator;       // プレイヤーのアニメーションコンポーネント

    [Header("アニメーションクリップ")]
    public AnimationClip idleClip;           // 待機アニメーション
    public AnimationClip moveClip;
    public PlayableGraph playableGraph;
    public AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable movePlayable;

    [Header("サウンド")]    
    public PlayerAudioManager audioManager;
    

    private CharacterController controller;  // CharacterControllerの参照
    private Vector3 velocity;                // 垂直方向の速度
    private bool isGrounded;                 // 接地しているかどうか
    private Transform mainCam;               // メインカメラのTransform

    // プレイヤーの状態
    private StateMachine<PlayerState, PlayerTrigger> _fsm;

    void Start()
    {
        

        controller = GetComponent<CharacterController>();
        mainCam = Camera.main.transform; // メインカメラを取得
        SwitchWeapon(HandType.Main); // 初期武器を設定


        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Attack.performed += ctx => OnAttackInput();
        inputActions.Player.SwitchWeapon.performed += ctx => OnSwitchWeaponInput();



        // PlayableGraph 初期化
        playableGraph = PlayableGraph.Create("PlayerGraph");

        idleClip.wrapMode = WrapMode.Loop; // 待機アニメーションをループさせる
        moveClip.wrapMode = WrapMode.Loop; // 移動アニメーションをループさせる

        idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
        movePlayable = AnimationClipPlayable.Create(playableGraph, moveClip);
       
        // Mixer 2 inputs
        mixer = AnimationMixerPlayable.Create(playableGraph, 2);

        mixer.ConnectInput(0, idlePlayable, 0, 1f);
        mixer.ConnectInput(1, movePlayable, 0, 0f);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);
        playableOutput.SetSourcePlayable(mixer);

        playableGraph.Play();
        SetupStateMachine();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        _fsm.Update(Time.deltaTime);

        //　重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    void SwitchWeapon(HandType hand)
    {
        int from = weaponInventory.mainIndex;
        if (weaponInventory.SwitchRightWeapon() == -1)
        {
            //UIEvents.OnRightWeaponSwitch?.Invoke(weaponInventory.weapons,-1,-1); // 武器がない場合はUIを更新
            return; // 失敗の場合は何もしない
        }
        EquipWeapon(hand); // 武器を装備
    }
    private void WeaponBroke(WeaponInstance weapon)
    {
        if (weapon != null && weapon.IsBroken)
        {
            Debug.LogWarning("Weapon is broken: " + weapon.template.weaponName);
            weaponInventory.DestroyWeapon(weapon); // インベントリから武器を削除
            EquipWeapon(HandType.Main); // メイン武器を再装備
        }
    }
    public void EquipWeapon(HandType hand)
    {
        GameObject weaponBox = hand == HandType.Main ? weaponBoxR : weaponBoxL;
        while (weaponBox.transform.childCount > 0)
        {
            Transform oldWeapon = weaponBox.transform.GetChild(0);
            if (oldWeapon != null)
            {
                Destroy(oldWeapon.gameObject); // 古い武器を削除
            }
        }
        WeaponInstance weapon = weaponInventory.GetWeapon(hand);
        if (weapon == null)
        {
            Debug.Log("No weapon equipped for hand: " + hand);
            return; // 武器がない場合は何もしない
        }
        
        GameObject newWeapon = Instantiate(weapon.template.modelPrefab, weaponBox.transform);
        newWeapon.transform.localPosition = Vector3.zero; // 武器の位置をリセット
        newWeapon.transform.localRotation = Quaternion.identity; // 武器の回転をリセット
        //tempUI?.UpdateWeapon(weapon);
    }
    public void PickUpWeapon(WeaponItem weapon)
    {
        if (weapon!= null)
        {
            weaponInventory.AddWeapon(weapon);
            Debug.Log("PickUp:" + weapon.weaponName);
        }
    }
    private void OnDestroy()
    {
        playableGraph.Destroy();
    }
    public void CheckMoveInput()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.MoveStart);
        }
    }
    private void SetupStateMachine()
    {
        _fsm = new StateMachine<PlayerState, PlayerTrigger>(this, PlayerState.Idle);

        _fsm.RegisterState(PlayerState.Idle, new PlayerIdleState(this));
        _fsm.RegisterState(PlayerState.Move, new PlayerMoveState(this));
        _fsm.RegisterState(PlayerState.Attack, new PlayerAttackState(this));

        _fsm.AddTransition(PlayerState.Idle, PlayerState.Move, PlayerTrigger.MoveStart);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Idle, PlayerTrigger.MoveStop);

        _fsm.AddTransition(PlayerState.Idle, PlayerState.Attack, PlayerTrigger.AttackInput);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Attack, PlayerTrigger.AttackInput);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Idle, PlayerTrigger.MoveStop);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Move, PlayerTrigger.MoveStart);
    }
    public void HandleMovement(float deltaTime)
    {
        float inputX = moveInput.x;
        float inputZ = moveInput.y;

        Vector3 camForward = mainCam.forward;
        Vector3 camRight = mainCam.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camRight * inputX + camForward * inputZ;
        moveDir.Normalize();

        if (moveDir.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);

            controller.Move(moveDir * moveSpeed * deltaTime);
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
    public WeaponInstance GetMainWeapon()
    {
        return weaponInventory.GetWeapon(HandType.Main);
    }
    public void ToIdle()
    {
        _fsm.ExecuteTrigger(PlayerTrigger.MoveStop);
    }
    public void EnsureMixerInputCount(int requiredCount)
    {
        if (mixer.GetInputCount() < requiredCount)
        {
            mixer.SetInputCount(requiredCount);
        }
    }

    private void OnAttackInput()
    {
        WeaponInstance weapon = weaponInventory.GetWeapon(HandType.Main);
        if (weapon != null)
        {
            Debug.Log("Attacking with: " + weapon.template.weaponName + "Durability now: " + weapon.currentDurability);
            _fsm.ExecuteTrigger(PlayerTrigger.AttackInput);

            if (weapon.IsBroken)
            {
                WeaponBroke(weapon); // 武器が壊れた場合の処理
            }
        }
    }

    private void OnSwitchWeaponInput()
    {
        if (_fsm.CurrentState != PlayerState.Attack)
        {
            SwitchWeapon(HandType.Main);
        }
        else
        {
            Debug.Log("Cannot switch weapon while attacking!");
        }
    }
}
*/