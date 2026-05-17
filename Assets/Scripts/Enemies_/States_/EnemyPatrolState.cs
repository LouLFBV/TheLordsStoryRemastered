using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolState : EnemyState
{
    private Vector3 destination;
    private float waitTimer;
    private bool isWaiting;

    // L'ancre de patrouille fixe par défaut
    private Vector3 spawnPosition;
    private bool hasSpawnPosition = false;

    public EnemyPatrolState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.speed = enemy.enemyData.walkSpeed;
        agent.isStopped = false;
        isWaiting = false;

        // On mémorise sa position de départ UNIQUE une bonne fois pour toutes
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

        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                StartWaiting();
            }
        }

        float currentSpeed = agent.velocity.magnitude / agent.speed;
        enemy.Animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }

    private void FindNewDestination()
    {
        for (int i = 0; i < 5; i++)
        {
            // 1. On génère le décalage aléatoire
            Vector3 randomDirection = Random.insideUnitSphere * enemy.enemyData.patrolRadius;
            randomDirection.y = 0; // On force sur un plan horizontal pour le NavMesh

            // Sécurité anti-surplace
            if (randomDirection.magnitude < 3f)
                randomDirection = randomDirection.normalized * 3f;

            // 2. LOGIQUE DE CENTRE UNIQUE : 
            // Si tu as mis un point précis dans l'inspecteur (patrolCenterPoint), on l'utilise.
            // Sinon, on utilise la position où il a spawn (spawnPosition).
            Vector3 finalCenter = enemy.patrolCenterPoint != null ? enemy.patrolCenterPoint.position : spawnPosition;

            // 3. On applique le centre choisi
            randomDirection += finalCenter;

            // 4. Test sur le NavMesh
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return;
            }
        }

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