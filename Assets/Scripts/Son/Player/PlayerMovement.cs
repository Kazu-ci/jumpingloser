using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using static EventBus;

public enum PlayerState
{
    Idle,        // �ҋ@
    Move,        // ����
    Hit,         // ��e
    Pickup,      // �s�b�N�A�b�v
    SwitchWeapon,// ����؂�ւ�
    Attack       // �U��
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
    // ====== ���͌n ======
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    [Header("�X�e�[�^�X")]
    public int maxHealth = 50;
    private int currentHealth = 50;


    // ====== �ړ��ݒ� ======
    [Header("�ړ��ݒ�")]
    public float moveSpeed = 5f;           // �ړ����x
    public float gravity = -9.81f;         // �d�͉����x
    public float rotationSpeed = 360f;     // ��]���x�i�x/�b�j

    // ====== �ڒn���� ======
    [Header("�ڒn����")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // ====== ����E���f�� ======
    [Header("����֘A")]
    public GameObject weaponBoxR;          // �E��̕���e
    public GameObject weaponBoxL;          // ����̕���e
    public WeaponInstance fist;            // �f��
    public GameObject wpBrokeEffect;      // ����j��G�t�F�N�g

    // ����C���x���g��
    public PlayerWeaponInventory weaponInventory = new PlayerWeaponInventory();

    [Header("�v���C���[���f��")]
    public GameObject playerModel;
    public Animator playerAnimator;

    // ====== �A�j���[�V�����iPlayableGraph�j======
    [Header("�A�j���[�V�����N���b�v")]
    public AnimationClip idleClip;
    public AnimationClip moveClip;
    public PlayableGraph playableGraph;
    public AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable movePlayable;

    [Header("�T�E���h")]
    public PlayerAudioManager audioManager;

    // ====== ������� ======
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCam;
    private StateMachine<PlayerState, PlayerTrigger> _fsm;

    // ====== ���C�t�T�C�N�� ======
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        PlayerEvents.GetPlayerObject = () => this.gameObject; // �v���C���[�I�u�W�F�N�g�̒�
    }

    private void OnEnable()
    {
        // --- ���͍w�� ---
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Attack.performed += ctx => OnAttackInput();
        inputActions.Player.SwitchWeapon.performed += ctx => OnSwitchWeaponInput();
        inputActions.Player.SwitchWeapon2.performed += ctx => OnSwitchWeaponInput2();

        // --- UIEvents �̍w�ǁF�����ؑ�/�j��/�ϋv ---
        UIEvents.OnRightWeaponSwitch += HandleRightWeaponSwitch;   // (weapons, from, to)
        UIEvents.OnLeftWeaponSwitch += HandleLeftWeaponSwitch;    // (weapons, from, to)
        UIEvents.OnWeaponDestroyed += HandleWeaponDestroyed;     // (removedIndex, item)

        // --- PlayerEvents �̍w�� ---
        PlayerEvents.OnWeaponBroke += PlayrWeaponBrokeEffect; // (handType)


    }

    private void OnDisable()
    {
        // ���͉���
        inputActions?.Disable();

        // �w�ǉ���
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

        // --- PlayableGraph ������ ---
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

        // --- FSM ������ ---
        SetupStateMachine();

        // --- ���������i�K�v�Ȃ�E�聨����̏��Łj---
        // weaponInventory.AddWeapon(startWeaponItem);
        // weaponInventory.TrySwitchRight();

        // --- ���C�t������ ---
        currentHealth = maxHealth;
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }

