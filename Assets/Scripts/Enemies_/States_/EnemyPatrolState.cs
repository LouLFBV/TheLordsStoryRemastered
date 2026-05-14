using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolState : EnemyState
{
    private Vector3 destination;
    private float waitTimer;
    private bool isWaiting;


    public EnemyPatrolState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.speed = enemy.enemyData.walkSpeed;
        agent.isStopped = false;
        isWaiting = false; // Reset important
        FindNewDestination();
    }

    public override void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                FindNewDestination();
            }
            return;
        }

        // Condition d'arrivée robuste
        // On vérifie si la distance restante est faible (0.5f est une bonne marge)
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                // Debug.Log("Destination atteinte, début de l'attente.");
                StartWaiting();
            }
        }

        // Mise à jour de l'animation avec la vélocité réelle
        // On utilise Mathf.Lerp pour éviter que l'anim ne saute
        float currentSpeed = agent.velocity.magnitude / agent.speed;
        enemy.Animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }

    private void FindNewDestination()
    {
        // On essaie de trouver un point valide jusqu'à 5 fois si nécessaire
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * enemy.enemyData.patrolRadius;

            // On s'assure d'une distance minimale pour éviter le surplace
            if (randomDirection.magnitude < 3f)
                randomDirection = randomDirection.normalized * 3f;

            randomDirection += enemy.transform.position;

            // On utilise un rayon de recherche un peu plus large (2f au lieu de patrolRadius) 
            // pour SamplePosition pour être sûr de trouver le sol
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                // Debug.Log("Nouveau point trouvé à l'essai n°" + (i + 1));
                return;
            }
        }

        // Si après 5 essais rien n'est trouvé, on attend un peu et on recommence
        // Debug.LogWarning("Aucun point NavMesh trouvé, l'ours attend.");
        StartWaiting();
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(enemy.enemyData.waitTimeMin, enemy.enemyData.waitTimeMax);
        agent.isStopped = true;
        enemy.Animator.SetFloat("Speed", 0f);
    }

    public override void Exit()
    {
        isWaiting = false;
        if (agent.isActiveAndEnabled)
            agent.isStopped = true;
    }
}