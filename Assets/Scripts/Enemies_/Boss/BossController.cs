using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BossController : EnemyControllerBase
{
    [Header("Boss UI & Audio")]
    [SerializeField] private AudioSource bossAudioSource;
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("Phase Management")]
    [SerializeField] private List<BossPhase> phases;
    private int currentPhaseIndex = 0;
    private float baseMusicVolume;

    [Header("States")]
    public BossAttackState AttackState { get; private set; }


    [Header("VFX / Attack GameObjects")]
    private GameObject _currentActiveVisual;

    protected override void Awake()
    {
        base.Awake();
        // Dans BossController.cs (au moment d'écraser ou de setup le dictionnaire de la State Machine) :
        AttackState = new BossAttackState(this); // Si ta variable AttackState est protected/public dans la base

        // Et dans ton dictionnaire d'états du Boss, assure-toi d'associer :
        StateMachine.AddState(EnemyStateType.Attack, new BossAttackState(this));
        baseMusicVolume = bossAudioSource.volume;
    }

    protected override void Start()
    {
        base.Start();
        PlayPhaseMusic(0);
    }

    protected override void Update()
    {
        base.Update();
        CheckPhaseTransitions();
    }

    private void CheckPhaseTransitions()
    {
        // On vérifie s'il reste une phase suivante et si le seuil de PV est atteint
        if (currentPhaseIndex + 1 < phases.Count)
        {
            float healthPercent = Health.CurrentHealth / Health.MaxHealth;
            if (healthPercent <= phases[currentPhaseIndex + 1].healthThreshold)
            {
                TriggerNextPhase();
            }
        }
    }

    private void TriggerNextPhase()
    {
        currentPhaseIndex++;
        BossPhase newPhase = phases[currentPhaseIndex];

        if (newPhase.gameObjectEvent != null)
        {
            newPhase.gameObjectEvent.SetActive(true);
            Debug.Log($"GameObject event triggered for phase: {newPhase.phaseName}");
        }
        // 1. Animation & Son de cri
        Animator.SetTrigger(newPhase.screamAnimationTrigger);

        // 2. Musique avec transition fluide
        if (newPhase.phaseMusic != null)
            StartCoroutine(FadeMusicSequence(newPhase.phaseMusic));

        // 3. Nouvelles attaques
        foreach (var attack in newPhase.newAttacks)
        {
            if (attack != null && !availableAttacks.Contains(attack))
            {
                availableAttacks.Add(attack);
                Debug.Log($"New attack added: {attack.animationName}");
            }
        }

        // 4. Boost physique
        BoostBossStats(newPhase);
    }


    // On override CheckForPlayer pour utiliser ta distance spécifique
    protected override void CheckForPlayer()
    {
        Debug.Log("[Boss] Checking for player...");

        if (target == null && PlayerController.Instance != null)
            target = PlayerController.Instance.transform;
        if (target == null) return;
        if (enemyData == null)
            enemyData = AIManager.GetData();

        float distance = Vector3.Distance(transform.position, target.position);

        Vector3 dirToPlayer = (target.position - transform.position).normalized;

        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        if ((distance <= enemyData.visionRange && angleToPlayer < enemyData.visionAngle / 2f) || distance <= enemyData.detectionRange)
        {
            if (StateMachine.CurrentState == IdleState || StateMachine.CurrentState == PatrolState)
            {
                StateMachine.ChangeState(EnemyStateType.Follow);
            }
        }

        // 2. Si je suis en Idle trop longtemps -> Patrouille (Optionnel)
        if (StateMachine.CurrentState == IdleState)
        {
            // Tu peux ajouter un petit timer ici pour passer en Patrol automatiquement
            StateMachine.ChangeState(EnemyStateType.Patrol);
        }
    }
    private void PlayPhaseMusic(int phaseIndex)
    {
        if (phases.Count > phaseIndex && phases[phaseIndex].phaseMusic != null)
        {
            bossAudioSource.clip = phases[phaseIndex].phaseMusic;
            bossAudioSource.loop = true;
            bossAudioSource.volume = baseMusicVolume;
            bossAudioSource.Play();
        }
    }

    // Optionnel : Si tu veux que le boss devienne plus rapide à chaque phase
    private void BoostBossStats(BossPhase phase)
    {
        // Augmente la vitesse du NavMeshAgent (ex: +20%)
        Agent.speed *= 1.2f;
        Agent.acceleration *= 1.2f;

        // On peut aussi booster les dégâts via un multiplicateur global si tu en as un
        Debug.Log($"Stats boosted for {phase.phaseName}: Speed is now {Agent.speed}");
    }


    // Call cette fonction au début du souffle/vfx dans l'animator
    public void AE_ActivateAttackVisual()
    {
        // On récupère l'attaque en cours d'exécution depuis l'état d'attaque
        BossAttackState attackState = StateMachine.CurrentState as BossAttackState;
        if (attackState == null || attackState.CurrentAttack == null) return;

        // On cherche le setup correspondant à cette attaque
        if (attackToWeaponMap.TryGetValue(attackState.CurrentAttack, out var setup))
        {
            if (setup.attackVisualObject != null)
            {
                setup.attackVisualObject.SetActive(true);
                _currentActiveVisual = setup.attackVisualObject; // On garde une référence pour le couper plus tard
                Debug.Log($"[Boss VFX] Activé : {setup.attackVisualObject.name}");
            }
        }
    }

    // Call cette fonction à la fin du souffle/vfx dans l'animator
    public void AE_DeactivateAttackVisual()
    {
        if (_currentActiveVisual != null)
        {
            _currentActiveVisual.SetActive(false);
            Debug.Log($"[Boss VFX] Désactivé : {_currentActiveVisual.name}");
            _currentActiveVisual = null;
        }
    }

    // Sécurité : Si le boss prend un coup ou change d'état brusquement, on coupe le VFX
    public void ForceDisableActiveVisual()
    {
        if (_currentActiveVisual != null)
        {
            _currentActiveVisual.SetActive(false);
            _currentActiveVisual = null;
        }
    }

    public void AE_OnAttackFinished() => (StateMachine.CurrentState as BossAttackState)?.OnAnimationFinished();

    #region Audio Fading
    private IEnumerator FadeMusicSequence(AudioClip newClip)
    {
        yield return StartCoroutine(FadeVolume(bossAudioSource.volume, 0, musicFadeDuration));
        bossAudioSource.clip = newClip;
        bossAudioSource.Play();
        yield return StartCoroutine(FadeVolume(0, baseMusicVolume, musicFadeDuration));
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            bossAudioSource.volume = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        bossAudioSource.volume = to;
    }
    #endregion
}

[System.Serializable]
public class BossPhase
{
    public string phaseName;
    [Range(0, 1)] public float healthThreshold; // Ex: 0.5f pour 50% PV
    public AudioClip phaseMusic;
    public float damageBoost;
    public List<AttackSO> newAttacks; // Attaques ajoutées à cette phase
    public string screamAnimationTrigger = "Scream";
    public GameObject gameObjectEvent;
}

[System.Serializable]
public class BossAttack
{
    public GameObject gameObjectAttack;
    public HitBoxAttack hitBoxAttack;
    public int damage;
    public int boostDamage;
    public string animTriggerName;
    public float distanceMin, distanceMax;
    public float timeBetweenAttacks = 3f;
    [HideInInspector] public float nextAttackTime;
    public DamageType damageType;
    public bool isFireBreath = false;

    [Header("Fire Ball")]
    public GameObject fireBallPrefab;
    public Transform firePoint;
    public float fireBallSpeed = 10f;
}