    private void Update()
    {
        // �ڒn
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        // �X�e�[�g�X�V
        _fsm.Update(Time.deltaTime);

        // �d��
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ====== FSM �\�z ======
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

    // ====== ���̓n���h�� ======
    private void OnAttackInput()
    {
        var weapon = weaponInventory.GetWeapon(HandType.Main);
        _fsm.ExecuteTrigger(PlayerTrigger.AttackInput);

    }

    private void OnSwitchWeaponInput()
    {
        // �E��D��̐ؑցB�U�����͕s��
        if (_fsm.CurrentState == PlayerState.Attack)
        {
            Debug.Log("Cannot switch weapon while attacking!");
            return;
        }
        // --- ���ڃC���x���g���ɖ�����B�����E���f�������̓C�x���g�Ŕ��f ---
        if (!weaponInventory.TrySwitchRight())
        {
            Debug.Log("No usable weapon for right hand.");
        }
    }
    private void OnSwitchWeaponInput2()
    {
        // �U�����͕s��
        if (_fsm.CurrentState == PlayerState.Attack)
        {
            Debug.Log("Cannot switch weapon while attacking!");
            return;
        }
        // --- ���ڃC���x���g���ɖ�����B�����E���f�������̓C�x���g�Ŕ��f ---
        if (!weaponInventory.TrySwitchLeft())
        {
            Debug.Log("No usable weapon for left hand.");
        }
    }

    // ====== ���f�������i�C�x���g�h���u���j ======

    private void PlayrWeaponBrokeEffect(HandType handType)
    {
        if(wpBrokeEffect != null)
        {
            GameObject box = (handType == HandType.Main) ? weaponBoxR : weaponBoxL;
            Instantiate(wpBrokeEffect, box.transform.position, Quaternion.identity);
        }
    }

    // �E��̐ؑփC�x���g
    private void HandleRightWeaponSwitch(List<WeaponInstance> list, int from, int to)
    {
        // �Efrom->to �̕ω����󂯂āA�E��̕��탂�f�������ւ���
        if(from == to) return; // �ω��Ȃ�
        ApplyHandModel(HandType.Main, list, to);
    }

    // ����̐ؑփC�x���g
    private void HandleLeftWeaponSwitch(List<WeaponInstance> list, int from, int to)
    {
        if (from == to) return; // �ω��Ȃ�
        ApplyHandModel(HandType.Sub, list, to);
    }

    // �j��C�x���g�iUI�����j�B���f���͐ؑփC�x���g�Ŕ��f����邽�߁A�����̓��O���x��OK
    private void HandleWeaponDestroyed(int removedIndex, WeaponItem item)
    {
        // �EremovedIndex �͍폜�O�̔ԍ�
        // �E�茳�Ɏ����Ă���ꍇ�́ARemoveAtAndRecover ���� SetHandIndex ������A
        //   ���ʂƂ��� OnRightWeaponSwitch / OnLeftWeaponSwitch �����ł���̂ŁA
        //   �����ڂ̓����͂�����Ŏ��{�����
        Debug.Log($"Weapon destroyed @index={removedIndex} ({item?.weaponName})");
    }

    // �w���̕��탂�f�����X�V
    private void ApplyHandModel(HandType hand, List<WeaponInstance> list, int toIndex)
    {
        // �E���s�̎q��S�폜���AtoIndex ���L���Ȃ�V�����v���n�u���C���X�^���X������
        GameObject box = (hand == HandType.Main) ? weaponBoxR : weaponBoxL;

        // �����q�I�u�W�F�N�g�j��
        for (int i = box.transform.childCount - 1; i >= 0; --i)
        {
            Transform c = box.transform.GetChild(i);
            if (c) Destroy(c.gameObject);
        }

        if (toIndex < 0 || toIndex >= list.Count) return;
        var inst = list[toIndex];
        if (inst == null || inst.template == null || inst.template.modelPrefab == null) return;

        // �V�K�C���X�^���X��
        GameObject newWeapon = Instantiate(inst.template.modelPrefab, box.transform);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        newWeapon.transform.localScale = Vector3.one;
    }

    // ====== ���[�e�B���e�B ======
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
        // �� Input �n���c���Ă���ꍇ�̌݊��`�F�b�N�i�K�v�Ȃ�폜�j
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

    // === ���E���i����͒P���ɏ����֒ǉ��j===
    public void PickUpWeapon(WeaponItem weapon)
    {
        if (weapon == null) return;
        weaponInventory.AddWeapon(weapon);
        Debug.Log("PickUp: " + weapon.weaponName);

        // ��F���񑕔��̎������i�E�肪��Ȃ�E��ɑ��� etc�j
        if (weaponInventory.GetWeapon(HandType.Main) == null)
        {
            weaponInventory.TrySwitchRight(); // �C�x���g�ŉE�胂�f�������������
        }
        else if (weaponInventory.GetWeapon(HandType.Sub) == null)
        {
            weaponInventory.TrySwitchLeft(); // �C�x���g�ō��胂�f�������������
        }
    }

    // === �_���[�W�E�� ===
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
    Idle,// �ҋ@
    Move,// ����
    Hit,// ��e
    Pickup,// �s�b�N�A�b�v
    SwitchWeapon,// ����؂�ւ�
    Attack //�U��
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

    [Header("�ړ��ݒ�")]
    public float moveSpeed = 5f;             // �ړ����x
    //public float jumpHeight = 2f;            // �W�����v�̍����i���g�p�j
    public float gravity = -9.81f;           // �d�͉����x
    public float rotationSpeed = 360f;       // ��]���x�i�x�^�b�j

    [Header("�ڒn����")]
    public Transform groundCheck;            // �n�ʔ���̂��߂�Transform
    public float groundDistance = 0.4f;      // �ڒn�`�F�b�N�̔��a
    public LayerMask groundMask;             // �n�ʂƂ��Ĕ��肳��郌�C���[

    [Header("����֘A")]
    public GameObject weaponBoxR;             // �E�蕐��̐e�I�u�W�F�N�g
    public GameObject weaponBoxL;             // ���蕐��̐e�I�u�W�F�N�g
    public WeaponInstance fist;               // �f��
    private PlayerWeaponInventory weaponInventory = new PlayerWeaponInventory(); // �v���C���[�̕���C���x���g��

    [Header("�v���C���[���f��")]
    public GameObject playerModel;           // �v���C���[���f����GameObject
    public Animator playerAnimator;       // �v���C���[�̃A�j���[�V�����R���|�[�l���g

    [Header("�A�j���[�V�����N���b�v")]
    public AnimationClip idleClip;           // �ҋ@�A�j���[�V����
    public AnimationClip moveClip;
    public PlayableGraph playableGraph;
    public AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable movePlayable;

    [Header("�T�E���h")]    
    public PlayerAudioManager audioManager;
    

    private CharacterController controller;  // CharacterController�̎Q��
    private Vector3 velocity;                // ���������̑��x
    private bool isGrounded;                 // �ڒn���Ă��邩�ǂ���
    private Transform mainCam;               // ���C���J������Transform

    // �v���C���[�̏��
    private StateMachine<PlayerState, PlayerTrigger> _fsm;

    void Start()
    {
        

        controller = GetComponent<CharacterController>();
        mainCam = Camera.main.transform; // ���C���J�������擾
        SwitchWeapon(HandType.Main); // ���������ݒ�


        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Attack.performed += ctx => OnAttackInput();
        inputActions.Player.SwitchWeapon.performed += ctx => OnSwitchWeaponInput();



        // PlayableGraph ������
        playableGraph = PlayableGraph.Create("PlayerGraph");

        idleClip.wrapMode = WrapMode.Loop; // �ҋ@�A�j���[�V���������[�v������
        moveClip.wrapMode = WrapMode.Loop; // �ړ��A�j���[�V���������[�v������

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

        //�@�d��
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
            //UIEvents.OnRightWeaponSwitch?.Invoke(weaponInventory.weapons,-1,-1); // ���킪�Ȃ��ꍇ��UI���X�V
            return; // ���s�̏ꍇ�͉������Ȃ�
        }
        EquipWeapon(hand); // ����𑕔�
    }
    private void WeaponBroke(WeaponInstance weapon)
    {
        if (weapon != null && weapon.IsBroken)
        {
            Debug.LogWarning("Weapon is broken: " + weapon.template.weaponName);
            weaponInventory.DestroyWeapon(weapon); // �C���x���g�����畐����폜
            EquipWeapon(HandType.Main); // ���C��������đ���
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
                Destroy(oldWeapon.gameObject); // �Â�������폜
            }
        }
        WeaponInstance weapon = weaponInventory.GetWeapon(hand);
        if (weapon == null)
        {
            Debug.Log("No weapon equipped for hand: " + hand);
            return; // ���킪�Ȃ��ꍇ�͉������Ȃ�
        }
        
        GameObject newWeapon = Instantiate(weapon.template.modelPrefab, weaponBox.transform);
        newWeapon.transform.localPosition = Vector3.zero; // ����̈ʒu�����Z�b�g
        newWeapon.transform.localRotation = Quaternion.identity; // ����̉�]�����Z�b�g
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
                WeaponBroke(weapon); // ���킪��ꂽ�ꍇ�̏���
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