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

        AudioSource source = enemy.GetComponent<AudioSource>();
        if (source != null && enemy.enemyData != null)
        {
            source.Stop(); // On coupe la boucle d'idle/run en cours
            source.loop = false; // On désactive le mode boucle
            source.PlayOneShot(enemy.enemyData.hitSound); // On joue le cri de douleur
        }

    }
    public override void Update() { }
    public override void Exit() { }
}