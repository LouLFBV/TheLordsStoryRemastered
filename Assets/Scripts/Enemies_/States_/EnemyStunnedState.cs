using UnityEngine;

public class EnemyStunnedState : EnemyState
{
    private float _stunDuration;
    private float _timer;

    public EnemyStunnedState(EnemyControllerBase enemy) : base(enemy) { }

    // On passe la durée du stun de façon dynamique lors du changement d'état
    public void SetDuration(float duration) => _stunDuration = duration;

    public override void Enter()
    {
        _timer = 0f;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        enemy.Poise.ResetPoise(); 
        if (enemy.AIManager.HasStunnedAnim)
            enemy.Animator.SetTrigger("Stunned"); // Assure-toi d'avoir un trigger "Stunned" dans ton Animator
        else
            enemy.Animator.speed = 0f; // Si pas d'anim spécifique, on freeze l'animation actuelle pour simuler le stun

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
        Debug.Log($"{enemy.gameObject.name} est PARALYSÉ !");
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _stunDuration)
        {
            // Une fois le temps écoulé, on le renvoie en Chase ou Idle
            enemy.StateMachine.ChangeState(EnemyStateType.Follow);
        }
    }

    public override void Exit()
    {
        enemy.Animator.speed = 1f; // On remet la vitesse d'animation à la normale
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }
}