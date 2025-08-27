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
    [SerializeField] protected float attackDamage;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float lookPlayerDir;
    [SerializeField] protected float angle;
    [SerializeField] protected GameObject playerPos;
    protected float nowHp;
    protected float nowSpeed;
    protected float distance;
    protected NavMeshAgent navMeshAgent;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
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
        DropWeapon();
        Destroy(gameObject);
    }
    protected float GetDistance()
    {
        return Vector3.Distance(playerPos.transform.position,transform.position);
    }
    void DropWeapon()
    {
        int index = Random.Range(0, weaponDrops.Length); // ランダム選択
        Instantiate(weaponDrops[index], transform.position, Quaternion.identity);

    }
    protected bool animationEnd()
    {
        return true;
    }
    
}
