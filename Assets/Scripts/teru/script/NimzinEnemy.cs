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
    void Update()
    {
        stateMachine.OnUpdate();
    }
    private class IdleState : EStateMachine<NinzinEnemy>.StateBase
    {
        float cDis;
        public override void OnStart()
        {
            Debug.Log("Idleだよ");
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
            Debug.Log("Idleは終わった");
        }
    }
    private class PatrolState : EStateMachine<NinzinEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        float cDis;
        public override void OnStart()
        {
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
            cDis = Owner.lookPlayerDir;
            Debug.Log("Patrolだよ");
        }
        public override void OnUpdate()
        {
            float playerDis = Owner.GetDistance();
            if (playerDis <= cDis) { StateMachine.ChangeState((int)EnemyState.Chase); }
            Vector3 currentPos = Owner.transform.position;
            Vector3 randomDestination = currentPos + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
            navMeshAgent.SetDestination(randomDestination);
        }
        public override void OnEnd()
        {
            Debug.Log("Patrolは終わった");
        }
    }
    private class ChaseState : EStateMachine<NinzinEnemy>.StateBase
    {
        NavMeshAgent navMeshAgent;
        public override void OnStart()
        {
            navMeshAgent = Owner.navMeshAgent;
            navMeshAgent.isStopped = false;
            Debug.Log("Chaseだよ");
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
            Debug.Log("Chaseは終わった");
        }
    }
    private class AttackState : EStateMachine<NinzinEnemy>.StateBase
    {
        public override void OnStart()
        {
            Debug.Log("Attackだよ");

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
            Debug.Log("Attackは終わった");
        }
    }
    private class AttackInterbalState : EStateMachine<NinzinEnemy>.StateBase
    {
        float time;
        public override void OnStart()
        {
            Debug.Log("AttackInterbalだよ");
        }
        public override void OnUpdate()
        {
            time += Time.deltaTime;
            if (time > Owner.maxSpeed) { StateMachine.ChangeState((int)EnemyState.Idle); time = 0; }
        }
        public override void OnEnd()
        {
            Debug.Log("AttackInterbalは終わり");
        }
    }
    private class HitState : EStateMachine<NinzinEnemy>.StateBase { }
    private class DeadState : EStateMachine<NinzinEnemy>.StateBase { }
}
