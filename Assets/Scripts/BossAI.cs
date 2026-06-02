using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : EnemyParent
{
    [Header("Boss Stats")]
    [SerializeField] private TextMeshProUGUI currentHealthText;
    [SerializeField] private float distanceToChasePlayer = 25f;

    [Header("Attacks")]
    [SerializeField] private BossAttack[] attacksBoss;

    [Header("Others")]
    private Collider basicCollider;
    [SerializeField] private GameObject colliderOfDeath;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] phaseMusic;
    private Coroutine phaseMusicRoutine;
    [SerializeField] private float fadeDuration = 1.2f;
    private float baseVolume;

    [Header("Systems")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private DamageReceiver damageReceiver;

    private bool playerDetected = false;
    private bool isDefending = false;
    private bool phase2 = false;
    private bool isScreaming = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        basicCollider = GetComponent<Collider>();
        colliderOfDeath.SetActive(false);
        UpdateLife();

        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Add(vie);

        baseVolume = audioSource.volume;
        foreach (var attack in attacksBoss)
        {
            attack.hitBoxAttack.damageAmount = attack.damage;
            attack.hitBoxAttack.damageType = attack.damageType;
            attack.hitBoxAttack.isFireBreath = attack.isFireBreath;
        }
        PlayPhase1Music();
    }

    private void Update()
    {
        if (player == null) player = PlayerController.Instance.transform;
        if (IsDead || agent == null) return;
        if (PlayerController.Instance.IsDead) return;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);


        if (!playerDetected && distanceToChasePlayer >= distanceToPlayer)
            playerDetected = true;

        if (playerDetected && !isScreaming) 
            FacePlayer();

        foreach (var attack in attacksBoss)
        {
            PlayAttack(distanceToPlayer, attack);
        }

        if(isAttacking)
            agent.isStopped = true;
        else if (playerDetected)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }


    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (isDefending || isAttacking || IsDead)
            return;

        if (!playerDetected)
            playerDetected = true;
        foreach (DamageType type in defensePointFortType)
        {
            if (type == damageInfo.physicalType)
            {
                damageInfo.rawPhysicalDamage *= (1f - pourcentageOfResistance);
                break;
            }
        }
        foreach (DamageType type in defensePointFaibleType)
        {
            if (type == damageInfo.physicalType)
            {
                damageInfo.rawPhysicalDamage *= (1f + pourcentageOfResistance);
                break;
            }
        }

        currentHealth = (int)Mathf.Max(0, currentHealth - damageInfo.rawPhysicalDamage);
        UpdateLife();
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        if (currentHealth <= enemyData.pvMax / 2 && !phase2)
        {
            phase2 = true;
            animator.SetTrigger("Scream");

            if (phaseMusicRoutine != null)
                StopCoroutine(phaseMusicRoutine);

            phaseMusicRoutine = StartCoroutine(PlayPhase2MusicSequence());

            foreach (var attack in attacksBoss)
            {
                attack.damage += attack.boostDamage;
            }
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
    #region Animator
    private void AttackPlayer(string animTrigger) => animator.SetTrigger(animTrigger);
    

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


    public void ActiveIsScreaming() { isScreaming = true; }

    public void DesactiveIsScreaming() { isScreaming = false; }

    #endregion
    protected override void Die()
    {
        base.Die();
        IsDead = true;
        basicCollider.enabled = false;
        if (phaseMusicRoutine != null)
            StopCoroutine(phaseMusicRoutine);

        StartCoroutine(FadeAndStop());


        if (colliderOfDeath != null)
            colliderOfDeath.SetActive(true);
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

    public override void UpdateSpeedWitchCoefficient(float speedCoefficient)
    {
        agent.velocity = agent.velocity * speedCoefficient;
    }
    protected override void UpdateLife()
    {
        base.UpdateLife();
        currentHealthText.text = currentHealth.ToString()+"/"+enemyData.pvMax.ToString();
    }

    private void PlayPhase1Music()
    {
        if (phaseMusic.Length < 1 || phaseMusic[0] == null) return;

        audioSource.clip = phaseMusic[0];
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.Play();

        StartCoroutine(FadeVolume(0f, baseVolume, fadeDuration));
    }


    private IEnumerator PlayPhase2MusicSequence()
    {
        if (phaseMusic.Length < 2) yield break;

        // --- Fade Out Phase 1 ---
        yield return StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeDuration));

        if (IsDead) yield break;

        // --- Phase 2 ---
        audioSource.clip = phaseMusic[1];
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.Play();

        yield return StartCoroutine(FadeVolume(0f, baseVolume, fadeDuration));
    }


    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }

        audioSource.volume = to;
    }

    private IEnumerator FadeAndStop()
    {
        yield return StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeDuration));
        audioSource.Stop();
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


