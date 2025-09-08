using Unity.VisualScripting;
using UnityEngine;

public class OnHitDamage : MonoBehaviour
{
    [SerializeField ]private Enemy enemy;
    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }
    public void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerMovement>();
        if(player != null)
        {
            DamageData damageData = new DamageData(enemy.GetDamage());
            player.TakeDamage(damageData);
        }
        else { return; }
    }
}
