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
    public int mainIndex = -1; // ���݂̃��C������C���f�b�N�X
    public int subIndex = -1; // ���݂̃T�u����C���f�b�N�X
    public enum HandType
    {
        Main, // ���C������
        Sub   // �T�u����
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
        return null; // ���킪�Ȃ��ꍇ
    }

    public int SwitchWeapon(HandType handType)
    {
        if (weapons.Count == 0)
        {
            Debug.LogWarning("No weapons available to switch.");
            return 0; // ���킪�Ȃ��ꍇ�͉������Ȃ�
        }
        if (handType == HandType.Main)
        {
            mainIndex = (mainIndex + 1) % weapons.Count; // ���C�������؂�ւ�
            return 1;
        }
        else if (handType == HandType.Sub)
        {
            subIndex = (subIndex + 1) % weapons.Count; // �T�u�����؂�ւ�
            return -1;
        }
        else
        {
            Debug.LogWarning("Invalid hand type specified for weapon switch.");
            return 0; // �����ȃn���h�^�C�v�̏ꍇ�͉������Ȃ�
        }
    }
    public void UnequipWeapon(HandType handType)
    {
        if (handType == HandType.Main)
        {
            mainIndex = -1; // ���C��������O��
        }
        else if (handType == HandType.Sub)
        {
            subIndex = -1; // �T�u������O��
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
            // weapons ����폜
            weapons.RemoveAt(index);

            if (weapons.Count == 0)
            {
                mainIndex = -1; // �S�Ă̕��킪�폜���ꂽ�ꍇ�A���C������̃C���f�b�N�X�����Z�b�g
                subIndex = -1; // �T�u����̃C���f�b�N�X�����Z�b�g
                return;
            }

            if (mainIndex == index)
            {
                mainIndex %= weapons.Count; // ���C������̃C���f�b�N�X�����Z�b�g
            }
            else if (main != null)
            {
                mainIndex = GetIndex(main); // ���C������̃C���f�b�N�X���Đݒ�
            }

            if (subIndex == index)
            {
                subIndex %= weapons.Count;
            }
            else if (sub != null)
            {
                subIndex = GetIndex(sub); // �T�u����̃C���f�b�N�X���Đݒ�
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
    public TempUI tempUI;

    [Header("����֘A")]
    public GameObject weaponBoxR;             // �E�蕐��̐e�I�u�W�F�N�g
    public GameObject weaponBoxL;             // ���蕐��̐e�I�u�W�F�N�g
    public WeaponInstance fist;               // �f��
    private PlayerWeaponInventory weaponInventory; // �v���C���[�̕���C���x���g��

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
        tempUI = FindObjectsByType<TempUI>(FindObjectsSortMode.None)[0]; // �V�[�����̍ŏ���TempUI���擾

        controller = GetComponent<CharacterController>();
        mainCam = Camera.main.transform; // ���C���J�������擾
        weaponInventory = new PlayerWeaponInventory(); // ����C���x���g���̏�����
        SwitchWeapon(PlayerWeaponInventory.HandType.Main); // ���������ݒ�


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

    void SwitchWeapon(PlayerWeaponInventory.HandType hand)
    {
        if (weaponInventory.SwitchWeapon(hand) == 0)
        {
            tempUI?.UpdateWeapon(null); // ���킪�Ȃ��ꍇ��UI���X�V
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
            EquipWeapon(PlayerWeaponInventory.HandType.Main); // ���C��������đ���
            //EquipWeapon(PlayerWeaponInventory.HandType.Sub); // �T�u������đ���
        }
    }
    public void EquipWeapon(PlayerWeaponInventory.HandType hand)
    {
        WeaponInstance weapon = weaponInventory.GetWeapon(hand);
        if (weapon == null)
        {
            Debug.LogWarning("No weapon equipped for hand: " + hand);
            tempUI?.UpdateWeapon(null); // ���킪�Ȃ��ꍇ��UI���X�V
            return; // ���킪�Ȃ��ꍇ�͉������Ȃ�
        }
        if (weaponBoxR.transform.childCount > 0)
        {
            Transform oldWeapon = weaponBoxR.transform.GetChild(0);
            if (oldWeapon != null)
            {
                Destroy(oldWeapon.gameObject); // �Â�������폜
            }
        }
        GameObject weaponBox = hand == PlayerWeaponInventory.HandType.Main ? weaponBoxR : weaponBoxL;
        GameObject newWeapon = Instantiate(weapon.template.modelPrefab, weaponBox.transform);
        newWeapon.transform.localPosition = Vector3.zero; // ����̈ʒu�����Z�b�g
        newWeapon.transform.localRotation = Quaternion.identity; // ����̉�]�����Z�b�g
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
                WeaponBroke(weapon); // ���킪��ꂽ�ꍇ�̏���
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