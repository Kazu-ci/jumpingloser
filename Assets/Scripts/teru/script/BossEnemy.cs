using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections; 
using static UnityEngine.UI.GridLayoutGroup;

public class BossEnemy : Enemy
{
    EStateMachine<BossEnemy> stateMachine;
    [SerializeField] GameObject[] mobEnemy;
    [SerializeField] private List<string> attackStates; 
    [SerializeField] private List<GameObject> effects;  
    [SerializeField] private List<Collider> attackColliders; 
    [SerializeField] float rushSpeed;
    [SerializeField] float stiffnessTime;
    private Dictionary<string, Collider> colliderDict;
    private Dictionary<string, GameObject> effectDict;

    private enum EnemyState
    {
        Idle,
        Chase,
        Vigilance,
        Combo,
        Rotate,
        Sumon,
        Rush,
        Stiffness,
        Hit,
        Dead
    }
    void Awake()
    {
        effectDict = new Dictionary<string, GameObject>();
        colliderDict = new Dictionary<string, Collider>();
        for (int i = 0; i < Mathf.Min(attackStates.Count, effects.Count); i++)
        {
            effectDict[attackStates[i]] = effects[i];
            effects[i].SetActive(false);
            if (i < attackColliders.Count && attackColliders[i] != null)
                colliderDict[attackStates[i]] = attackColliders[i];
        }

        foreach (var c in attackColliders) c.enabled = false;
    }
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        nowHp = maxHp;
        stateMachine = new EStateMachine<BossEnemy>(this);
        stateMachine.Add<IdleState>((int)EnemyState.Idle);
        stateMachine.Add<ChaseState>((int)EnemyState.Chase);
        stateMachine.Add<VigilanceState>((int)EnemyState.Vigilance);
        stateMachine.Add<ComboState>((int)EnemyState.Combo);
        stateMachine.Add<RotateState>((int)EnemyState.Rotate);
        stateMachine.Add<SumonState>((int)EnemyState.Sumon);
        stateMachine.Add<RushState>((int)EnemyState.Rush);
        stateMachine.Add<StiffnessState>((int)EnemyState.Stiffness);
        stateMachine.Add<HitState>((int)EnemyState.Hit);
        stateMachine.Add<DeadState>((int)EnemyState.Dead);
        stateMachine.OnStart((int)EnemyState.Idle);
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        OnEffects();
        if (nowHp <= 0) { stateMachine.ChangeState((int)EnemyState.Dead); }
        stateMachine.OnUpdate();
    }
    public override void OnAttackSet()
    {
        attackColliders.ForEach(c => c.enabled = false);

        var state = enemyAnimation.GetCurrentAnimatorStateInfo(0);
        foreach (var kv in colliderDict)
        {
            if (state.IsName(kv.Key))
            {
                kv.Value.enabled = true;
                break;
            }
        }
    }

    public override void OnAttackEnd() => attackColliders.ForEach(c => c.enabled = false);
    private void OnEffects()
    {
        var state = enemyAnimation.GetCurrentAnimatorStateInfo(0);
        foreach (var kvp in effectDict)
            kvp.Value.SetActive(state.IsName(kvp.Key) && state.normalizedTime < 1f);
    }
    public override void OnSumon()
    {
        for (int i = 0; i < mobEnemy.Length; i++)
        {
            if (mobEnemy[i] == null) continue; // nullチェック
            float angle = Random.Range(-90, 90);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = rot * transform.forward;
            Vector3 spawnPos = transform.position + dir.normalized * 5;
            Instantiate(mobEnemy[i], spawnPos, Quaternion.identity);
        }
    }


    private class IdleState : EStateMachine<BossEnemy>.StateBase
    {
        float cDis;
        public override void OnStart()
        {
            Owner.enemyAnimation.SetTrigger("Idle");
            cDis = Owner.lookPlayerDir;
        }
        public override void OnUpdate()
        {
            StateMachine.ChangeState((int)EnemyState.Chase);
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Idle");
        }
    }
    
    private class ChaseState : EStateMachine<BossEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        public override void OnStart()
        {
            Owner.enemyAnimation.SetTrigger("Idle");
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
        }
        public override void OnUpdate()
        {
            Vector3 playerPos = Owner.playerPos.transform.position;
            navMeshAgent.SetDestination(playerPos);
            if (Owner.GetDistance() <= Owner.attackRange)
            {
                navMeshAgent.isStopped = true;
                if (Probability(70)) { StateMachine.ChangeState((int)EnemyState.Combo); }
                if (Probability(30)) { StateMachine.ChangeState((int)EnemyState.Rotate); }
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Idle");
        }
    }
    private class VigilanceState : EStateMachine<BossEnemy>.StateBase
    {
        float time;
        float mTime;

        public float roamRadius = 5f;      // プレイヤーを中心とした円の半径
        public float roamChangeInterval = 2f; // ランダム位置を更新する間隔

        private Vector3 roamTarget;        // 今の円内ターゲット位置
        private float roamTimer;
        public override void OnStart()
        {
            Owner.navMeshAgent.isStopped = false;
            Owner.enemyAnimation.SetTrigger("Idle");
            time = 0;
            mTime = Random.Range(4, 6);
            PickNewRoamPosition();
        }
        public override void OnUpdate()
        {
            if (time > mTime)
            {
                time = 0;
                if (Probability(60)) { StateMachine.ChangeState((int)EnemyState.Sumon); }
                if (Probability(40)) { StateMachine.ChangeState((int)EnemyState.Rotate); }
            }
            time+= Time.deltaTime;

            float distance = Owner.GetDistance();

            if (distance < Owner.attackRange)
            {
                Vector3 dir = (Owner.transform.position - Owner.playerPos.transform.position).normalized;
                Vector3 retreatPos = Owner.playerPos.transform.position + dir * Owner.attackRange*2;
                Owner.navMeshAgent.SetDestination(retreatPos);
            }
            else
            {
                // ========================
                // 円内をランダムに回る
                // ========================
                roamTimer -= Time.deltaTime;
                if (roamTimer <= 0f)
                {
                    PickNewRoamPosition();
                }
                Owner.navMeshAgent.SetDestination(roamTarget);
            }

            Vector3 lookDir = Owner.playerPos.transform.position - Owner.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, Quaternion.LookRotation(lookDir), 0.1f);
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Idle");
        }
        void PickNewRoamPosition()
        {
            roamTimer = roamChangeInterval;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(0f, roamRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
            roamTarget = Owner.playerPos.transform.position + offset;
        }
    }

    private class ComboState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.transform.LookAt(Owner.playerPos.transform.position);
            Owner.enemyAnimation.SetTrigger("Combo");
            Owner.navMeshAgent.isStopped = true;   
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd("Combo"))
            {
                if (Owner.GetDistance() <= Owner.attackRange)
                {
                    if (Probability(30)) { StateMachine.ChangeState((int)EnemyState.Combo); }
                    if (Probability(50)) { StateMachine.ChangeState((int)EnemyState.Rotate); }
                    if (Probability(20)) { StateMachine.ChangeState((int)EnemyState.Vigilance); }
                }
                else
                {
                    if (Probability(20)) { StateMachine.ChangeState((int)EnemyState.Rush); }
                    if (Probability(30)) { StateMachine.ChangeState((int)EnemyState.Vigilance); }
                    if (Probability(50)) { StateMachine.ChangeState((int)EnemyState.Chase); }
                }
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Combo");
        }
    }
    private class RotateState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.navMeshAgent.isStopped = true;
            Owner.enemyAnimation.SetTrigger("Combo");
        }
        public override void OnUpdate()
        {
            if ((Owner.AnimationEnd("Combo") ))
            {
                if (Owner.GetDistance() <= Owner.attackRange)
                {
                    if (Probability(30)) { StateMachine.ChangeState((int)EnemyState.Combo); }
                    if(Probability(20)) { StateMachine.ChangeState((int)EnemyState.Rush); }
                    if(Probability(50)) { StateMachine.ChangeState((int)EnemyState.Vigilance); }
                }
                else
                {
                    if (Probability(50)) { StateMachine.ChangeState((int)EnemyState.Rush); }
                    if (Probability(50)) { StateMachine.ChangeState((int)EnemyState.Chase); }
                }
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Combo");
        }
    }
    private class SumonState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.navMeshAgent.isStopped = true;
            Owner.enemyAnimation.SetTrigger("Sumon");
            
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd("Sumon"))
            {
                if (Probability(20)) { StateMachine.ChangeState((int)EnemyState.Vigilance); }
                if (Probability(80))
                {
                    if(Owner.GetDistance() <= Owner.attackRange)
                    {
                        if (Probability(40)) { StateMachine.ChangeState((int)EnemyState.Rotate); }
                        if (Probability(60)) { StateMachine.ChangeState((int)EnemyState.Combo); }
                    }
                    else
                    {
                        StateMachine.ChangeState((int)EnemyState.Rush);
                    }
                }
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Sumon");
        }
    }
    private class RushState : EStateMachine<BossEnemy>.StateBase
    {
        float rayDistance = 1.5f;      // 前方判定距離
        float overshootDistance = 3.0f; // プレイヤーの奥まで行く距離
        Vector3 targetPos;

        public override void OnStart()
        {
            Owner.transform.LookAt(Owner.playerPos.transform.position);
            Owner.enemyAnimation.SetTrigger("Rush");
            Owner.navMeshAgent.isStopped = true;

            // プレイヤーの位置＋オフセットで目的地を設定
            //Vector3 direction = (Owner.playerPos.transform.position - Owner.transform.position).normalized;
            //targetPos = Owner.playerPos.transform.position + direction * overshootDistance;
        }

        public override void OnUpdate()
        {
            //Owner.transform.position = Vector3.MoveTowards(
            //    Owner.transform.position,
            //    targetPos,
            //    Owner.rushSpeed * Time.deltaTime
            //);
            //Ray ray = new Ray(Owner.transform.position + Vector3.up * 3f, Owner.transform.forward);
            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit, rayDistance))
            //{
            //    if (hit.collider.GetComponent<PlayerMovement>() == null &&
            //        hit.collider.GetComponent<Enemy>() == null)
            //    {
            //        StateMachine.ChangeState((int)EnemyState.Stiffness);
            //        return;
            //    }
            //}
            if (Owner.AnimationEnd("Rush"))
            {
                if (Vector3.Distance(Owner.transform.position, targetPos) < 0.1f){StateMachine.ChangeState((int)EnemyState.Vigilance);}
                else { StateMachine.ChangeState((int)EnemyState.Chase); }
            }
        }

        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Rush");
        }

    }
    private class StiffnessState : EStateMachine<BossEnemy>.StateBase
    {
        float time;
        public override void OnStart()
        {
            Owner.navMeshAgent.isStopped = true;
            Owner.enemyAnimation.SetTrigger("Idle");
            time = 0;
        }
        public override void OnUpdate()
        {
            if(time>= Owner.stiffnessTime) 
            {
                if (Probability(50)){ StateMachine.ChangeState((int)EnemyState.Vigilance);}
                if (Probability(50)) { StateMachine.ChangeState((int)EnemyState.Chase); }
                time = 0;
            }
            time += Time.deltaTime;
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Idle");
        }
    }

    private class HitState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.enemyAnimation.SetTrigger("Damage");
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd("")) { StateMachine.ChangeState((int)EnemyState.Idle); }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Damage");
        }
    }
    private class DeadState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.enemyAnimation.SetTrigger("Dead");
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd("Dead"))
            {
                Owner.OnDead();
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Dead");
        }
    }
    
}
