using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class NinzinEnemy : Enemy
{
    EStateMachine<NinzinEnemy> stateMachine;
    [SerializeField] GameObject efe;
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
        stateMachine = new EStateMachine<NinzinEnemy>(this);
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
    private class IdleState : EStateMachine<NinzinEnemy>.StateBase
    {
        float cDis;
        public override void OnStart()
        {
            Owner.ChangeTexture(0);
            Debug.Log("Idleだよ");
            Owner.enemyAnimation.SetTrigger("Idle");
            cDis = Owner.lookPlayerDir;
        }
        public override void OnUpdate()
        {
                float playerDis = Owner.GetDistance();
                if (playerDis <= cDis) { StateMachine.ChangeState((int)EnemyState.Chase); }
                else { StateMachine.ChangeState((int)EnemyState.Patrol); }
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Idle");
            Debug.Log("Idleは終わった");
        }
    }
    private class PatrolState : EStateMachine<NinzinEnemy>.StateBase
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
                endPos = Owner.GetRandomNavMeshPoint(startPos, 7f);
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
            if (playerDis <= cDis && angle <= Owner.angle)            
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
            Debug.Log("Patrolは終わった");
            Owner.enemyAnimation.ResetTrigger("Walk");
        }
    }
    private class ChaseState : EStateMachine<NinzinEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        public override void OnStart()
        {
            Owner.ChangeTexture(1);
            Owner.enemyAnimation.SetTrigger("Walk");
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
            Owner.enemyAnimation.ResetTrigger("Walk");
            Debug.Log("Chaseは終わった");
        }
    }
    private class AttackState : EStateMachine<NinzinEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.ChangeTexture(1);
            Debug.Log("Attackだよ");
            Owner.enemyAnimation.SetTrigger("Attack");
            Owner.navMeshAgent.isStopped = true;
        }
        public override void OnUpdate()
        {
            //GameObject game = Instantiate(Owner.efe);
            //game.transform.position = Owner.playerPos.transform.position;
            if (Owner.AnimationEnd()) { StateMachine.ChangeState((int)EnemyState.AttackInterbal); }
            /*if (Owner.GetDistance() > Owner.attackRange)
            {
                StateMachine.ChangeState((int)EnemyState.Patrol);
            }*/
        }
        public override void OnEnd()
        {
            Owner.enemyAnimation.ResetTrigger("Attack");
            Debug.Log("Attackは終わった");
        }
    }
    private class AttackInterbalState : EStateMachine<NinzinEnemy>.StateBase
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
    private class HitState : EStateMachine<NinzinEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.ChangeTexture(2);
            Debug.Log("Hitだよ");
        }
        public override void OnUpdate()
        {
            if (Owner.AnimationEnd()) { StateMachine.ChangeState((int)EnemyState.Idle); }
        }
        public override void OnEnd()
        {
            Debug.Log("Hitは終わり");
        }
    }
    private class DeadState : EStateMachine<NinzinEnemy>.StateBase
    {
        public override void OnStart()
        {
            Owner.ChangeTexture(2);
            Debug.Log("Deadだよ");
            Owner.enemyAnimation.SetTrigger("Dead");
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
