using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class BommerangAttack : MonoBehaviour
{
    // === �q�b�g���ɓn���_���[�W��� ===
    [SerializeField] public DamageData damageData = new DamageData(2);

    // === ���������u�Ԃɒe��j�����邩 ===
    [FormerlySerializedAs("�����j��")]
    [SerializeField] public bool isDestroyedOnHit = true;

    // === �����蔻��Ώۂ̃��C���[ ===
    [SerializeField] public LayerMask hitLayer;

    // === �q�b�g���̃G�t�F�N�g ===
    [SerializeField] public GameObject hitEffect;

    // === �e�̎��� ===
    [SerializeField] public float lifetime = 1f;

    // === ���� Collider �ւ̍ăq�b�g�Ԋu ===
    [SerializeField] public double hitInterval = -1;

    // ---- �q�b�g���� ----
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
        // --- 1) ���C���[�t�B���^ ---
        if (!IsInLayerMask(other.gameObject.layer, hitLayer))
            return;

        // --- 2) ����t���[�����ɖ����ȓ����� ---
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == this.gameObject)
            return;

        // --- 3) ��x���� or �N�[���_�E������ ---
        if (!CanHitNow(other, Time.timeAsDouble))
            return;

        // --- 4) �_���[�W�K�p�iIDamageable ������O��B�Ȃ���ΐe���T���j ---
        var dmgTarget = other.GetComponent<Enemy>();
        if (dmgTarget == null) dmgTarget = other.GetComponentInParent<Enemy>();
        if (dmgTarget != null)
        {
            // �_���[�W��K�p
            dmgTarget.TakeDamage(damageData);

            // �q�b�g�����̍X�V
            _lastHitTimePerCollider[other] = Time.timeAsDouble;

            // �q�b�g�G�t�F�N�g����
            SpawnHitVFX(other);

            // �������j���t���O�������Ă���Ȃ玩��
            if (isDestroyedOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    // === �_���[�W���̐ݒ� ===
    public void SetDamage(DamageData dmg)
    {
        damageData = dmg;
    }

    // === �w�背�C���[�� LayerMask �Ɋ܂܂�邩 ===
    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // === ���� Collider �ɍ��q�b�g���Ă悢���i�Ԋu�`�F�b�N�j ===
    private bool CanHitNow(Collider col, double now)
    {
        // ���l�F��x�����q�b�g����
        if (hitInterval < 0)
        {
            if (_lastHitTimePerCollider.ContainsKey(col)) return false; // ���ɓ�������
            return true;
        }

        // �񕉒l�F�N�[���_�E������
        double last;
        if (!_lastHitTimePerCollider.TryGetValue(col, out last))
            return true; // ����

        return (now - last) >= hitInterval;
    }

    // === �q�b�g���� VFX ���� ===
    private void SpawnHitVFX(Collider other)
    {
        if (hitEffect == null) return;

        // ������ʒu�̐���
        var myPos = transform.position;
        var closest = other.ClosestPoint(myPos);
        var pos = closest;

        // �����͍U���̐i�s����
        Quaternion rot;
        var dir = (closest - myPos);
        if (dir.sqrMagnitude > 1e-6f) rot = Quaternion.LookRotation(dir);
        else rot = transform.rotation;

        var vfx = Instantiate(hitEffect, pos, rot);

    }
}
