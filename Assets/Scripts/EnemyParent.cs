using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public abstract class EnemyParent : WorldDisappearOnCollected, IDamageable
{
    [Header("References")]
    [SerializeField] protected EnemySO enemyData;

    [HideInInspector]public NavMeshAgent agent;
    protected Animator animator;

    [SerializeField] private BarriereDeCombat[] barriereDeCombat;


    [SerializeField] protected Image healthBar;
    [SerializeField] protected GameObject vie;

    protected float currentHealth;

    protected Transform player;
    protected PlayerController playerStats;

    [SerializeField] protected GameObject itemToDrop;

    protected bool playerInSight = false;

    [Header("Type Defense and Attack")]
    [SerializeField] protected DamageType[] defensePointFortType;
    [SerializeField] protected DamageType[] defensePointFaibleType;
    [SerializeField] protected float pourcentageOfResistance = 0.2f; // 20% de résistance par rapport au type de défense

    public bool isAttacking = false;

    private bool isDead; 
    public bool IsDead
    {
        get => isDead;  
        protected set     
        {
            isDead = value;                        
            animator.SetBool("IsDead", isDead);    
        }
    }

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = enemyData.pvMax;

        isDead = animator.GetBool("IsDead");
    }


    void Start()
    {
        playerStats = PlayerController.Instance;
        player = playerStats.transform;
    }

    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        Debug.Log($"[{name}] TakeDamage called with damage: {damageInfo.rawPhysicalDamage}, poiseDamage: {damageInfo.poiseDamage}, damageType: {damageInfo.physicalType}");
        if (IsDead) return;

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

        currentHealth = Mathf.Max(0, currentHealth - damageInfo.rawPhysicalDamage);
        if (currentHealth < 0.001f)
            currentHealth = 0f;
        UpdateLife();

        playerInSight = true;

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
           animator.SetTrigger("GetHit");
        }
    }


    protected virtual void UpdateLife()
    {
        healthBar.fillAmount = currentHealth / enemyData.pvMax;
    }

    protected virtual void Die()
    {
        if (worldID != null)
        {
            WorldStateManager.Instance.RegisterCollectedObject(worldID.UniqueID);
            Debug.LogWarning($"<color=purple>[{name}] registered as collected in WorldStateManager, with ID : {worldID.UniqueID}.</color>");
        }
        if (barriereDeCombat != null)
        {
            Debug.Log("Raising the combat barrier.");
            UpAllBarriere();
        }
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

    public abstract void UpdateSpeedWitchCoefficient(float speedCoefficient);
}



