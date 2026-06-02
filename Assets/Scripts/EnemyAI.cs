using NUnit.Framework.Interfaces;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyAI : EnemyParent
{


    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource grognemment;
    [SerializeField] private Collider boxCollider;
    [SerializeField] private Collider boxColliderOfDeath;




    [Header("Stats")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private float attackDelay = 1f;
    [SerializeField] private float damageDealt = 10f;
    [SerializeField] private float rotationSpeed = 5f;


    [Header("Type Defense and Attack")]
    [SerializeField] protected DamageType damageType;


    [Header("Wandering Parameters")]
    [SerializeField] private float wanderingWaitTimeMin = 2f;
    [SerializeField] private float wanderingWaitTimeMax = 5f;
    [SerializeField] private float wanderingDistanceMin = 5f;
    [SerializeField] private float wanderingDistanceMax = 10f;

    private bool hasDestination;
    private bool beAttacked = false;

    [Header("Vision")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 60f;

    private void Start()
    {
        vie.SetActive(false);
    }
    private void Update()
    {
        if (playerStats == null)
        {
            playerStats = PlayerController.Instance;
            player = playerStats.transform;
        }
        if (IsDead || PlayerController.Instance.IsDead) return;


        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Vérifie si le joueur est visible
        if (distanceToPlayer < visionRange && angleToPlayer < visionAngle / 2f)
        {
            if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, visionRange))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    playerInSight = true;
                    beAttacked = false;
                }
            }
        }

        if (playerInSight || beAttacked)
        {
            ChasePlayer(distanceToPlayer);

            if (!beAttacked && distanceToPlayer > detectionRadius * 1.5f)
            {
                playerInSight = false;
            }
        }
        else
        {
            Wander();
        }


        if (animator == null)
            Debug.LogWarning("Animator not assigned in " + gameObject.name);
        if (agent == null)
            Debug.LogWarning("NavMeshAgent not assigned in " + gameObject.name);

        // Animation Speed
        float currentSpeed = agent.velocity.magnitude; // vitesse réelle en unités/sec
        float normalizedSpeed = Mathf.InverseLerp(0f, chaseSpeed, currentSpeed);
        animator.SetFloat("Speed", normalizedSpeed);

    }




    private void ChasePlayer(float distance)
    {
        agent.speed = chaseSpeed;

        if (!isAttacking)
        {
            // Rotation vers le joueur uniquement hors attaque
            Quaternion lookRotation = Quaternion.LookRotation(player.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }

        if (distance < attackRadius && !isAttacking)
        {
            StartCoroutine(AttackPlayer());
        }

        if (!agent.isStopped && !IsDead)
        {
            agent.SetDestination(player.position);
        }
    }

    private IEnumerator AttackPlayer()
    {
        if (IsDead) yield return null;
        isAttacking = true;
        agent.isStopped = true;

        // Rotation fluide mais rapide vers le joueur
        float t = 0f;
        float maxRotationTime = 0.25f; // Temps max pour se retourner (0.25s = rapide mais visible)

        Vector3 dirToPlayer = player.position - transform.position;
        dirToPlayer.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);

        while (t < maxRotationTime)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t / maxRotationTime);
            yield return null; // Attendre une frame
        }


        // Lance l'attaque immédiatement
        audioSource.Play();
        animator.SetTrigger("Attack");

        yield return null; // Laisse une frame

        DamageInfo info = new DamageInfo(10f, damageType, 0f, 0f, transform.root.gameObject);
        playerStats.DmgReceiver.TakeDamage(info);
        yield return new WaitForSeconds(attackDelay);
        if (agent.enabled)
            agent.isStopped = false;
        isAttacking = false;
    }


    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead) return;
        vie.SetActive(true);
        beAttacked = true;
        base.TakeDamage(damageInfo);
    }


    protected override void Die()
    {
        base.Die();
        IsDead = true;

        vie.SetActive(false);
        if (enemyData.enemyType == EnemyType.Squelette)
        {
            agent.isStopped = true;
            if(gameObject.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = true;
        }
        else
            agent.enabled = false;
        grognemment.Stop();
        if (boxCollider != null) boxCollider.enabled = false;
        if (boxColliderOfDeath != null) boxColliderOfDeath.enabled = true;

        if (itemToDrop != null)
        {
            itemToDrop.SetActive(true);
        }
        NewQuestManager.instance.UpdateQuestProgress(enemyData.enemyType.ToString(), 1);
    }

    #region Déplacement

    private void Wander()
    {
        agent.speed = walkSpeed;

        if (agent.remainingDistance < 0.75f && !hasDestination && !IsDead)
        {
            StartCoroutine(GetNewDestination());
        }
    }

    private IEnumerator GetNewDestination()
    {
        hasDestination = true;

        float waitTime = Random.Range(wanderingWaitTimeMin, wanderingWaitTimeMax);
        agent.speed = 0f;
        yield return new WaitForSeconds(waitTime);

        Vector3 randomDirection = Random.insideUnitSphere * Random.Range(wanderingDistanceMin, wanderingDistanceMax);
        randomDirection.y = 0f;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderingDistanceMax, NavMesh.AllAreas) && !IsDead)
        {
            agent.SetDestination(hit.position);
        }

        hasDestination = false;
    }

    #endregion
    public override void UpdateSpeedWitchCoefficient(float speedCoefficient)
    {
        walkSpeed *= speedCoefficient;
        chaseSpeed *= speedCoefficient;
    }

    protected override void UpdateLife()
    {
        base.UpdateLife();

        float ratio = currentHealth / enemyData.pvMax;


        healthBar.color = Color.Lerp(Color.red, Color.yellow, ratio);
    }



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Cône de vision
        Gizmos.color = Color.yellow;

        Vector3 leftRay = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward;

        Gizmos.DrawRay(transform.position, leftRay * visionRange);
        Gizmos.DrawRay(transform.position, rightRay * visionRange);
    }
}


