public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.isStopped = true;
        enemy.Animator.SetFloat("Speed", 0);
        enemy.HasAggroedOnce = false;
        enemy.target = null;
        if (enemy.enemyData != null && enemy.enemyData.idleSound != null)
        {
            enemy.ChangeLoopingSound(enemy.enemyData.idleSound);
        }
    }

    public override void Update() { } // Le changement vers Follow est géré par le Controller pour l'instant

    public override void Exit() { }
}