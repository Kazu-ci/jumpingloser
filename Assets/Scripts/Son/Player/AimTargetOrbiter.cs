using UnityEngine;

public class AimTargetOrbiter : MonoBehaviour
{
    [Header("=== 参照 ===")]
    public Transform player;          // プレイヤー（中心）
    public Transform cameraRef;       // 参照するカメラ（通常は Camera.main.transform）

    [Header("=== 配置パラメータ（水平面での配置）===")]
    public float backDistance = 0.8f; // プレイヤー後方への距離（カメラ前方の逆方向）
    public float shoulderOffset = 0.6f; // 右肩(+) / 左肩(-) 方向オフセット（カメラ右方向基準）
    public float height = 1.5f;       // プレイヤー足元からの高さ加算

    [Header("=== スムージング ===")]
    public float positionSmooth = 12f; // 位置のスムーズ追従（大きいほど追従が速い）
    public float rotationSmooth = 12f; // 向きのスムーズ追従（必要な場合）
    public bool alignYawToCamera = true; // 水平向きをカメラ方位に合わせる（任意）

    [Header("=== その他 ===")]
    public bool autoDetachFromPlayer = true; // プレイヤーの子の場合に自動で unparent して回転の継承をカット

    // --- 内部用 ---
    private Vector3 velocity;         // SmoothDamp 用
    private Quaternion rotVelocity;   // 回転補間（Slerp を使うので未使用でもOK）

    void Awake()
    {
        // カメラ未指定ならメインカメラを使用
        if (cameraRef == null && Camera.main != null)
            cameraRef = Camera.main.transform;

        // プレイヤーの子になっているとプレイヤー回転を継承してしまうため、必要なら自動で外す
        if (autoDetachFromPlayer && transform.parent != null && player != null && transform.IsChildOf(player))
        {
            transform.SetParent(null, true); // ワールド座標を維持して unparent
        }
    }

    void LateUpdate()
    {
        if (player == null || cameraRef == null) return;

        // --- カメラ基準の水平ベクトルを作成（Y成分を落として正規化） ---
        Vector3 camF = cameraRef.forward;
        camF.y = 0f;
        if (camF.sqrMagnitude < 1e-6f) camF = player.forward; // 非常時フォールバック
        camF.Normalize();

        Vector3 camR = cameraRef.right;
        camR.y = 0f;
        if (camR.sqrMagnitude < 1e-6f) camR = player.right;   // フォールバック
        camR.Normalize();

        // --- 望ましい位置：プレイヤー位置 + 後方 + 肩側 + 高さ ---
        Vector3 desired =
            player.position
            + (-camF * backDistance)
            + (camR * shoulderOffset)
            + Vector3.up * height;

        // --- 位置をスムーズに追従 ---
        float t = (positionSmooth <= 0f) ? 1f : (Time.deltaTime * positionSmooth);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, (positionSmooth <= 0f ? 0f : 1f / positionSmooth));

        // --- （任意）水平方向の向きをカメラ方位へ合わせる ---
        if (alignYawToCamera)
        {
            // カメラ水平前方へ向ける（ピッチは固定）
            if (camF.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camF, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(player.position + Vector3.up * height, transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
#endif
}
