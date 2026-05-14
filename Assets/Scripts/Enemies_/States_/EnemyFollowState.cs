using UnityEngine;

public class EnemyFollowState : EnemyState
{
    public EnemyFollowState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.speed = enemy.enemyData.chaseSpeed;
        agent.isStopped = false;

    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);




        AttackSO ready = enemy.PeekBestAttack();
        if (ready != null)
        {
            //Debug.Log($"<color=green>[FOLLOW]</color> Cible à portée ({distance:F2}m). Transition vers Attack.");
            enemy.StateMachine.ChangeState(EnemyStateType.Attack);
            return;
        }

        // Si on est déjà au contact (agent.stoppingDistance) mais que Peek renvoie null
        // c'est que les AttackSO sont mal réglés (minDistance trop haute)
        if (distance <= agent.stoppingDistance + 0.5f && ready == null)
        {
            //Debug.LogWarning("[FOLLOW] Au contact mais aucune attaque possible. Vérifiez les ranges/cooldowns des SO.");
        }

        if (enemy.AIManager.HasPermission(EnemyStateType.Orbit) && distance <= enemy.AIManager.OrbitDistance + 2f)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Orbit);
            return;
        } 

        // 2. Si le joueur s'est trop éloigné
        if (distance > enemy.enemyData.visionRange * 1.5f)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
            return;
        }

        // 4. Si on n'est pas en orbite (Squelette ou trop loin), on fonce !
        agent.SetDestination(enemy.target.position);

        // Animation
        enemy.Animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed, 0.1f, Time.deltaTime);
    }

    public override void Exit()
    {
        // On arrête l'agent proprement en quittant l'état
        if (agent.isActiveAndEnabled)
            agent.isStopped = true;
    }
}