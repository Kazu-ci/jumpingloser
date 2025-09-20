using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class BommerangAttack : MonoBehaviour
{
    // === ヒット時に渡すダメージ情報 ===
    [SerializeField] public DamageData damageData = new DamageData(2);

    // === 命中した瞬間に弾を破棄するか ===
    [FormerlySerializedAs("命中破棄")]
    [SerializeField] public bool isDestroyedOnHit = true;

    // === 当たり判定対象のレイヤー ===
    [SerializeField] public LayerMask hitLayer;

    // === ヒット時のエフェクト ===
    [SerializeField] public GameObject hitEffect;

    // === 弾の寿命 ===
    [SerializeField] public float lifetime = 1f;

    // === 同じ Collider への再ヒット間隔 ===
    [SerializeField] public double hitInterval = -1;

    // ---- ヒット履歴 ----
    private readonly Dictionary<Collider, double> _lastHitTimePerCollider = new Dictionary<Collider, double>(32);

    // ----------------------------------------------------------------------
    // ▼▼▼ ここから「地形追従（Yのみ補正）」の統合設定 ▼▼▼
    // ----------------------------------------------------------------------
    [Header("=== 地形追従（Y 補正） ===")]
    [Tooltip("接地基準に使う子オブジェクト上の CapsuleCollider")]
    [SerializeField] private CapsuleCollider sourceCapsule;

    [Tooltip("地面からの目標オフセット")]
    [SerializeField] private float hoverHeight = 0.02f;

    [Tooltip("上向き開始オフセット")]
    [SerializeField] private float upCastExtra = 0.2f;

    [Tooltip("下向きのレイ長さ")]
    [SerializeField] private float downCastExtra = 0.6f;

    [Tooltip("補間のスムーズ時間")]
    [SerializeField] private float smoothTime = 0.05f;

    [Tooltip("地面レイヤー")]
    [SerializeField] private LayerMask groundMask;

    [Tooltip("デバッグ用の可視化（レイ表示など）")]
    [SerializeField] private bool drawGizmos = false;

    private float _yVelocity;

    private void Awake()
    {
        if (sourceCapsule == null)
        {
            sourceCapsule = GetComponentInChildren<CapsuleCollider>();
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnDisable()
    {
        _lastHitTimePerCollider.Clear();
    }

    private void LateUpdate()
    {
        // === 地形追従：Animator や移動で子が動いた「後」に Y を補正 ===
        AdjustYByGround();
    }

    private void OnTriggerStay(Collider other)
    {
        // --- 1) レイヤーフィルタ ---
        if (!IsInLayerMask(other.gameObject.layer, hitLayer))
            return;

        // --- 2) 同一フレーム中に無効な当たり ---
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == this.gameObject)
            return;

        // --- 3) 一度だけ or クールダウン判定 ---
        if (!CanHitNow(other, Time.timeAsDouble))
            return;

        // --- 4) ダメージ適用（IDamageable がある前提。なければ親も探索） ---
        var dmgTarget = other.GetComponent<Enemy>();
        if (dmgTarget == null) dmgTarget = other.GetComponentInParent<Enemy>();
        if (dmgTarget != null)
        {
            // ダメージを適用
            dmgTarget.TakeDamage(damageData);

            // ヒット履歴の更新
            _lastHitTimePerCollider[other] = Time.timeAsDouble;

            // ヒットエフェクト生成
            SpawnHitVFX(other);

            // 命中即破棄フラグが立っているなら自壊
            if (isDestroyedOnHit)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("BommerangAttack: No damageable component found on " + other.name);
        }
    }

    // === ダメージ情報の設定 ===
    public void SetDamage(DamageData dmg)
    {
        damageData = dmg;
    }

    // === 指定レイヤーが LayerMask に含まれるか ===
    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // === この Collider に今ヒットしてよいか（間隔チェック） ===
    private bool CanHitNow(Collider col, double now)
    {
        // 負値：一度だけヒット許可
        if (hitInterval < 0)
        {
            if (_lastHitTimePerCollider.ContainsKey(col)) return false; // 既に当たった
            return true;
        }

        // 非負値：クールダウン方式
        if (!_lastHitTimePerCollider.TryGetValue(col, out double last))
            return true; // 初回

        return (now - last) >= hitInterval;
    }

    // === ヒット時の VFX 生成 ===
    private void SpawnHitVFX(Collider other)
    {
        if (hitEffect == null) return;

        // 当たり位置の推定
        var myPos = transform.position;
        var closest = other.ClosestPoint(myPos);
        var pos = closest;

        // 向きは攻撃の進行方向
        Quaternion rot;
        var dir = (closest - myPos);
        if (dir.sqrMagnitude > 1e-6f) rot = Quaternion.LookRotation(dir);
        else rot = transform.rotation;

        var vfx = Instantiate(hitEffect, pos, rot);
        // ここで必要なら自動破棄や親子付けなどを行う
    }

    // ----------------------------------------------------------------------
    // 地形追従（Y 補正）本体
    // ・子の CapsuleCollider の底球中心を基準に、地面との距離を計測し、
    //   ルートオブジェクトの Y のみを補正する
    // ----------------------------------------------------------------------
    private void AdjustYByGround()
    {
        if (sourceCapsule == null) return;

        // --- ワールドへ変換 ---
        Vector3 worldCenter = sourceCapsule.transform.TransformPoint(sourceCapsule.center);

        // 非一様スケール対応（半径は最大軸を採用）
        float sx = Mathf.Abs(sourceCapsule.transform.lossyScale.x);
        float sy = Mathf.Abs(sourceCapsule.transform.lossyScale.y);
        float sz = Mathf.Abs(sourceCapsule.transform.lossyScale.z);
        float worldRadius = sourceCapsule.radius * Mathf.Max(sx, sy, sz);
        float worldHeight = Mathf.Max(sourceCapsule.height * sy, worldRadius * 2f);

        // カプセルの“上方向”
        Vector3 up = sourceCapsule.transform.TransformDirection(Vector3.up).normalized;

        // 底球中心
        float half = worldHeight * 0.5f;
        Vector3 bottomSphereCenter = worldCenter - up * (half - worldRadius);

        // レイの開始点（底球中心より少し上）
        Vector3 rayStart = bottomSphereCenter + up * upCastExtra;
        Vector3 rayDir = -up;
        float rayLen = upCastExtra + downCastExtra + worldRadius + hoverHeight;

        // 地面レイキャスト
        if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
        {
            // 目標：底球中心が「地面 + hoverHeight」に来るようにする
            Vector3 targetBottomCenter = hit.point + up * hoverHeight;

            Vector3 pos = transform.position;
            float deltaAlongUp = Vector3.Dot((targetBottomCenter - bottomSphereCenter), up);
            float targetY = pos.y + deltaAlongUp;

            // Y のみスムーズ補正
            float newY = Mathf.SmoothDamp(pos.y, targetY, ref _yVelocity, smoothTime);
            transform.position = new Vector3(pos.x, newY, pos.z);

            if (drawGizmos)
            {
                Debug.DrawLine(rayStart, hit.point, Color.green);
            }
        }
        else
        {
            // 地面が検出できない（空中など）。Yは保持
            if (drawGizmos)
            {
                Debug.DrawLine(rayStart, rayStart + rayDir * rayLen, Color.yellow);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || sourceCapsule == null) return;

        Vector3 worldCenter = sourceCapsule.transform.TransformPoint(sourceCapsule.center);
        float sx = Mathf.Abs(sourceCapsule.transform.lossyScale.x);
        float sy = Mathf.Abs(sourceCapsule.transform.lossyScale.y);
        float sz = Mathf.Abs(sourceCapsule.transform.lossyScale.z);
        float worldRadius = sourceCapsule.radius * Mathf.Max(sx, sy, sz);
        float worldHeight = Mathf.Max(sourceCapsule.height * sy, worldRadius * 2f);
        Vector3 up = sourceCapsule.transform.TransformDirection(Vector3.up).normalized;
        float half = worldHeight * 0.5f;
        Vector3 bottomSphereCenter = worldCenter - up * (half - worldRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(bottomSphereCenter, worldRadius * 0.2f);
    }
}