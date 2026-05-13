using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyController : WorldDisappearOnCollected, ICombatant
{
    [Header("Core Components")]
    public EnemyStateMachine StateMachine { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator Animator { get; private set; }
    public Rigidbody Rigidbody { get; private set; }

    [Header("Combat Settings")]
    [SerializeField] private List<EnemyWeaponSetup> weaponSetups;
    private Dictionary<AttackSO, EnemyWeaponSetup> _attackToWeaponMap;
    public Dictionary<AttackSO, WeaponDamageDetector> weaponDict;
    [SerializeField] private List<AttackSO> availableAttacks;
    public ItemData PendingWeaponItem { get; set; }
    private AttackSO _lastPerformedAttack; // On stocke la dernière attaque
    public float lastAttackExitTime { get; set; }
    [SerializeField] private BarriereDeCombat[] barriereDeCombat;

    [Header("Senses & AI")]
    public Transform target; // Le joueur
    [HideInInspector] public NewEnemySO enemyData; 

    [Header("UI & Feedback")]
    [SerializeField] private GameObject lockOnIndicator;
    [SerializeField] private EnemyHealthUI healthUI;

    [Header("Systems (Shared with Player)")]
    public HealthSystem Health { get; private set; }
    public PoiseSystem Poise { get; private set; }
    public CombatSystem Combat { get; private set; }
    public DamageReceiver DmgReceiver { get; private set; }
    public AIManager AIManager { get; private set; }

    [Header ("Enemy States")]
    public EnemyIdleState IdleState { get; private set; }
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyFollowState FollowState { get; private set; }
    public EnemyOrbitState OrbitState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    public EnemyHitState HitState { get; private set; }
    public EnemyStunnedState StunnedState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    [Header("Other")]
    private WorldObjectID _worldID;

    protected override void Awake()
    {
        // 1. Initialisation des composants physiques
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Rigidbody = GetComponent<Rigidbody>();

        // 2. Initialisation des systèmes de combat (les mêmes que le joueur !)
        Health = GetComponent<HealthSystem>();
        Poise = GetComponent<PoiseSystem>();
        Combat = GetComponent<CombatSystem>();
        DmgReceiver = GetComponent<DamageReceiver>();
        AIManager = GetComponent<AIManager>();
        AIManager.Initialize(this); // On passe le controller à l'AI Manager pour qu'il puisse interagir avec les états et les systèmes de l'ennemi

        // 3. Création des instances d'états
        // On passera 'this' (le controller) à chaque état
        IdleState = new EnemyIdleState(this);
        FollowState = new EnemyFollowState(this);
        OrbitState = new EnemyOrbitState(this);
        AttackState = new EnemyAttackState(this);
        PatrolState = new EnemyPatrolState(this); // On le fera juste après
        HitState = new EnemyHitState(this);
        StunnedState = new EnemyStunnedState(this);
        DeathState = new EnemyDeathState(this);

        // 4. Setup de la State Machine
        var states = new Dictionary<EnemyStateType, EnemyState>
        {
            { EnemyStateType.Idle, IdleState },
            { EnemyStateType.Follow, FollowState },
            { EnemyStateType.Orbit, OrbitState },
            { EnemyStateType.Attack, AttackState },
            { EnemyStateType.Patrol, PatrolState },
            { EnemyStateType.Hit, HitState },
            { EnemyStateType.Stunned, StunnedState },
            { EnemyStateType.Death, DeathState }
        };

        StateMachine = new EnemyStateMachine(states);

        _attackToWeaponMap = new Dictionary<AttackSO, EnemyWeaponSetup>();
        foreach (var setup in weaponSetups)
        {
            foreach (var attack in setup.usableAttacks)
            {
                if (!_attackToWeaponMap.ContainsKey(attack))
                {
                    _attackToWeaponMap.Add(attack, setup);
                   // Debug.Log($"[EnemyController] Mapping attack {attack.animationName} to weapon {setup.weaponData.itemName}.");
                }
                else
                {
                   // Debug.LogWarning($"[EnemyController] Duplicate attack {attack.animationName} found in weapon setups. Only the first mapping will be used.");
                }
            }
        }

        foreach (var cooldown in availableAttacks)
        {
            cooldown.nextAttackTime = 0f; // S'assure que toutes les attaques sont prêtes au départ
        }

        _worldID = GetComponent<WorldObjectID>();
    }

    private void Start()
    {
        // 1. Recherche du joueur
        if (target == null && PlayerController.Instance != null)
            target = PlayerController.Instance.transform;

        if (enemyData == null)
            enemyData = AIManager.GetData();

        // 3. Initialisation de l'UI et du Lock
        if (healthUI != null) healthUI.Initialize(Health);
        SetLockOnIndicator(false);

        // 4. Lancement de l'IA
        StateMachine.Initialize(EnemyStateType.Idle);

    }

    private void Update()
    {
        StateMachine.Update();

        // C'est ici que l'AI Manager interviendrait, 
        // mais pour l'instant on peut mettre une logique simple :
        CheckForPlayer();
    }

    private void FixedUpdate() => StateMachine.FixedUpdate();

    private void CheckForPlayer()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        Vector3 dirToPlayer = (target.position - transform.position).normalized;

        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        // LOGIQUE DE TRANSITION :

        // 1. Si je suis en Idle ou Patrol et que je vois le joueur -> Poursuite
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

    public AttackSO PeekBestAttack()
    {
        if (target == null) return null;
        if (Time.time < lastAttackExitTime + 1.5f) return null;

        float distance = Vector3.Distance(transform.position, target.position);

        // 1. On crée une liste temporaire des attaques actuellement possibles
        List<AttackSO> potentialAttacks = new List<AttackSO>();

        foreach (var attack in availableAttacks)
        {
            bool cooldownTermine = Time.time >= attack.nextAttackTime;
            bool distanceOK = distance >= attack.minDistance && distance <= attack.maxDistance;

            if (distanceOK && cooldownTermine)
            {
                // On évite toujours la toute dernière attaque pour la variété
                if (attack == _lastPerformedAttack && availableAttacks.Count > 1) continue;

                potentialAttacks.Add(attack);
            }
        }

        // 2. Si on a plusieurs choix, on en prend un au hasard !
        if (potentialAttacks.Count > 0)
        {
            int randomIndex = Random.Range(0, potentialAttacks.Count);
            return potentialAttacks[randomIndex];
        }

        return null;
    }

    public AttackSO GetBestAttack()
    {
        AttackSO attack = PeekBestAttack(); // On cherche l'attaque

        if (attack != null)
        {
            // FORCE le cooldown ici pour que PeekBestAttack() 
            // renvoie NULL à la frame suivante !
            attack.nextAttackTime = Time.time + attack.attackCooldown;
            _lastPerformedAttack = attack;

            Debug.Log($"<color=cyan>[CORE]</color> Cooldown activé pour {attack.animationName}. Prochaine dispo dans {attack.attackCooldown}s");
            return attack;
        }
        return null;
    }

    // Implémentation de ICombatant
    public float GetBaseWeaponDamage()
    {
        // On utilise les dégâts de ton ItemData (arme) ou de ton EnemySO
        return PendingWeaponItem != null ? PendingWeaponItem.attackPoints : 10f;
    }

    // On fait le pont entre les events de l'Animator et le CombatSystem
    public void AE_HitboxOpen() => Combat.AE_HitboxOpen();
    public void AE_HitboxClose() => Combat.AE_HitboxClose();
    // --- Animation Events (Même logique que le joueur) ---
    public void AE_OnAttackFinished() => (StateMachine.CurrentState as EnemyAttackState)?.OnAnimationFinished();

    public void EquipWeapon(AttackSO weapon)
    {
        if (weaponDict.TryGetValue(weapon, out var detector))
        {
            Combat.UpdateWeaponDetector(detector);
        }
        else
            Debug.LogWarning($"[EnemyController] No WeaponDamageDetector found for attack {weapon.animationName} in weaponDict.");
    }
    public void PrepareAttack(AttackSO attack)
    {
        if (_attackToWeaponMap.TryGetValue(attack, out var setup))
        {
            PendingWeaponItem = setup.weaponData;
            Combat.UpdateWeaponDetector(setup.detector);

            // Audio (comme dans ton BossAI)
            if (attack.attackSound != null)
            {
                // On peut utiliser une AudioSource fixe sur l'ennemi pour plus de contrôle
                AudioSource source = GetComponent<AudioSource>();
                if (source != null) source.PlayOneShot(attack.attackSound);
                else AudioSource.PlayClipAtPoint(attack.attackSound, transform.position);
            }
        }
        else
        {
            Debug.LogWarning($"[EnemyController] No weapon setup found for attack {attack.animationName} in _attackToWeaponMap.");
        }
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        if (Health != null)
        {
            Health.OnDeath += HandleDeath;
            Health.OnHit += GoToFollowState; 
        }
    }

    protected override void OnDisable()
    {
        if (Health != null)
        {
            Health.OnDeath -= HandleDeath;
            Health.OnHit -= GoToFollowState; 
        }
    }

    private void HandleDeath()
    {
        // On force le passage à l'état de mort, peu importe l'état actuel

        NewQuestManager.instance.UpdateQuestProgress(AIManager.GetData().enemyType.ToString(), 1);
        StateMachine.ChangeState(EnemyStateType.Death);

        if (_worldID != null)
        {
            WorldStateManager.Instance.RegisterCollectedObject(_worldID.UniqueID);
            Debug.LogWarning($"<color=purple>[{name}] registered as collected in WorldStateManager, with ID : {_worldID.UniqueID}.</color>");
        }
        if (barriereDeCombat != null)
        {
            Debug.Log("Raising the combat barrier.");
            UpAllBarriere();
        }
    }

    private void GoToFollowState()
    {
        if(StateMachine.CurrentState == FollowState) return;
        StateMachine.ChangeState(EnemyStateType.Follow);
    }

    public void SetLockOnIndicator(bool isLocked)
    {
        if (lockOnIndicator != null)
            lockOnIndicator.SetActive(isLocked);
    }

    private void UpAllBarriere()
    {
        if (barriereDeCombat != null)
        {
            foreach (var barriere in barriereDeCombat)
            {
                Debug.Log("Raising a combat barrier.");
                barriere.UpBarriere();
            }
        }
    }
    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, enemyData.visionRange);
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, AttackRadius);

    //    // Cône de vision
    //    Gizmos.color = Color.yellow;

    //    Vector3 leftRay = Quaternion.Euler(0, -enemyData.visionAngle / 2f, 0) * transform.forward;
    //    Vector3 rightRay = Quaternion.Euler(0, enemyData.visionAngle / 2f, 0) * transform.forward;

    //    Gizmos.DrawRay(transform.position, leftRay * enemyData.visionRange);
    //    Gizmos.DrawRay(transform.position, rightRay * enemyData.visionRange);
    //}


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"State: {StateMachine.CurrentState.GetType().Name}");
        }
        // 2. Dessin des ranges de CHAQUE AttackSO
        if (availableAttacks != null)
        {
            foreach (var attack in availableAttacks)
            {
                if (attack == null) continue;

                // On change la couleur en fonction de l'attaque pour les différencier
                // On utilise le hash du nom pour générer une couleur unique par attaque
                Random.InitState(attack.animationName.GetHashCode());
                Color attackColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

                // Cercle Max Distance (Plein)
                Gizmos.color = attackColor;
                DrawWireDisk(transform.position, attack.maxDistance);

                // Cercle Min Distance (Pointillé ou plus fin)
                Gizmos.color = new Color(attackColor.r, attackColor.g, attackColor.b, 0.3f);
                DrawWireDisk(transform.position, attack.minDistance);


                // Afficher le nom de l'attaque au bord de sa range
                UnityEditor.Handles.Label(transform.position + (transform.forward * attack.maxDistance),
                    $"{attack.animationName} ({attack.minDistance}m - {attack.maxDistance}m)");

            }
        }
    }
#endif
    private void OnDrawGizmosSelected()
    {
        // 1. Dessin de la portée de détection (Vision)
        if (enemyData != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); // Jaune transparent
            Gizmos.DrawWireSphere(transform.position, enemyData.visionRange);
        }

        
    }

    // Petite fonction utilitaire pour dessiner des disques propres au sol
    private void DrawWireDisk(Vector3 center, float radius)
    {
        float angle = 0f;
        Vector3 lastPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

        for (int i = 1; i <= 32; i++)
        {
            angle = i * (Mathf.PI * 2f) / 32;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}

[System.Serializable]
public class EnemyWeaponSetup
{
    public ItemData weaponData;          // Contient les points d'attaque (ex: 15)
    public WeaponDamageDetector detector; // Le script sur l'objet physique
    public List<AttackSO> usableAttacks;  // Les attaques que CETTE arme peut faire
}