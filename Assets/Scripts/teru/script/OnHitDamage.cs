using Unity.VisualScripting;
using UnityEngine;

public class OnHitDamage : MonoBehaviour
{
    [SerializeField ]private Enemy enemy;
    [SerializeField]private float damage;
    public void OnTriggerEnter(Collider other)
    {
        // プレイヤーへのダメージ処理
        var player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            float finalDamage = (enemy != null) ? enemy.GetDamage() : damage;
            DamageData damageData = new DamageData(finalDamage);
            player.TakeDamage(damageData);
        }
        // 敵へのダメージ処理
        var otherEnemy = other.GetComponent<Enemy>();
        if (otherEnemy != null && otherEnemy != enemy) // 自分以外のEnemy
        {
            float finalDamage = (enemy != null) ? enemy.GetDamage() : damage;
            DamageData damageData = new DamageData(finalDamage);
            otherEnemy.TakeDamage(damageData);
        }
    }
}
