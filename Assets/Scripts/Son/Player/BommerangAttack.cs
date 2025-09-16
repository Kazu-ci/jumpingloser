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

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnDisable()
    {
        _lastHitTimePerCollider.Clear();
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
        double last;
        if (!_lastHitTimePerCollider.TryGetValue(col, out last))
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

    }
}
