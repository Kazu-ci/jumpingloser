using System;
using UnityEngine;

/// <summary>
/// LungeManager for CharacterController（単体版）
/// ・攻撃突進／ダッシュの統合コンポーネント（外部は開始時に一度だけ呼び出す）
/// ・CharacterController.Move を主軸に、事前の Physics.CapsuleCast で距離クリップ（隧穿対策）
/// ・CC の slopeLimit / stepOffset / skinWidth を活用（斜面/段差対応）
///
/// 重要仕様：lungeDistance == 0f は「突進しない」（無制限ではない）
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class LungeManager : MonoBehaviour
{
    // ===== 方向モード =====
    public enum LungeAim { Forward, ToTarget, CustomDir }

    [Header("参照")]
    [Tooltip("前方の基準（未設定なら自分のTransform）")]
    public Transform facingSource;

    [Header("必須：CharacterController")]
    public CharacterController controller;

    [Header("衝突判定レイヤー（地形/壁など）")]
    public LayerMask environmentMask = ~0;

    [Header("物理/数値安定パラメータ")]
    [Tooltip("プリスイープ距離から差し引く最小マージン")]
    public float skin = 0.03f;
    [Tooltip("地面吸着距離（微小な浮きを解消）")]
    public float groundSnapDistance = 0.08f;
    [Tooltip("スイープ時の半径膨張（微少値）")]
    public float radiusInflation = 0.01f;
    [Tooltip("段差補助：必要ならスイープ前に一時的に上げる量（CC.stepOffset と併用可）")]
    public float preStepLift = 0.0f; // 0 でCCに任せる

    // ==== ランタイム移動状態 ====
    private bool isLunging;
    private Vector3 moveDir = Vector3.zero;  // 正規化方向（水平）
    private float baseSpeed = 0f;            // m/s
    private float? maxTime = null;           // 秒（null=時間制限なし）
    private float? maxDistance = null;       // m（null=距離制限なし）※距離版で使用
    private AnimationCurve accelCurve = null;

    private float elapsed = 0f;              // 経過時間
    private float movedDistance = 0f;        // 蓄積距離

    // ==== 外部通知 ====
    public event Action OnLungeStart;     // 突進開始
    public event Action OnLungeFinish;    // 正常終了（距離/時間）
    public event Action OnLungeBlocked;   // 衝突（事前スイープで距離が詰まった）
    public event Action OnLungeTooSteep;  // 斜面限界（上り禁止）

    public bool IsLunging => isLunging;
    public Vector3 LastUsedDirection => moveDir;

    private void Reset()
    {
        if (!facingSource) facingSource = transform;
        controller = GetComponent<CharacterController>();
        // CC の推奨設定（必要に応じてInspectorで上書き）
        controller.minMoveDistance = 0f;          // 微小移動も拾う
        controller.detectCollisions = true;
        // slopeLimit（度）、stepOffset、skinWidth はプロジェクト基準に合わせて設定
    }

    private void Awake()
    {
        if (!facingSource) facingSource = transform;
        if (!controller) controller = GetComponent<CharacterController>();
    }

    // ======================================================================
    // 公開API：距離ベース（推奨）  ※ distance <= 0f → 非発動
    // ======================================================================
    public bool StartLungeByDistance(
        LungeAim aim,
        Vector3 toTargetPos,
        Vector3 customDir,
        float speed,
        float distance,
        AnimationCurve curve = null)
    {
        if (distance <= 0f) return false; // ← 重要：0は突進しない
        if (!TryResolveDirection(aim, toTargetPos, customDir, out Vector3 dir)) return false;

        moveDir = dir;
        baseSpeed = Mathf.Max(0.01f, speed);
        maxDistance = distance;
        maxTime = null;
        accelCurve = curve;

        BeginLunge();
        return true;
    }

    // ======================================================================
    // 公開API：時間ベース（必要時のみ）
    // ======================================================================
    public bool StartLungeByTime(
        LungeAim aim,
        Vector3 toTargetPos,
        Vector3 customDir,
        float speed,
        float timeSeconds,
        AnimationCurve curve = null)
    {
        if (timeSeconds <= 0f) return false;
        if (!TryResolveDirection(aim, toTargetPos, customDir, out Vector3 dir)) return false;

        moveDir = dir;
        baseSpeed = Mathf.Max(0.01f, speed);
        maxTime = timeSeconds;
        maxDistance = null;
        accelCurve = curve;

        BeginLunge();
        return true;
    }

    /// <summary>外部から強制キャンセル（ステート遷移時の保険）</summary>
    public void ForceCancel(bool invokeFinish = true)
    {
        isLunging = false;
        elapsed = 0f;
        movedDistance = 0f;
        if (invokeFinish) OnLungeFinish?.Invoke();
    }

    private void BeginLunge()
    {
        isLunging = true;
        elapsed = 0f;
        movedDistance = 0f;
        OnLungeStart?.Invoke();
    }

    private void FixedUpdate()
    {
        if (!isLunging) return;

        float dt = Time.fixedDeltaTime;

        // === 速度決定（加減速カーブ対応） ===
        float speed = baseSpeed;
        if (accelCurve != null)
        {
            float denom = Mathf.Max(1e-4f, maxTime ?? 1f);
            float t01 = Mathf.Clamp01(elapsed / denom);
            speed *= Mathf.Max(0f, accelCurve.Evaluate(t01));
        }

        // === 移動予定ベクトル（水平） ===
        Vector3 delta = moveDir * speed * dt;

        // === 上りが斜面限界を超えるか簡易判定（CC の slopeLimit を尊重） ===
        // CC は内部で処理するが、"強制停止" のゲームルールを入れたい場合に事前制御
        // 上り角がきつすぎる時は中断（プレイヤーのYは外部の重力系で管理前提）
        // → delta が上向き成分を持つ場合かつ slopeLimit を超えそうなら停止
        if (controller.slopeLimit < 89.9f && Vector3.Dot(delta, Vector3.up) > 0f)
        {
            // 簡易：接地近傍なら上りを禁止（厳密な地面法線が必要なら SphereCast で拾う）
            if (controller.isGrounded)
            {
                StopTooSteep();
                return;
            }
        }

        // === 段差補助（必要なら一時的に持ち上げてからスイープ） ===
        Vector3 startPos = transform.position;
        if (preStepLift > 0f && controller.isGrounded)
        {
            // CC.Move で上げると衝突判定が入るので、事前スイープ位置だけ持ち上げる
            startPos += Vector3.up * preStepLift;
        }

        // === 事前スイープ（Physics.CapsuleCast）で距離クリップ（隧穿防止） ===
        float allowed = ClipByCapsuleCast(startPos, delta, out RaycastHit hit);
        bool blocked = false;
        Vector3 move = delta;

        if (allowed < delta.magnitude)
        {
            move = delta.normalized * Mathf.Max(0f, allowed - skin);
            blocked = true;
        }

        // === 実移動：CharacterController.Move ===
        // ・CC が分離/段差/斜面を内部処理
        // ・Y方向の制御（重力/着地）はゲーム側の別系統で
        controller.Move(move);

        // === 地面吸着（微小な浮き下げ；CC.isGrounded が false でも近傍なら下げる） ===
        if (!controller.isGrounded && groundSnapDistance > 0f)
        {
            if (ProbeGroundDistance(out float down))
            {
                // CC.Move でのみ位置を適用
                controller.Move(Vector3.down * down);
            }
        }

        // === 進捗更新＆停止条件 ===
        movedDistance += move.magnitude;
        elapsed += dt;

        if (blocked)
        {
            StopBlocked();
            return;
        }
        if (maxDistance.HasValue && movedDistance >= maxDistance.Value - 1e-4f)
        {
            StopFinished();
            return;
        }
        if (maxTime.HasValue && elapsed >= maxTime.Value - 1e-6f)
        {
            StopFinished();
            return;
        }
    }

    // ==== 停止ハンドラ ====
    private void StopFinished()
    {
        isLunging = false;
        OnLungeFinish?.Invoke();
    }
    private void StopBlocked()
    {
        isLunging = false;
        OnLungeBlocked?.Invoke();
    }
    private void StopTooSteep()
    {
        isLunging = false;
        OnLungeTooSteep?.Invoke();
    }

    // ======================================================================
    // 事前スイープ（CapsuleCast）? CC の形状をワールドに展開して使用
    // ======================================================================
    private float ClipByCapsuleCast(Vector3 startPos, Vector3 delta, out RaycastHit hitInfo)
    {
        hitInfo = new RaycastHit();
        if (delta.sqrMagnitude <= 1e-8f) return 0f;

        // CC のカプセルをワールドへ（Y軸アライン前提）
        GetControllerCapsule(out Vector3 p0, out Vector3 p1, out float r);

        // startPos に仮移動（スイープの原点だけ持ち上げる等に対応）
        Vector3 off = startPos - transform.position;
        p0 += off; p1 += off;

        float dist = delta.magnitude;
        Vector3 dir = delta / dist;
        float rr = r + radiusInflation;

        if (Physics.CapsuleCast(p0, p1, rr, dir, out hitInfo, dist + skin,
                                environmentMask, QueryTriggerInteraction.Ignore))
        {
            return hitInfo.distance;
        }
        return dist;
    }

    // CC のワールド形状（端点と半径）
    private void GetControllerCapsule(out Vector3 p0, out Vector3 p1, out float r)
    {
        // CharacterController はローカル center/height/radius を持つ
        // カプセル端点は CC の up 方向に沿って計算
        var t = controller.transform;
        Vector3 up = t.up;

        // lossyScale による半径のスケール（X/Zの平均を採用）
        float sx = Mathf.Abs(t.lossyScale.x);
        float sz = Mathf.Abs(t.lossyScale.z);
        float sy = Mathf.Abs(t.lossyScale.y);

        r = controller.radius * ((sx + sz) * 0.5f);
        float h = Mathf.Max(controller.height * sy, r * 2f);

        Vector3 center = t.TransformPoint(controller.center);
        float half = Mathf.Max(0f, h * 0.5f - r);

        p0 = center + up * half; // 上端
        p1 = center - up * half; // 下端
    }

    // 地面までの距離
    private bool ProbeGroundDistance(out float downDistance)
    {
        downDistance = 0f;
        GetControllerCapsule(out Vector3 p0, out Vector3 p1, out float r);
        Vector3 center = (p0 + p1) * 0.5f;

        if (Physics.SphereCast(center + Vector3.up * 0.02f, r * 0.95f, Vector3.down,
                               out RaycastHit hit, groundSnapDistance,
                               environmentMask, QueryTriggerInteraction.Ignore))
        {
            // 少し余裕を見て下げる距離を算出
            downDistance = Mathf.Max(0f, hit.distance - skin * 0.5f);
            return downDistance > 1e-3f;
        }
        return false;
    }

    // ======================================================================
    // 方向
    // ======================================================================
    private bool TryResolveDirection(LungeAim aim, Vector3 toTargetPos, Vector3 customDir, out Vector3 dir)
    {
        dir = Vector3.zero;
        switch (aim)
        {
            case LungeAim.Forward:
                dir = (facingSource ? facingSource.forward : transform.forward);
                break;
            case LungeAim.ToTarget:
                dir = (toTargetPos - transform.position);
                dir.y = 0f;
                break;
            case LungeAim.CustomDir:
                dir = customDir;
                break;
        }
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return false;
        dir.Normalize();
        return true;
    }
}
