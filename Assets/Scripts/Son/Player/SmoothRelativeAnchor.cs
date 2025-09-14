using UnityEngine;

/// <summary>
/// カメラ用アンカー（相対追従＋ワールド独立運用）
/// ・開始時にターゲット（プレイヤー）から「相対位置/回転」を記録
/// ・以降はワールド空間で独立し、毎フレームで相対目標へスムーズ追従
/// ・アニメ揺れ/子階層のノイズを遮断し、ダッシュ中の急変にも追従品質を維持
/// </summary>
[DefaultExecutionOrder(50)]
public class SmoothRelativeAnchor : MonoBehaviour
{
    [Header("=== 追従対象 ===")]
    public Transform target;                 // プレイヤー等の基準
    public bool detachOnStart = true;        // 開始時に親子関係を解除するか

    [Header("=== 相対オフセット ===")]
    public Vector3 initialLocalOffset = new Vector3(0f, 1.6f, -0.5f); // 初期の相対位置（ターゲット基準）
    public Vector3 initialLocalEulerOffset = Vector3.zero;            // 初期の相対回転（オイラー）

    [Header("=== 追従スムーズ ===")]
    [Tooltip("位置のスムーズ時間（秒）。小さいほどキビキビ")]
    public float positionSmoothTime = 0.12f;
    [Tooltip("回転の遷移係数。0=即時 / 1=全く回らない。推奨: 0.1?0.3")]
    [Range(0f, 1f)] public float rotationLerpFactor = 0.15f;
    [Tooltip("大きく離れた場合のスナップ距離（m）。0以下で無効")]
    public float snapDistance = 6f;
    [Tooltip("位置スムーズの最大速度（m/s）。0以下で無制限")]
    public float maxPositionSpeed = 0f;

    [Header("=== 予測（任意）===")]
    [Tooltip("ターゲットの速度を推定して、少し先を追う（ダッシュ時に有効）")]
    public bool usePrediction = true;
    [Tooltip("先読み時間（秒）。0.03?0.1 程度")]
    public float predictionTime = 0.06f;
    [Tooltip("速度のローパス（0=即時/1=追従せず）。推奨: 0.15?0.35")]
    [Range(0f, 1f)] public float velocitySmoothing = 0.2f;

    // --- 内部状態 ---
    private Vector3 _localOffset;            // 実際に使う相対位置
    private Quaternion _localRotOffset;      // 実際に使う相対回転
    private Vector3 _vel;                    // SmoothDamp 用の速度
    private Vector3 _lastTargetPos;
    private Vector3 _targetVel;              // 推定ターゲット速度
    private bool _initialized;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[SmoothRelativeAnchor] target 未設定");
            enabled = false;
            return;
        }

        // --- 初期相対オフセット（明示指定 or シーン上の現在相対） ---
        // すでにターゲットの子で配置されている場合は、そのローカルを採用しやすい
        if (transform.parent == target)
        {
            _localOffset = transform.localPosition;
            _localRotOffset = transform.localRotation;
        }
        else
        {
            _localOffset = initialLocalOffset;
            _localRotOffset = Quaternion.Euler(initialLocalEulerOffset);
        }

        // --- 親子関係の解除（ワールド空間へ） ---
        if (detachOnStart)
        {
            // 現在のワールド変換を保持したまま親を外す
            transform.SetParent(null, true);
        }

        // --- 初期位置/回転の確定（相対からワールドへ反映） ---
        var desired = ComputeDesiredPose(Time.deltaTime);
        transform.position = desired.position;
        transform.rotation = desired.rotation;

        _lastTargetPos = target.position;
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized || target == null) return;

        // --- ターゲット速度の推定 ---
        var rawVel = (target.position - _lastTargetPos) / Mathf.Max(Time.deltaTime, 1e-6f);
        _targetVel = Vector3.Lerp(rawVel, _targetVel, Mathf.Clamp01(velocitySmoothing));
        _lastTargetPos = target.position;

        // --- 相対目標の算出 ---
        var desired = ComputeDesiredPose(Time.deltaTime);

        // --- 大距離スナップ（ワープ/復帰時の安全策） ---
        if (snapDistance > 0f)
        {
            float dist = Vector3.Distance(transform.position, desired.position);
            if (dist > snapDistance)
            {
                transform.position = desired.position;
                transform.rotation = desired.rotation;
                _vel = Vector3.zero;
                return;
            }
        }

        // --- 位置をスムーズに追従（SmoothDamp） ---
        float smooth = Mathf.Max(0.0001f, positionSmoothTime);
        if (maxPositionSpeed > 0f)
            transform.position = Vector3.SmoothDamp(transform.position, desired.position, ref _vel, smooth, maxPositionSpeed);
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired.position, ref _vel, smooth);

        // --- 回転は指数的に緩やかに補間（Slerp） ---
        float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(1f - rotationLerpFactor), Time.deltaTime * 60f);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired.rotation, t);
    }

    /// <summary>
    /// 相対目標のワールド姿勢を算出
    /// ターゲットの現在姿勢 × 相対オフセット
    /// 予測が有効なら先読み位置へ
    /// </summary>
    private (Vector3 position, Quaternion rotation) ComputeDesiredPose(float dt)
    {
        // 相対回転を適用した「相対位置」
        Vector3 local = _localRotOffset * _localOffset;
        // ターゲット姿勢に乗せる
        Vector3 pos = target.TransformPoint(local);
        Quaternion rot = target.rotation * _localRotOffset;

        // 予測（任意）
        if (usePrediction && predictionTime > 0f)
        {
            pos += _targetVel * predictionTime;
        }
        return (pos, rot);
    }

    // ====== 公開API ======

    /// <summary>
    /// 現在のターゲット⇔アンカー関係から「相対オフセット」を再計算して記録
    /// ・新しいカメラ構図を基準にしたい時に使用
    /// </summary>
    public void RefreshRelativeOffset()
    {
        if (target == null) return;
        // ターゲット空間へ逆変換
        _localOffset = target.InverseTransformPoint(transform.position);
        // 回転の相対
        _localRotOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
    }

    /// <summary>
    /// すぐ親子関係を解除（ワールド空間で保持）
    /// </summary>
    public void DetachNow()
    {
        if (transform.parent != null)
            transform.SetParent(null, true);
    }

    /// <summary>
    /// ターゲットの子に戻す（デバッグ/一時用途）
    /// </summary>
    public void AttachBack(bool keepWorld = true)
    {
        if (target == null) return;
        transform.SetParent(target, keepWorld);
    }

    /// <summary>
    /// 目標相対を直接セット（外部システムから構図を切り替えたい場合）
    /// </summary>
    public void SetRelativeOffset(Vector3 localOffset, Quaternion localRotOffset)
    {
        _localOffset = localOffset;
        _localRotOffset = localRotOffset;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.3f);
    }
}
