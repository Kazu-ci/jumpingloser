using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;  
    public Vector2 rotationSpeed = new Vector2(400f, 300f); // �J������]���x
    public Vector2 pitchClamp = new Vector2(-30f, 60f);    // �����p�x�̐���

    public float defaultDistance = 10f; // �J��������
    public float minDistance = 1f;     // �ŏ�����
    public float smoothSpeed = 10f;    // �J�����̒Ǐ]�X���[�Y��
    public LayerMask collisionMask;    // �J�������Ԃ���Ώۂ̃��C���[

    private float yaw;    // �����p�x
    private float pitch;  // �����p�x
    private Vector3 currentVelocity;   // �X���[�Y�ړ��p�̑��x�x�N�g��
    private Transform cam;             // ���ۂ̃J����

    [Header("�f�o�C�X�ʊ��x�{��")]
    public float mouseSensitivityMultiplier = 0.3f;
    public float gamepadSensitivityMultiplier = 1.0f;

    private InputSystem_Actions inputActions;
    private Vector2 lookInput;

    void Start()
    {
        cam = Camera.main.transform;

        // �����p�x�ݒ�
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        // �}�E�X����ʂɌŒ�
        Cursor.lockState = CursorLockMode.Locked;

        // ���̓V�X�e��������
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    void LateUpdate()
    {
        // ���͂ŃJ������]
        float sensitivityMultiplier = IsGamepadUsed() ? gamepadSensitivityMultiplier : mouseSensitivityMultiplier;

        yaw += lookInput.x * rotationSpeed.x * Time.deltaTime * sensitivityMultiplier;
        pitch -= lookInput.y * rotationSpeed.y * Time.deltaTime * sensitivityMultiplier;
        pitch = Mathf.Clamp(pitch, pitchClamp.x, pitchClamp.y);


        
        transform.position = target.position;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f); // �J����������ݒ�

        
        Vector3 desiredCameraPos = transform.position - transform.forward * defaultDistance;

        // ��Q���`�F�b�N
        RaycastHit hit;
        float actualDistance = defaultDistance;
        if (Physics.Raycast(target.position, -transform.forward, out hit, defaultDistance, collisionMask))
        {
            // ��Q��������ꍇ�́A�������k�߂�
            actualDistance = Mathf.Clamp(hit.distance-0.25f, minDistance, defaultDistance);
        }

        // ���ۂ̃J�����ʒu���X�V
        Vector3 finalPos = transform.position - transform.forward * actualDistance;
        cam.position = Vector3.SmoothDamp(cam.position, finalPos, ref currentVelocity, 1f / smoothSpeed);

        
        cam.LookAt(target.position + Vector3.up * 0.5f);
    }
    private bool IsGamepadUsed()
    {
        return Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
    }
}