using UnityEngine;
using UnityEngine.AI;
public struct DamageData
{
    public float damageAmount; // ダメージ量
    // 他のダメージ関連情報（属性など）を追加可能
    public DamageData(float damage)
    {
        damageAmount = damage;
    }
}
public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject[] weaponDrops; // ドロップされる武器のプレハブ（2種類）
    [SerializeField] protected float maxHp;
    [SerializeField] protected float maxSpeed;
    [SerializeField] protected float attackSpeed;
    [SerializeField] protected int attackDamage;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float lookPlayerDir;
    [SerializeField] protected float angle;
    [SerializeField] protected GameObject playerPos;
    protected float nowHp;
    protected float nowSpeed;
    protected float distance;
    protected NavMeshAgent navMeshAgent;


    protected GameObject TestTarget;

    private bool _isDead; // 重複死亡防止フラグ

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected virtual void UpdateTestTarget()
    {
        TestTarget = EventBus.PlayerEvents.GetPlayerObject?.Invoke();
        if (TestTarget != null)
        {
            Debug.Log($"Enemy: Target{TestTarget.name} found");
        }
        else
        {
            Debug.Log("Enemy: No target found");
        }
    }
    protected virtual void OnDamage()
    {
        nowHp-=5;
    }
    public virtual void OnAttackSet(){  }
    public virtual void OnAttackEnd() { }
    public virtual int TakeDamage(DamageData dmg)
    {
        nowHp -= (int)dmg.damageAmount;
        if (nowHp <= 0)
        {
            OnDead();
        }
        return (int)dmg.damageAmount;
    }
    protected virtual void OnDead()
    {
        if (_isDead) return;
        _isDead = true;
        DropWeapon();
        Destroy(gameObject);
    }
    protected float GetDistance()
    {
        return Vector3.Distance(playerPos.transform.position,transform.position);
    }
    void DropWeapon()
    {
        // 配列が空ならドロップしない
        if (weaponDrops == null || weaponDrops.Length == 0) return;
        int index = Random.Range(0, weaponDrops.Length); // ランダム選択
        Instantiate(weaponDrops[index], transform.position, Quaternion.identity);

    }
    protected bool animationEnd()
    {
        return true;
    }
    public int GetDamage()
    {
        return attackDamage;
    }
    public Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 rand = center + new Vector3(
                Random.Range(-radius, radius),
                0,
                Random.Range(-radius, radius)
            );
            rand.y = center.y; // 高さを固定

            NavMeshHit hit;
            if (NavMesh.SamplePosition(rand, out hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return center;
    }

}
