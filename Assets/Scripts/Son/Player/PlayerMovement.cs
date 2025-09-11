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
    Attack,      // �U��
    Skill,       // �X�L��
    Dash,        // �_�b�V��
    Dead,        // ���S
    Falling      // ����
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
    // ====== ���͌n ======
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    [HideInInspector] public bool attackHeld;                // �������ςȂ��t���O
    [HideInInspector] public bool attackPressedThisFrame;    // ���̃t���[���ɉ������ꂽ��

    [Header("�U�����́i����������j")]
    [Tooltip("���̕b���ȏ�Ȃ�X�L���Ƃ��Ĉ���")]
    public float skillHoldThreshold = 0.35f;   // ��F0.35�b
    private double attackPressStartTime = -1.0; // ���������iTime.timeAsDouble�j

    [Header("�X�e�[�^�X")]
    public float maxHealth = 50;
    private float currentHealth = 50;


    // ====== �ړ��ݒ� ======
    [Header("�ړ��ݒ�")]
    public float moveSpeed = 5f;           // �ړ����x
    public float gravity = -9.81f;         // �d�͉����x
    public float rotationSpeed = 360f;     // ��]���x�i�x/�b�j

    [Header("�_�b�V���ݒ�")]
    public float dashSpeed = 10f;           // �_�b�V�����x
    public float dashDistance = 3f;       // �_�b�V������


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
    public bool isHitboxVisible = true; // �q�b�g�{�b�N�X����

    // ����C���x���g��
    public PlayerWeaponInventory weaponInventory = new PlayerWeaponInventory();

    [Header("�v���C���[���f��")]
    public GameObject playerModel;
    public Animator playerAnimator;

    // ====== �A�j���[�V�����iPlayableGraph�j======
    [Header("�A�j���[�V�����N���b�v")]
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

    // ���{��F���C�����C���[�p�̒ǉ�Playable
    private AnimationClipPlayable fallPlayable;
    private AnimationClipPlayable hitPlayable;
    private AnimationClipPlayable deadPlayable;
    // ���{��F�A�N�V�����X���b�g�̃_�~�[�i���ڑ����̌����߁j
    private AnimationClipPlayable actionPlaceholder;

    // ���{��F���C�����C���[�̃t�F�[�h�p�R���[�`��
    private Coroutine mainLayerFadeCo;

    // ���{��F�������Ɏ��S���m�肵���t���O�i���n�Ŏ��S�ցj
    private bool pendingDie;

    // ================== ��Ԋԃu�����h�ݒ� ==================
    // ��(from��to)�̃u�����h���Ԃ��㏑������G���g��
    // ���C�����C���[�̃X���b�g�ԍ��iMixer���͂̌Œ芄�蓖�āj
    public enum MainLayerSlot
    {
        Idle = 0, // �A�C�h��
        Move = 1, // �ړ�
        Action = 2, // �U��/�X�L��
        Falling = 3, // ����
        Hit = 4, // ��e
        Dead = 5  // ���S
    }
    [System.Serializable]
    public class StateBlendEntry
    {
        public PlayerState from;                 // ��FMove
        public PlayerState to;                   // ��FHit
        [Min(0f)] public float duration = 0.12f; // ��F0.06f�i��e�͑����j
    }

    //�uto��ԁv�P�ʂ̃f�t�H���g�u�����h�ifrom����v���̃t�H�[���o�b�N�j
    [System.Serializable]
    public class StateDefaultBlend
    {
        public PlayerState to;                   // ��FHit
        [Min(0f)] public float duration = 0.12f; // ��F0.06f
    }

    [Header("��Ԋԃu�����h�i�ρj")]
    [Tooltip("�S�̂̋K��l�i�ʐݒ��to���肪������Ȃ��ꍇ�̃t�H�[���o�b�N�j")]
    public float defaultStateCrossfade = 0.12f;

    [Tooltip("�����from��to�ŏ㏑���������u�����h���ԁi�C�ӌ��j")]
    public List<StateBlendEntry> blendOverrides = new List<StateBlendEntry>();

    [Tooltip("to��Ԃ��Ƃ̊���u�����h�ifrom����v���Ɏg�p�j")]
    public List<StateDefaultBlend> toStateDefaults = new List<StateDefaultBlend>();

    // ���{��F���߂̘_����ԁi�u�����h�v�Z�p�j
    public PlayerState lastBlendState = PlayerState.Idle;

    [Header("�T�E���h")]
    public PlayerAudioManager audioManager;

    // ====== ������� ======


    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
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

        // �U���L�[�F�������͎������L�^�A�������u�ԂɒZ����/�������𕪊򂷂�
        inputActions.Player.Attack.performed += ctx =>
        {
            //�������ϊJ�n�B�����ł͔������Ȃ��i�m��͗������u�ԁj
            attackHeld = true;
            attackPressedThisFrame = false;                  // ���̃t���[���ł̐�s��t�͂��Ȃ�
            attackPressStartTime = Time.timeAsDouble;        // �����������L�^
        };

        inputActions.Player.Attack.canceled += ctx =>
        {
            //�{�^���𗣂����B�����p�����ԂŒʏ�U��/�X�L���𕪊�
            attackHeld = false;
            double held = (attackPressStartTime < 0.0) ? 0.0 : (Time.timeAsDouble - attackPressStartTime);
            attackPressStartTime = -1.0;

            // ���S/�����ȂǏ�ʗD���Ԃł͖���
            if (_fsm.CurrentState == PlayerState.Dead) return;

            if (held >= skillHoldThreshold)
            {
                //���������X�L�����̓g���K�[
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

        // --- UIEvents �̍w�ǁF�����ؑ�/�j��/�ϋv ---
        UIEvents.OnRightWeaponSwitch += HandleRightWeaponSwitch;   // (weapons, from, to)


        // --- PlayerEvents �̍w�� ---
        PlayerEvents.OnWeaponBroke += PlayrWeaponBrokeEffect; // (handType)


    }

    private void OnDisable()
    {
        // ���͉���
        inputActions?.Disable();

        // �w�ǉ���
        UIEvents.OnRightWeaponSwitch -= HandleRightWeaponSwitch;

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

        // ���{��F���[�g���[�V�����͐���R�[�h���ň������ߖ�����
        playerAnimator.applyRootMotion = false;

        // ���{��F�e�N���b�v�̃��b�v���[�h�i�����͍ŏI�t���[���ێ��j
        idleClip.wrapMode = WrapMode.Loop;
        moveClip.wrapMode = WrapMode.Loop;
        fallClip.wrapMode = WrapMode.ClampForever;    // �� �ύX�FLoop��ClampForever
        hitClip.wrapMode = WrapMode.ClampForever;
        dieClip.wrapMode = WrapMode.ClampForever;

        idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
        movePlayable = AnimationClipPlayable.Create(playableGraph, moveClip);
        fallPlayable = AnimationClipPlayable.Create(playableGraph, fallClip);
        hitPlayable = AnimationClipPlayable.Create(playableGraph, hitClip);
        deadPlayable = AnimationClipPlayable.Create(playableGraph, dieClip);

        // ���{��F���C���~�L�T�[��6���́iIdle/Move/Action/Falling/Hit/Dead�j
        mixer = AnimationMixerPlayable.Create(playableGraph, 6);

        // 0:Idle, 1:Move
        mixer.ConnectInput((int)MainLayerSlot.Idle, idlePlayable, 0, 1f);
        mixer.ConnectInput((int)MainLayerSlot.Move, movePlayable, 0, 0f);

        // 2:Action�i�����̓_�~�[��ڑ��B�U��/�X�L�����Ɏq�~�L�T�[�֍����ւ��j
        actionSubMixer = AnimationMixerPlayable.Create(playableGraph, 2);
        actionSubMixer.SetInputCount(2);
        mixer.ConnectInput((int)MainLayerSlot.Action, actionSubMixer, 0, 0f);

        // 3:Fall, 4:Hit, 5:Dead
        mixer.ConnectInput((int)MainLayerSlot.Falling, fallPlayable, 0, 0f);
        mixer.ConnectInput((int)MainLayerSlot.Hit, hitPlayable, 0, 0f);
        mixer.ConnectInput((int)MainLayerSlot.Dead, deadPlayable, 0, 0f);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);
        playableOutput.SetSourcePlayable(mixer);
        playableGraph.Play();

        // --- FSM ������ ---
        SetupStateMachine();

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
        // �ڒn����
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // ���{��F�n��ł͐������x�������ȕ��l�ɌŒ肵�A�ڒn������m��
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -4f; // �� ����ɂ������ȉ����t���ŕ��V��h�~
        }

        // ���n���o �� Falling�ցiDead/Falling �����O�j
        if (!isGrounded && _fsm.CurrentState != PlayerState.Dead && _fsm.CurrentState != PlayerState.Falling)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.NoGround);
        }

        if (isGrounded && _fsm.CurrentState == PlayerState.Falling)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.Grounded);
            if (pendingDie) { pendingDie = false; _fsm.ExecuteTrigger(PlayerTrigger.Die); }
        }

        // �n�ʂ��� Idle/Move ���͈ړ����͂Ŋm���ɑJ�ڂ��쓮�i�ی��j
        if (isGrounded && (_fsm.CurrentState == PlayerState.Idle || _fsm.CurrentState == PlayerState.Move))
        {
            if (moveInput.sqrMagnitude > 0.01f)
                _fsm.ExecuteTrigger(PlayerTrigger.MoveStart);
            else
                _fsm.ExecuteTrigger(PlayerTrigger.MoveStop);
        }

        // �d��/�ړ��i�d�͂͏펞���Z�j
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // �X�e�[�g�X�V
        bool pressedNow = attackPressedThisFrame;
        _fsm.Update(Time.deltaTime);
        attackPressedThisFrame = false && pressedNow;
    }

    // ====== FSM �\�z ======
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
        // �U�����ɒ������ŃX�L���ڍs�������݌v�Ȃ�ȉ����ێ�
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Skill, PlayerTrigger.SkillInput);

        // Falling�iDead�ȊO�̑S��ԁ�Falling�j
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Skill, PlayerState.Falling, PlayerTrigger.NoGround);
        _fsm.AddTransition(PlayerState.Hit, PlayerState.Falling, PlayerTrigger.NoGround);

        // Falling �� Idle
        _fsm.AddTransition(PlayerState.Falling, PlayerState.Idle, PlayerTrigger.Grounded);

        // Hit�i�n��̂݁FFalling/Dead�ȊO��Hit�j
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Hit, PlayerTrigger.GetHit);
        _fsm.AddTransition(PlayerState.Skill, PlayerState.Hit, PlayerTrigger.GetHit);

        // Dead�i�ǂ�����ł� �� Dead�j
        _fsm.AddTransition(PlayerState.Idle, PlayerState.Dead, PlayerTrigger.Die);
        _fsm.AddTransition(PlayerState.Move, PlayerState.Dead, PlayerTrigger.Die);
        _fsm.AddTransition(PlayerState.Attack, PlayerState.Dead, PlayerTrigger.Die);
        _fsm.AddTransition(PlayerState.Skill, PlayerState.Dead, PlayerTrigger.Die);
        _fsm.AddTransition(PlayerState.Falling, PlayerState.Dead, PlayerTrigger.Die);
        _fsm.AddTransition(PlayerState.Hit, PlayerState.Dead, PlayerTrigger.Die);
    }

    // ====== ���̓n���h�� ======
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
        if (wpBrokeEffect != null)
        {
            GameObject box = (handType == HandType.Main) ? weaponBoxR : weaponBoxL;
            Instantiate(wpBrokeEffect, box.transform.position, Quaternion.identity);
        }
    }

    // �E��̐ؑփC�x���g
    private void HandleRightWeaponSwitch(List<WeaponInstance> list, int from, int to)
    {
        // �Efrom->to �̕ω����󂯂āA�E��̕��탂�f�������ւ���
        if (from == to) return; // �ω��Ȃ�
        ApplyHandModel(HandType.Main, list, to);
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


    // === ���E�� ===
    public void PickUpWeapon(WeaponItem weapon)
    {
        if (weapon == null) return;
        weaponInventory.AddWeapon(weapon);
        Debug.Log("PickUp: " + weapon.weaponName);

        // ���񑕔��̎������i�E�肪��Ȃ�E��ɑ��� etc�j
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
    public void TakeDamage(DamageData damage)
    {
        if (damage.damageAmount <= 0) return;
        if (_fsm.CurrentState == PlayerState.Dead) return; // ���S��͖���

        currentHealth = Mathf.Max(0, currentHealth - damage.damageAmount);
        Debug.Log($"Player took {damage.damageAmount} damage. Current HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            // �󒆂Ȃ璅�n��Ɏ��S�ցA�n��Ȃ瑦���S��
            if (!isGrounded)
            {
                pendingDie = true;
                _fsm.ExecuteTrigger(PlayerTrigger.NoGround);
            }
            else
            {
                _fsm.ExecuteTrigger(PlayerTrigger.Die);
            }
            return;
        }

        // �n��ł���Δ�e��D��
        if (isGrounded)
        {
            _fsm.ExecuteTrigger(PlayerTrigger.GetHit);
        }
        // �󒆔�e��Falling�ŏ����i���S�̂ݒx���t���O�j
    }

    // ====== �A�j���[�V�������� ======
    // ���C�����C���[�̎w��X���b�g�փN���X�t�F�[�h�i���̓��͂�0�ցj
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

        // ���ݏd�݂̃X�i�b�v�V���b�g
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
            case PlayerState.Attack: return MainLayerSlot.Action; // �q�~�L�T�[
            case PlayerState.Skill: return MainLayerSlot.Action; // �q�~�L�T�[
            case PlayerState.Falling: return MainLayerSlot.Falling;
            case PlayerState.Hit: return MainLayerSlot.Hit;
            case PlayerState.Dead: return MainLayerSlot.Dead;
            default: return MainLayerSlot.Idle;
        }
    }

    // ���{��F�u�����h���Ԃ̉����i1.��from to > 2.to���� > 3.�S�̊���j
    public float ResolveBlendDuration(PlayerState from, PlayerState to)
    {
        // 1) �ʃI�[�o�[���C�h����
        for (int i = 0; i < blendOverrides.Count; ++i)
        {
            var e = blendOverrides[i];
            if (e != null && e.from == from && e.to == to) return Mathf.Max(0f, e.duration);
        }
        // 2) to��Ԃ̊���
        for (int i = 0; i < toStateDefaults.Count; ++i)
        {
            var d = toStateDefaults[i];
            if (d != null && d.to == to) return Mathf.Max(0f, d.duration);
        }
        // 3) �S�̊���
        return Mathf.Max(0f, defaultStateCrossfade);
    }

    // ���{��F�_����Ԃփu�����h�i�Ăяo������to��Ԃ����n���΂悢�j
    public void BlendToState(PlayerState toState)
    {
        var slot = MapStateToSlot(toState);
        float dur = ResolveBlendDuration(lastBlendState, toState);

        // �����̃��C���w�t�F�[�hAPI���g�p
        BlendToMainSlot(slot, dur);

        // ����̂��߂ɋL�^
        lastBlendState = toState;
    }

    public AnimationMixerPlayable GetActionSubMixer() => actionSubMixer;
    public void EvaluateGraphOnce() { if (playableGraph.IsValid()) playableGraph.Evaluate(0f); }
    public bool HasMoveInput() => moveInput.sqrMagnitude > 0.01f;
}

