using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using static EventBus;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("=== 追従ターゲット ===")]
    public Transform target;      // プレイヤーの通常時ピボット
    public Transform aimTarget;   // ロックオン時の肩越しピボット

    [Header("=== カメラ回転と距離 ===")]
    public Vector2 rotationSpeed = new Vector2(400f, 300f); // カメラ回転速度
    public Vector2 pitchClamp = new Vector2(-30f, 60f);     // 垂直角度の制限
    public float defaultDistance = 6.5f;                    // 通常距離
    public float minDistance = 1f;                          // 最小距離
    public LayerMask collisionMask;                         // カメラ衝突レイヤー

    [Header("=== デバイス別感度倍率 ===")]
    public float mouseSensitivityMultiplier = 0.3f;
    public float gamepadSensitivityMultiplier = 1.0f;

    [Header("=== ロックオン設定 ===")]
    public LayerMask enemyLayer;        // 敵レイヤー
    public float maxLockDistance = 25f; // ロック対象の最大距離
    [Range(10f, 120f)]
    public float lockFovAngle = 60f;    // 正面視野角
    public float switchInputThreshold = 0.3f; // 入力でターゲット切替の閾値
    public float switchCooldown = 0.35f;      // 連続切替のクールダウン

    [Header("=== 追従スピード (通常とロック別) ===")]
    public float pivotLerpSpeed_Normal = 8f;  // 通常時ピボット補間
    public float pivotLerpSpeed_Locked = 3f;  // ロック時ピボット補間
    public float smoothSpeed_Normal = 8f;     // 通常時カメラ位置補間
    public float smoothSpeed_Locked = 3f;     // ロック時カメラ位置補間

    [Header("=== ロックオン回転補間 ===")]
    public float lockSwitchRotateSpeed = 6f;  // 切替直後の回転補間速度

    [Header("=== ヒエラルキー対策 ===")]
    public bool autoDetachFromPlayer = true;  // プレイヤー配下なら自動で外す

    [Header("=== 切替時の移動ブレンド ===")]
    public float switchMoveBlendTime = 0.15f; // ターゲット切替時の位置ブレンド時間

    // 内部
    private float yaw;
    private float pitch;
    private Transform cam;
    private Vector3 currentVelocity;     // カメラ位置用 SmoothDamp 速度

    private InputSystem_Actions inputActions;
    private Vector2 lookInput;
    private bool lockPressedThisFrame;

    private bool isLocked;
    private Transform lockedEnemy;
    private float switchTimer;
    private bool needsSmoothRotate;
    private Quaternion smoothLockRotation;

    // ピボット補間
    private Vector3 followPivot;
    private Vector3 pivotVelocity;

    // 切替時の位置ブレンド
    private bool switchMoveActive;
    private float switchMoveRemain;
    private float switchMoveTotal;
    private Vector3 camPosOnSwitch;

    void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;

        // 初期角度
        var angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;

        // 入力
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
        inputActions.Player.Lock.performed += _ => lockPressedThisFrame = true;

        // 初期ピボット
        followPivot = target != null ? target.position : transform.position;

        // プレイヤー配下から外す
        if (autoDetachFromPlayer && target != null)
        {
            Transform root = target.root;
            if (root != null && transform.IsChildOf(root))
            {
                transform.SetParent(null, true); // ワールド座標を維持
            }
        }
    }

    private void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (cam == null || target == null) return;

        float dt = Time.deltaTime;
        switchTimer -= dt;

        bool gamepad = IsGamepadUsed();
        float sensMul = gamepad ? gamepadSensitivityMultiplier : mouseSensitivityMultiplier;

        // 非ロック時は手動回転
        if (!isLocked)
        {
            yaw += lookInput.x * rotationSpeed.x * dt * sensMul;
            pitch -= lookInput.y * rotationSpeed.y * dt * sensMul;
            pitch = Mathf.Clamp(pitch, pitchClamp.x, pitchClamp.y);
        }

        // ロックキー
        if (lockPressedThisFrame)
        {
            lockPressedThisFrame = false;
            if (!isLocked) { TryAcquireLock(); }
            else { CancelLock(); }
        }

        // ロック中の有効性と左右切替
        if (isLocked)
        {
            if (!IsValidLockTarget(lockedEnemy))
            {
                CancelLock();
            }
            else
            {
                float horiz = lookInput.x;
                if (Mathf.Abs(horiz) > switchInputThreshold && switchTimer <= 0f)
                {
                    SwitchLockTarget(horiz > 0f ? +1 : -1);
                    switchTimer = switchCooldown;
                }
            }
        }

        // ピボット補間
        float pivotSpeed = isLocked ? pivotLerpSpeed_Locked : pivotLerpSpeed_Normal;
        Vector3 desiredPivot = (isLocked && aimTarget != null) ? aimTarget.position : target.position;
        followPivot = Vector3.SmoothDamp(
            followPivot, desiredPivot, ref pivotVelocity,
            1f / Mathf.Max(0.0001f, pivotSpeed)
        );

        // リグ位置
        transform.position = followPivot;

        // 向き
        if (isLocked && lockedEnemy != null)
        {
            Vector3 refPos = (aimTarget != null ? aimTarget.position : followPivot);
            Vector3 dir = GetAimPoint(lockedEnemy) - refPos;

            if (dir.sqrMagnitude > 1e-6f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

                if (needsSmoothRotate)
                {
                    smoothLockRotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lockSwitchRotateSpeed);
                    transform.rotation = smoothLockRotation;

                    var e = smoothLockRotation.eulerAngles;
                    yaw = e.y;
                    float rp = e.x > 180f ? e.x - 360f : e.x;
                    pitch = Mathf.Clamp(rp, pitchClamp.x, pitchClamp.y);

                    if (Quaternion.Angle(transform.rotation, targetRot) < 0.5f)
                        needsSmoothRotate = false;
                }
                else
                {
                    transform.rotation = targetRot;

                    var e = targetRot.eulerAngles;
                    yaw = e.y;
                    float rp = e.x > 180f ? e.x - 360f : e.x;
                    pitch = Mathf.Clamp(rp, pitchClamp.x, pitchClamp.y);
                }
            }
        }
        else
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // 簡易衝突縮距
        float actualDistance = defaultDistance;
        if (Physics.Raycast(followPivot, -transform.forward, out RaycastHit hit, defaultDistance, collisionMask))
        {
            actualDistance = Mathf.Clamp(hit.distance - 0.25f, minDistance, defaultDistance);
        }

        // カメラ位置更新 (切替時ブレンドを優先)
        float moveSpeed = isLocked ? smoothSpeed_Locked : smoothSpeed_Normal;
        Vector3 finalPos = followPivot - transform.forward * actualDistance;

        if (switchMoveActive)
        {
            switchMoveRemain -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(switchMoveRemain / switchMoveTotal);
            float eased = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); // イーズアウト
            cam.position = Vector3.Lerp(camPosOnSwitch, finalPos, eased);

            if (switchMoveRemain <= 0f)
            {
                switchMoveActive = false;
                currentVelocity = Vector3.zero; // 履歴を切る
                cam.position = finalPos;
            }
        }
        else
        {
            cam.position = Vector3.SmoothDamp(
                cam.position, finalPos, ref currentVelocity,
                1f / Mathf.Max(0.0001f, moveSpeed)
            );
        }

        // 注視点
        if (isLocked && lockedEnemy != null)
        {
            cam.LookAt(GetAimPoint(lockedEnemy));
        }
        else
        {
            cam.LookAt(target.position + Vector3.up * 0.5f);
        }
    }

    private bool IsGamepadUsed()
    {
        return Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
    }

    // ロック取得
    private void TryAcquireLock()
    {
        Transform best = FindBestTargetInFov();
        if (best != null)
        {
            isLocked = true;
            lockedEnemy = best;
            needsSmoothRotate = true;

            PlayerEvents.OnAimTargetChanged?.Invoke(lockedEnemy.gameObject);
        }
    }

    // ロック解除
    private void CancelLock()
    {
        if (!isLocked) return;
        isLocked = false;
        lockedEnemy = null;
        needsSmoothRotate = false;

        PlayerEvents.OnAimTargetChanged?.Invoke(null);
    }

    // 左右切替
    private void SwitchLockTarget(int dirSign)
    {
        if (!isLocked || lockedEnemy == null) return;

        List<Transform> candidates = GatherTargetsInFov();
        if (candidates.Count == 0) return;

        candidates = candidates
            .Where(t => t != lockedEnemy && IsValidLockTarget(t))
            .OrderBy(t => AngleFromCameraForward(t))
            .ToList();

        if (candidates.Count == 0) return;

        Vector3 right = transform.right;
        var sideList = candidates
            .Where(t =>
            {
                Vector3 to = (GetAimPoint(t) - cam.position).normalized;
                float side = Mathf.Sign(Vector3.Dot(to, right)); // 右は+1, 左は-1
                return side == Mathf.Sign(dirSign);
            })
            .ToList();

        Transform next = (sideList.Count > 0 ? sideList : candidates).First();

        if (next != null)
        {
            lockedEnemy = next;
            needsSmoothRotate = true;
            PlayerEvents.OnAimTargetChanged?.Invoke(lockedEnemy.gameObject);

            // 位置ブレンド開始
            switchMoveActive = true;
            switchMoveTotal = Mathf.Max(0.0001f, switchMoveBlendTime);
            switchMoveRemain = switchMoveTotal;
            camPosOnSwitch = cam.position;

            // SmoothDamp の履歴速度を切る
            currentVelocity = Vector3.zero;
        }
    }

    // FOV 内の最適ターゲット
    private Transform FindBestTargetInFov()
    {
        List<Transform> list = GatherTargetsInFov();
        if (list.Count == 0) return null;

        Transform best = null;
        float bestAngle = float.MaxValue;
        float bestDist = float.MaxValue;

        foreach (var t in list)
        {
            float ang = AngleFromCameraForward(t);
            float dist = Vector3.Distance(target.position, t.position);

            if (ang < bestAngle || (Mathf.Approximately(ang, bestAngle) && dist < bestDist))
            {
                best = t;
                bestAngle = ang;
                bestDist = dist;
            }
        }
        return best;
    }

    // 候補収集
    private List<Transform> GatherTargetsInFov()
    {
        var results = new List<Transform>();
        Collider[] hits = Physics.OverlapSphere(target.position, maxLockDistance, enemyLayer);
        if (hits == null || hits.Length == 0) return results;

        float half = lockFovAngle * 0.5f;
        Vector3 fwd = transform.forward;

        foreach (var c in hits)
        {
            Transform t = c.transform;
            if (!IsValidLockTarget(t)) continue;

            Vector3 to = (GetAimPoint(t) - cam.position).normalized;
            float ang = Vector3.Angle(fwd, to);
            if (ang <= half) results.Add(t);
        }
        return results;
    }

    // カメラ前方からの角度
    private float AngleFromCameraForward(Transform t)
    {
        Vector3 to = (GetAimPoint(t) - cam.position).normalized;
        return Vector3.Angle(transform.forward, to);
    }

    // 有効性
    private bool IsValidLockTarget(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;
        float d = Vector3.Distance(target.position, t.position);
        if (d > maxLockDistance) return false;
        return true;
    }

    // 狙い点
    private Vector3 GetAimPoint(Transform t)
    {
        var ap = t.GetComponentInChildren<AimPointMarker>();
        if (ap != null) return ap.transform.position;

        if (t.TryGetComponent<Collider>(out var col))
            return col.bounds.center;

        return t.position + Vector3.up * 1.0f;
    }
}

