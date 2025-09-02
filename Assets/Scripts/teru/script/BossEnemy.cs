using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class BossEnemy : Enemy
{
    EStateMachine<BossEnemy> stateMachine;
    [SerializeField] GameObject efe;
    [SerializeField] Collider attackCollider;
    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        AttackInterbal,
        Hit,
        Dead
    }
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        nowHp = maxHp;
        stateMachine = new EStateMachine<BossEnemy>(this);
        stateMachine.Add<IdleState>((int)EnemyState.Idle);
        stateMachine.Add<PatrolState>((int)EnemyState.Patrol);
        stateMachine.Add<ChaseState>((int)EnemyState.Chase);
        stateMachine.Add<AttackState>((int)EnemyState.Attack);
        stateMachine.Add<AttackInterbalState>((int)EnemyState.AttackInterbal);
        stateMachine.Add<HitState>((int)EnemyState.Hit);
        stateMachine.Add<DeadState>((int)EnemyState.Dead);
        stateMachine.OnStart((int)EnemyState.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        stateMachine.OnUpdate();
    }
    public override void OnAttackSet()
    {
        attackCollider.enabled = true;
    }
    public override void OnAttackEnd()
    {
        attackCollider.enabled = false;
    }
    private class IdleState : EStateMachine<BossEnemy>.StateBase
    {
        float cDis;
        public override void OnStart()
        {
            Debug.Log("Idle����");
            cDis = Owner.lookPlayerDir;
        }
        public override void OnUpdate()
        {
            float playerDis = Owner.GetDistance();
            var playerDir = Owner.playerPos.transform.position - Owner.transform.position;
            var angle = Vector3.Angle(Owner.transform.forward, playerDir);
            if (playerDis <= cDis && angle <= Owner.angle) { StateMachine.ChangeState((int)EnemyState.Chase); }
            else { StateMachine.ChangeState((int)EnemyState.Patrol); }
        }
        public override void OnEnd()
        {
            Debug.Log("Idle�͏I�����");
        }
    }
    private class PatrolState : EStateMachine<BossEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        float cDis;
        Vector3 endPos;
        Vector3 startPos;
        bool goingToEnd = true;
        public override void OnStart()
        {
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
            if (startPos == Vector3.zero && endPos == Vector3.zero)
            {
                startPos = Owner.transform.position;
                endPos = new Vector3(
                    Random.Range(startPos.x - 5, startPos.x + 5),
                    0,
                    Random.Range(startPos.z - 5, startPos.z + 5)
                );
            }
            cDis = Owner.lookPlayerDir;
            Debug.Log("Patrol����");
        }
        public override void OnUpdate()
        {
            float playerDis = Owner.GetDistance();
            var playerDir = Owner.playerPos.transform.position - Owner.transform.position;
            var angle = Vector3.Angle(Owner.transform.forward, playerDir);
            if (playerDis <= cDis && angle <= Owner.angle) { StateMachine.ChangeState((int)EnemyState.Chase); }
            Vector3 targetPos = goingToEnd ? endPos : startPos;
            navMeshAgent.SetDestination(targetPos);
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= 0.5f)
            {
                goingToEnd = !goingToEnd;
                StateMachine.ChangeState((int)EnemyState.Idle);
            }
        }
        public override void OnEnd()
        {
            Debug.Log("Patrol�͏I�����");
        }

    }
    private class ChaseState : EStateMachine<BossEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        public override void OnStart()
        {
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
            Debug.Log("Chase����");
        }
        public override void OnUpdate()
        {
            Vector3 playerPos = Owner.playerPos.transform.position;
            navMeshAgent.SetDestination(playerPos);
            if (Owner.GetDistance() <= Owner.attackRange)
            {
                StateMachine.ChangeState((int)EnemyState.Attack);
                navMeshAgent.isStopped = true;
            }


        }
        public override void OnEnd()
        {
            Debug.Log("Chase�͏I�����");
        }
    }
    private class AttackState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Debug.Log("Attack����");

        }
        public override void OnUpdate()
        {
            GameObject game = Instantiate(Owner.efe);
            game.transform.position = Owner.playerPos.transform.position;
            StateMachine.ChangeState((int)EnemyState.AttackInterbal);
            /*if (Owner.GetDistance() > Owner.attackRange)
            {
                StateMachine.ChangeState((int)EnemyState.Patrol);
            }*/
        }
        public override void OnEnd()
        {
            Debug.Log("Attack�͏I�����");
        }
    }
    private class AttackInterbalState : EStateMachine<BossEnemy>.StateBase
    {
        float time;
        public override void OnStart()
        {
            Debug.Log("AttackInterbal����");
        }
        public override void OnUpdate()
        {
            time += Time.deltaTime;
            if (time > Owner.maxSpeed) { StateMachine.ChangeState((int)EnemyState.Idle); time = 0; }
        }
        public override void OnEnd()
        {
            Debug.Log("AttackInterbal�͏I���");
        }
    }
    private class HitState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Debug.Log("Hit����");
        }
        public override void OnUpdate()
        {
            if (Owner.animationEnd()) { StateMachine.ChangeState((int)EnemyState.Idle); }
        }
        public override void OnEnd()
        {
            Debug.Log("Hit�͏I���");
        }
    }
    private class DeadState : EStateMachine<BossEnemy>.StateBase
    {
        public override void OnStart()
        {
            Debug.Log("Dead����");
        }
        public override void OnUpdate()
        {
            if (Owner.animationEnd())
            {
                Owner.OnDead();
            }
        }
        public override void OnEnd()
        {
            Debug.Log("Dead�͏I���");
        }
    }
}
