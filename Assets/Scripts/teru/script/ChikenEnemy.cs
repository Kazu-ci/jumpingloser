using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class ChikenEnemy : Enemy
{
    EStateMachine<ChikenEnemy> stateMachine;
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
        stateMachine = new EStateMachine<ChikenEnemy>(this);
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
    protected override void Update()
    {
        base.Update();
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
    private class IdleState : EStateMachine<ChikenEnemy>.StateBase
    {
        float cDis;
        public override void OnStart()
        {
            Owner.ChangeTexture(0);
            Owner.enemyAnimation.SetTrigger("Idle");
            Debug.Log("Idleだよ");
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
            Owner.enemyAnimation.ResetTrigger("Idle");
            Debug.Log("Idleは終わった");
        }
    }
    private class PatrolState : EStateMachine<ChikenEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        float cDis;
        Vector3 endPos;
        Vector3 startPos;
        bool goingToEnd = true;
        bool firstInit = true;
        public override void OnStart()
        {
            Owner.ChangeTexture(0);
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
            if (firstInit)
            {
                startPos = Owner.transform.position;
                endPos = Owner.GetRandomNavMeshPoint(startPos,7f);
                firstInit = false;
            }

            cDis = Owner.lookPlayerDir;
            Debug.Log("Patrolだよ");
        }
        public override void OnUpdate()
        {
            Owner.enemyAnimation.SetTrigger("Walk");
            float playerDis = Owner.GetDistance();
            var playerDir = Owner.playerPos.transform.position - Owner.transform.position;
            var angle = Vector3.Angle(Owner.transform.forward, playerDir);
            if (playerDis <= cDis && angle <= Owner.angle)            // プレイヤー検出
            {
                StateMachine.ChangeState((int)EnemyState.Chase);
                return;
            }
            // パトロール
            Vector3 targetPos = goingToEnd ? endPos : startPos;
            navMeshAgent.SetDestination(targetPos);
            // 到着判定
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                goingToEnd = !goingToEnd;
                StateMachine.ChangeState((int)EnemyState.Idle);
            }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Walk");
            Debug.Log("Patrolは終わった");
        }

    }
    private class ChaseState : EStateMachine<ChikenEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        public override void OnStart()
        {
            Owner.ChangeTexture(1);
            Owner.enemyAnimation.SetTrigger("Run");
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
            Debug.Log("Chaseだよ");
        }
        public override void OnUpdate()
        {
            if (Owner.GetDistance() <= Owner.attackRange)
            {
                StateMachine.ChangeState((int)EnemyState.Attack);
                navMeshAgent.isStopped = true;
            }
            if (Owner.GetDistance() >= Owner.lookPlayerDir) { StateMachine.ChangeState((int)EnemyState.Idle); }
            Vector3 playerPos = Owner.playerPos.transform.position;
            navMeshAgent.SetDestination(playerPos);
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Run");
            Debug.Log("Chaseは終わった");
        }
    }
    private class AttackState : EStateMachine<ChikenEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.enemyAnimation.SetTrigger("Attack");
            Debug.Log("Attackだよ");
        }
        public override void OnUpdate()
        {
            //GameObject game = Instantiate(Owner.efe);
            //game.transform.position = Owner.playerPos.transform.position;
            //StateMachine.ChangeState((int)EnemyState.AttackInterbal);
            /*if (Owner.GetDistance() > Owner.attackRange)
            {
                StateMachine.ChangeState((int)EnemyState.Patrol);
            }*/
            if (Owner.AnimationEnd()) { StateMachine.ChangeState((int)EnemyState.AttackInterbal); }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Attack");
            Debug.Log("Attackは終わった");
        }
    }
    private class AttackInterbalState : EStateMachine<ChikenEnemy>.StateBase
    {
        float time;
        public override void OnStart()
        {
            Owner.ChangeTexture(1);
            Owner.enemyAnimation.SetTrigger("Idle");
            Debug.Log("AttackInterbalだよ");
        }
        public override void OnUpdate()
        {
            time += Time.deltaTime;
            if (time > Owner.attackSpeed) { StateMachine.ChangeState((int)EnemyState.Idle); time = 0; }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Idle");
            Debug.Log("AttackInterbalは終わり");
        }
    }
    private class HitState : EStateMachine<ChikenEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.ChangeTexture(2);
            Debug.Log("Hitだよ");
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd()) { StateMachine.ChangeState((int)EnemyState.Idle);}
        }
        public override void OnEnd()
        {
            Debug.Log("Hitは終わり");
        }
    }
    private class DeadState : EStateMachine<ChikenEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.ChangeTexture(2);
            Debug.Log("Deadだよ");
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd())
            {
                Owner.OnDead();
            }
        }
        public override void OnEnd()
        {
            Debug.Log("Deadは終わり");
        }
    }
}
