using UnityEngine;

public class EnemyHitState : EnemyState
{
    public EnemyHitState(EnemyControllerBase enemy) : base(enemy) { }
    public override void Enter()
    {
        // 1. On arrête les mouvements
        agent.isStopped = true;
        // 2. On joue l'animation de hit
        enemy.Animator.SetTrigger("Hit");
        // 3. On peut aussi ajouter un feedback visuel ou sonore ici
        Debug.Log($"{enemy.gameObject.name} a été touché !");
    }
    public override void Update() { }
    public override void Exit() { }
}