using UnityEngine;

public class EnemyStunnedState : EnemyState
{
    private float stunDuration = 2f; // Durée de l'étourdissement
    private float stunTimer;
    public EnemyStunnedState(EnemyControllerBase enemy) : base(enemy) { }
    public override void Enter()
    {
        // 1. On arrête les mouvements
        agent.isStopped = true;
        // 2. On joue l'animation d'étourdissement
        enemy.Animator.SetTrigger("Stunned");
        // 3. On initialise le timer d'étourdissement
        stunTimer = stunDuration;
        Debug.Log($"{enemy.gameObject.name} est étourdi !");
    }
    public override void Update()
    {
        // On décrémente le timer d'étourdissement
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            // Une fois le timer écoulé, on retourne à l'état de patrouille
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
        }
    }
    public override void Exit() { }
}