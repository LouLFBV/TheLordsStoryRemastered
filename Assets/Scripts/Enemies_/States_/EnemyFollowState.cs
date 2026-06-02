using UnityEngine;

public class EnemyFollowState : EnemyState
{
    public EnemyFollowState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.speed = enemy.enemyData.chaseSpeed;
        agent.isStopped = false;

        if (enemy.enemyData != null && enemy.enemyData.runSound != null)
        {
            enemy.ChangeLoopingSound(enemy.enemyData.runSound);
        }
    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);

        // On récupère le modificateur depuis le DamageReceiver de l'ennemi
        float currentSlow = enemy.DmgReceiver.SpeedModifier;

        // On applique la vitesse de course modulée par le gel
        agent.speed = enemy.enemyData.chaseSpeed * currentSlow;

        // On adapte aussi la vitesse de l'animation pour éviter l'effet de glissade au sol !
        enemy.Animator.speed = currentSlow;

        // on considère qu'il a engagé le combat proprement.
        if (!enemy.HasAggroedOnce && distance <= enemy.enemyData.visionRange)
        {
            enemy.HasAggroedOnce = true;
            Debug.Log($"<color=orange>[AGGRO]</color> {enemy.gameObject.name} a atteint le joueur. Mode normal activé.");
        }

        AttackSO ready = enemy.PeekBestAttack();
        if (ready != null)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Attack);
            return;
        }

        if (distance <= agent.stoppingDistance + 0.5f && ready == null)
        {
            // Debug.LogWarning("[FOLLOW] Au contact mais aucune attaque possible...");
        }

        if (enemy.AIManager.HasPermission(EnemyStateType.Orbit) && distance <= enemy.AIManager.OrbitDistance + 2f)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Orbit);
            return;
        }

        if (enemy.HasAggroedOnce && distance > enemy.enemyData.visionRange * 1.5f)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
            return;
        }

        // Si on n'est pas en orbite, on fonce !
        agent.SetDestination(enemy.target.position);

        // Animation
        float speedParameter = agent.velocity.magnitude / enemy.enemyData.chaseSpeed;
        enemy.Animator.SetFloat("Speed", speedParameter, 0.1f, Time.deltaTime);
    }

    public override void Exit()
    {
        if (agent.isActiveAndEnabled)
            agent.isStopped = true;

        if (enemy.Animator != null)
            enemy.Animator.speed = 1f;
    }
}