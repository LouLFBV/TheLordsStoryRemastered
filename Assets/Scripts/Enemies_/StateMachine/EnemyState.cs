public abstract class EnemyState : State
{
    protected EnemyControllerBase enemy;
    protected UnityEngine.AI.NavMeshAgent agent; // Petit bonus confort

    protected EnemyState(EnemyControllerBase enemy)
    {
        this.enemy = enemy;
        this.agent = enemy.Agent;
    }
}