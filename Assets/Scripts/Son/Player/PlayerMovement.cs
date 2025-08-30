using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

[System.Serializable]
public class WeaponInstance
{
    public WeaponItem template;
    public int currentDurability;

    public WeaponInstance(WeaponItem weapon)
    {
        template = weapon;
        currentDurability = weapon.maxDurability;
    }

    public void Use(float cost)
    {
        currentDurability -= Mathf.CeilToInt(cost);
        currentDurability = Mathf.Max(0, currentDurability);
    }

    public bool IsBroken => currentDurability <= 0;
}
[System.Serializable]
public class PlayerWeaponInventory
{
    public List<WeaponInstance> weapons = new List<WeaponInstance>();
    public int mainIndex = -1; // 現在のメイン武器インデックス
    public int subIndex = -1; // 現在のサブ武器インデックス
    public enum HandType
    {
        Main, // メイン武器
        Sub   // サブ武器
    }
    public void AddWeapon(WeaponItem weapon)
    {
        if (weapon != null)
        {
            weapons.Add(new WeaponInstance(weapon));
        }
    }
    public void RemoveWeapon(int index)
    {
        if (index >= 0 && index < weapons.Count)
        {
            weapons.RemoveAt(index);
        }
    }
    public WeaponInstance GetWeapon(HandType handType)
    {
        if (handType == HandType.Main && mainIndex >= 0 && mainIndex < weapons.Count)
        {
            return weapons[mainIndex];
        }
        else if (handType == HandType.Sub && subIndex >= 0 && subIndex < weapons.Count)
        {
            return weapons[subIndex];
        }
        return null; // 武器がない場合
    }

    public int SwitchWeapon(HandType handType)
    {
        if (weapons.Count == 0)
        {
            Debug.LogWarning("No weapons available to switch.");
            return 0; // 武器がない場合は何もしない
        }
        if (handType == HandType.Main)
        {
            mainIndex = (mainIndex + 1) % weapons.Count; // メイン武器を切り替え
            return 1;
        }
        else if (handType == HandType.Sub)
        {
            subIndex = (subIndex + 1) % weapons.Count; // サブ武器を切り替え
            return -1;
        }
        else
        {
            Debug.LogWarning("Invalid hand type specified for weapon switch.");
            return 0; // 無効なハンドタイプの場合は何もしない
        }
    }
    public void UnequipWeapon(HandType handType)
    {
        if (handType == HandType.Main)
        {
            mainIndex = -1; // メイン武器を外す
        }
        else if (handType == HandType.Sub)
        {
            subIndex = -1; // サブ武器を外す
        }
    }
    public void DestroyWeapon(WeaponInstance target)
    {
        int index = GetIndex(target);
        if (index >= 0)
        {
            WeaponInstance main = null;
            WeaponInstance sub = null;
            if (mainIndex != -1 && mainIndex < weapons.Count) main = weapons[mainIndex];
            if (subIndex != -1 && subIndex < weapons.Count) sub = weapons[subIndex];
            // weapons から削除
            weapons.RemoveAt(index);

            if (weapons.Count == 0)
            {
                mainIndex = -1; // 全ての武器が削除された場合、メイン武器のインデックスをリセット
                subIndex = -1; // サブ武器のインデックスもリセット
                return;
            }

            if (mainIndex == index)
            {
                mainIndex %= weapons.Count; // メイン武器のインデックスをリセット
            }
            else if (main != null)
            {
                mainIndex = GetIndex(main); // メイン武器のインデックスを再設定
            }

            if (subIndex == index)
            {
                subIndex %= weapons.Count;
            }
            else if (sub != null)
            {
                subIndex = GetIndex(sub); // サブ武器のインデックスを再設定
            }
        }
    }
    private int GetIndex(WeaponInstance weapon)
    {
        return weapons.IndexOf(weapon);
    }
}




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
    public TempUI tempUI;

    [Header("武器関連")]
    public GameObject weaponBoxR;             // 右手武器の親オブジェクト
    public GameObject weaponBoxL;             // 左手武器の親オブジェクト
    public WeaponInstance fist;               // 素手
    private PlayerWeaponInventory weaponInventory; // プレイヤーの武器インベントリ

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
        tempUI = FindObjectsByType<TempUI>(FindObjectsSortMode.None)[0]; // シーン内の最初のTempUIを取得

        controller = GetComponent<CharacterController>();
        mainCam = Camera.main.transform; // メインカメラを取得
        weaponInventory = new PlayerWeaponInventory(); // 武器インベントリの初期化
        SwitchWeapon(PlayerWeaponInventory.HandType.Main); // 初期武器を設定


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

    void SwitchWeapon(PlayerWeaponInventory.HandType hand)
    {
        if (weaponInventory.SwitchWeapon(hand) == 0)
        {
            tempUI?.UpdateWeapon(null); // 武器がない場合はUIを更新
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
            EquipWeapon(PlayerWeaponInventory.HandType.Main); // メイン武器を再装備
            //EquipWeapon(PlayerWeaponInventory.HandType.Sub); // サブ武器を再装備
        }
    }
    public void EquipWeapon(PlayerWeaponInventory.HandType hand)
    {
        WeaponInstance weapon = weaponInventory.GetWeapon(hand);
        if (weapon == null)
        {
            Debug.LogWarning("No weapon equipped for hand: " + hand);
            tempUI?.UpdateWeapon(null); // 武器がない場合はUIを更新
            return; // 武器がない場合は何もしない
        }
        if (weaponBoxR.transform.childCount > 0)
        {
            Transform oldWeapon = weaponBoxR.transform.GetChild(0);
            if (oldWeapon != null)
            {
                Destroy(oldWeapon.gameObject); // 古い武器を削除
            }
        }
        GameObject weaponBox = hand == PlayerWeaponInventory.HandType.Main ? weaponBoxR : weaponBoxL;
        GameObject newWeapon = Instantiate(weapon.template.modelPrefab, weaponBox.transform);
        newWeapon.transform.localPosition = Vector3.zero; // 武器の位置をリセット
        newWeapon.transform.localRotation = Quaternion.identity; // 武器の回転をリセット
        tempUI?.UpdateWeapon(weapon);
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
        return weaponInventory.GetWeapon(PlayerWeaponInventory.HandType.Main);
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
        WeaponInstance weapon = weaponInventory.GetWeapon(PlayerWeaponInventory.HandType.Main);
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
            SwitchWeapon(PlayerWeaponInventory.HandType.Main);
        }
        else
        {
            Debug.Log("Cannot switch weapon while attacking!");
        }
    }
}