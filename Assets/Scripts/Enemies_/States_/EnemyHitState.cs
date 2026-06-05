using UnityEngine;

public class EnemyHitState : EnemyState
{
    private float hitDuration = 0.4f;
    private float timer;
    public EnemyHitState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        timer = 0f;

        // 1. On arrête les mouvements
        agent.isStopped = true;

        // 2. On joue l'animation de hit
        enemy.Animator.SetTrigger("Hit");
        Debug.Log($"<color=red>[ENEMY HIT]</color> {enemy.gameObject.name} est dans l'état Hit. Animation déclenchée.");

        // 3. Feedback visuel et sonore
        Debug.Log($"{enemy.gameObject.name} a été touché !");

        AudioSource source = enemy.GetComponent<AudioSource>();

        if (source != null && enemy.enemyData != null)
        {
            AudioClip[] hitSounds = enemy.enemyData.hitSound;

            if (hitSounds != null && hitSounds.Length > 0)
            {
                int randomIndex = Random.Range(0, hitSounds.Length);
                AudioClip chosenSound = hitSounds[randomIndex];

                if (chosenSound != null)
                {
                    source.Stop();
                    source.loop = false;
                    source.PlayOneShot(chosenSound);
                }
            }
        }
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        // Si l'ennemi meurt pendant qu'il encaisse le coup (ex: dégât de poison ou autre source)
        if (enemy.Health.CurrentHealth <= 0)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Death);
            return;
        }

        if (timer >= hitDuration && enemy.Health.CurrentHealth > 0)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Follow);
        }
    }

    public override void Exit()
    {
        enemy.Agent.isStopped = false;
    }
}