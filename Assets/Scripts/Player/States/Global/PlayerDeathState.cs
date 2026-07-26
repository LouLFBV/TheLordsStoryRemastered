using UnityEngine;

public class PlayerDeathState : PlayerState
{
    public PlayerDeathState(PlayerController player) : base(player) {}

    public override void Enter()
    {
        // 1. On arrête les mouvements

        // 2. On lance l'animation de mort
        player.Animator.SetBool("IsStunned", false);
        player.Animator.SetTrigger("Die");
        player.IsDead = true;
        Debug.Log($"{player.gameObject.name} est mort.");
    }
    public override void Exit() 
    {
        base.Exit(); 
    }
}

