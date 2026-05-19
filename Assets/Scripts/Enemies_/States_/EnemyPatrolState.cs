using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolState : EnemyState
{
    private Vector3 destination;
    private float waitTimer;
    private bool isWaiting;

    private Vector3 spawnPosition;
    private bool hasSpawnPosition = false;

    public EnemyPatrolState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.speed = enemy.enemyData.walkSpeed;
        agent.isStopped = false;
        isWaiting = false;

        if (!hasSpawnPosition)
        {
            spawnPosition = enemy.transform.position;
            hasSpawnPosition = true;
        }

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

        // Si l'agent calcule encore son chemin
        if (agent.pathPending)
        {
            // Log très verbeux (optionnel, décommente si besoin)
            // Debug.Log($"<color=yellow>[PATROL]</color> {enemy.gameObject.name} attend le calcul du pathfinding...");
            return;
        }

        // Maintenant que le chemin est prêt, on vérifie la distance de manière fiable
        if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            StartWaiting();
            return;
        }

        // On met à jour l'animation avec la vitesse réelle calculée
        float currentSpeed = agent.velocity.magnitude / agent.speed;
        enemy.Animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }

    private void FindNewDestination()
    {

        for (int i = 0; i < 5; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * enemy.enemyData.patrolRadius;
            randomDirection.y = 0;

            if (randomDirection.magnitude < 3f)
                randomDirection = randomDirection.normalized * 3f;

            Vector3 finalCenter = enemy.patrolCenterPoint != null ? enemy.patrolCenterPoint.position : spawnPosition;
            randomDirection += finalCenter;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                destination = hit.position;
                agent.isStopped = false;
                agent.SetDestination(destination);
                return;
            }
        }

        // Si on arrive ici, c'est que les 5 essais ont échoué à trouver un sol NavMesh valide
        StartWaiting();
    }

    private void StartWaiting()
    {
        isWaiting = true;
        float chosenWaitTime = Random.Range(enemy.enemyData.waitTimeMin, enemy.enemyData.waitTimeMax);
        waitTimer = chosenWaitTime;

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