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

        // 3. Feedback visuel et sonore
        Debug.Log($"{enemy.gameObject.name} a été touché !");

        AudioSource source = enemy.GetComponent<AudioSource>();

        if (source != null && enemy.enemyData != null)
        {
            // On récupère le tableau de sons depuis le ScriptableObject
            AudioClip[] hitSounds = enemy.enemyData.hitSound;

            if (hitSounds != null && hitSounds.Length > 0)
            {
                // On tire un index au hasard entre 0 et la taille du tableau (exclu)
                int randomIndex = Random.Range(0, hitSounds.Length);
                AudioClip chosenSound = hitSounds[randomIndex];

                if (chosenSound != null)
                {
                    source.Stop(); // On coupe le son en cours (pas, idle...)
                    source.loop = false; // Pas de boucle pour un cri de douleur

                    // On joue le son sélectionné aléatoirement
                    source.PlayOneShot(chosenSound);
                }
            }
        }
    }

    public override void Update() { }
    public override void Exit() { }
}