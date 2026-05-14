using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class BossAI2 : EnemyParent
{
    [Header("Boss Stats")]
    [SerializeField] private TextMeshProUGUI currentHealthText;
    [SerializeField] private float distanceToChasePlayer = 25f;

    [Header("Attacks")]
    [SerializeField] private BossAttack[] attacksBoss;

    [Header("Others")]
    private Collider basicCollider;
    [SerializeField] private GameObject colliderOfDeath;


    //[Header("Audio Settings")]
    private AudioSource audioSource;

    private bool playerDetected = false;
    private bool isDefending = false;
    private bool phase2 = false;
    private bool isScreaming = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        basicCollider = GetComponent<Collider>();
        if (colliderOfDeath != null ) 
            colliderOfDeath.SetActive(false);
        UpdateLife();

        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Add(vie);
        foreach (var attack in attacksBoss)
        {
            attack.hitBoxAttack.damageAmount = attack.damage;
            attack.hitBoxAttack.damageType = attack.damageType;
            attack.hitBoxAttack.isFireBreath = attack.isFireBreath;
        }
    }

    private void Update()
    {
        if (player == null) player = PlayerController.Instance.transform;
        if (IsDead || agent == null) return;
        //if (PlayerStats.instance.currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);


        if (!playerDetected && distanceToChasePlayer >= distanceToPlayer)
            playerDetected = true;

        if (playerDetected && !isScreaming)
            FacePlayer();

        foreach (var attack in attacksBoss)
        {
            PlayAttack(distanceToPlayer, attack);
        }

        if (isAttacking)
            agent.isStopped = true;
        else if (playerDetected)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }


    public override void TakeDamage(float damage, float poisedamage, DamageType damageType)
    {
        if (isDefending || isAttacking || IsDead)
            return;

        if (!playerDetected)
            playerDetected = true;
        foreach (DamageType type in defensePointFortType)
        {
            if (type == damageType)
            {
                damage *= (1f - pourcentageOfResistance);
                break;
            }
        }
        foreach (DamageType type in defensePointFaibleType)
        {
            if (type == damageType)
            {
                damage *= (1f + pourcentageOfResistance);
                break;
            }
        }

        currentHealth = (int)Mathf.Max(0, currentHealth - damage);
        UpdateLife();
        if (currentHealth <= enemyData.pvMax / 2 && !phase2)
        {
            phase2 = true;
            animator.SetTrigger("Scream");
            foreach (var attack in attacksBoss)
            {
                attack.damage = attack.damage + attack.boostDamage;
            }
        }
        else if (currentHealth <= 0f)
        {
            Die();
        }
        else if (currentHealth >= enemyData.pvMax / 2 && !phase2)
        {
            animator.SetTrigger("GetHit");
        }

    }

    private void PlayAttack(float distanceToPlayer, BossAttack bossAttack)
    {
        if (distanceToPlayer <= bossAttack.distanceMax && distanceToPlayer >= bossAttack.distanceMin && Time.time >= bossAttack.nextAttackTime && !isAttacking)
        {
            AttackPlayer(bossAttack.animTriggerName);
            bossAttack.nextAttackTime = Time.time + bossAttack.timeBetweenAttacks;
            Debug.Log($"Attaquer lancée : {bossAttack.animTriggerName}");
        }
    }
    private void AttackPlayer(string animTrigger)
    {
        animator.SetTrigger(animTrigger);
    }

    public void ActiveIsDefending() { isDefending = true; }

    public void DesactiveIsDefending() { isDefending = false; }

    public void ActiveIsAttacking()
    {
        isAttacking = true;
    }
    public void DesactiveIsAttacking()
    {
        isAttacking = false;
    }

    public void ActiveIsScreaming() { isScreaming =  true; }

    public void DesactiveIsScreaming() { isScreaming = false; }

    protected override void Die()
    {
        base.Die();
        IsDead = true;
        if (colliderOfDeath != null)
            colliderOfDeath.SetActive(true);
        basicCollider.enabled = false;
        agent.isStopped = true;
        NewQuestManager.instance.UpdateQuestProgress(enemyData.enemyType.ToString(), 1);
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.5f) // évite de surcorriger si le joueur est trop proche
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }

    public void ActiveFireBreath(int attackNumber)
    {
        int index = attackNumber - 1;
        if (index >= 0 && index < attacksBoss.Length)
            attacksBoss[index].gameObjectAttack.SetActive(true);
    }

    public void DesactiveFireBreath(int attackNumber)
    {
        int index = attackNumber - 1;
        if (index >= 0 && index < attacksBoss.Length)
            attacksBoss[index].gameObjectAttack.SetActive(false);
    }


    protected override void UpdateLife()
    {
        base.UpdateLife();
        currentHealthText.text = currentHealth.ToString() + "/" + enemyData.pvMax.ToString();
    }
    public override void UpdateSpeedWitchCoefficient(float speedCoefficient)
    {
        agent.velocity = agent.velocity * speedCoefficient;
    }
    private void OnDrawGizmosSelected()
    {
        if (attacksBoss == null) return;

        Color[] gizmoColors = { Color.red, Color.blue, Color.yellow, new Color(0.6f, 0.3f, 0.1f), Color.magenta };

        for (int i = 0; i < attacksBoss.Length; i++)
        {
            if (attacksBoss[i] != null)
            {
                Gizmos.color = gizmoColors[i % gizmoColors.Length];
                Gizmos.DrawWireSphere(transform.position, attacksBoss[i].distanceMin);

                Gizmos.DrawWireSphere(transform.position, attacksBoss[i].distanceMax);
            }
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanceToChasePlayer);
    }
}


