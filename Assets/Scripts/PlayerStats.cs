using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{

    public static PlayerStats instance;

    [Header("Others Elements References")]
    [SerializeField] private Animator animator;
    public ReputationData reputationData;
    private MoveBehaviour playerMovementScript;
    private AttackBehaviour attackBehaviour;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private float healthDecreaseRateForHungerAndThirst;
    [SerializeField] private ParticleSystem healthEffect;

    [Header("Endurance")]

    [SerializeField] 
    private float enduranceRecoveryDelay = 2f;  // délai avant regen endurance
    private float enduranceRecoveryTimer = 0f;
    private bool canRecoverEndurance = true;
    private float maxEndurance = 100f;
    public float currentEndurance;
    public float coutDuSprint = 30f;

    [SerializeField] private Image enduranceBarFill;

    [SerializeField] private float enduranceDecreaseRateForHungerAndThirst;


    [Header("Armor")]
    public float currentArmourPointsPercant;
    public float currentArmourPointsContendant;
    public float currentArmourPointsTranchant;
    public float currentArmourPointsFire;
    public float currentArmourPointsIce;
    public float currentArmourPointsElectric;

    [SerializeField] private TextMeshProUGUI armorPercantText;
    [SerializeField] private TextMeshProUGUI armorContendantText;
    [SerializeField] private TextMeshProUGUI armorTranchantText;
    [SerializeField] private TextMeshProUGUI armorFireText;
    [SerializeField] private TextMeshProUGUI armorIceText;
    [SerializeField] private TextMeshProUGUI armorElectricText;


    [Header("Gold")]

    public int goldAmount;

    [SerializeField] private TextMeshProUGUI goldText;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip goldSound;

    [Header("Panel de mort")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Animator deathAnimator;

    [Header("Camera Shake")]
    //[SerializeField] private float cameraShakeIntensity = 0.15f;
    //[SerializeField] private float cameraShakeDuration = 0.2f;

    [HideInInspector] public bool isDead = false;
    [HideInInspector] public EquipmentLibraryItem equipmentToEquip, equipmentToDesequip;
    [HideInInspector] public bool isEquiping = false;

    [Header("States Bars")]
    // Couleurs extraites de ta palette
    private Color colorFull = new Color32(0x2F, 0x62, 0x26, 0xFF);   // #2f6226 (Vert)
    private Color colorMedium = new Color32(0xC0, 0x88, 0x34, 0xFF); // #c08834 (Orange)
    private Color colorLow = new Color32(0x99, 0x46, 0x46, 0xFF);    // #994646 (Rouge)
    [SerializeField] private Animator staminaBarAnimator;
    [SerializeField] private Animator healthBarAnimator;
    private bool _fistAnimStaminaLowPlayed = false;


    public void AddGold(int amount)
    {
        goldAmount += amount;
        audioSource.PlayOneShot(goldSound);
        UpdateGoldText();
    }

    public void UpdateGoldText()
    {
        goldText.text = goldAmount.ToString();
    }

    void Awake()
    {
        instance = this;
        playerMovementScript = GetComponent<MoveBehaviour>();
        attackBehaviour = GetComponent<AttackBehaviour>();

        if (currentHealth <= 0)
            currentHealth = maxHealth;

        if (currentEndurance <= 0)
            currentEndurance = maxEndurance;
        UpdateHealthBar();
    }


    void Update()
    {
        if (currentEndurance <= 0)
        {
            if (!_fistAnimStaminaLowPlayed)
            {
                AnimStaminaLow();
                _fistAnimStaminaLowPlayed = true;
            }
            canRecoverEndurance = false;
            enduranceRecoveryTimer += Time.deltaTime;

            // Après le délai, on peut régénérer 
            if (enduranceRecoveryTimer >= enduranceRecoveryDelay)
            {
                _fistAnimStaminaLowPlayed = false;
                canRecoverEndurance = true;
                enduranceRecoveryTimer = 0f;
            }
        }
        else
        {
            // Si endurance > 0 on reset le timer et on peut toujours regen
            enduranceRecoveryTimer = 0f;
            canRecoverEndurance = true;
        }

        if (!playerMovementScript.isSprinting  && currentEndurance < maxEndurance && canRecoverEndurance && !attackBehaviour.isAttacking)
        {
            UpdateEndurance(20f * Time.deltaTime); // regen endurance
        }
    }


    public void TakeDamage(float damage,DamageType damageType, bool overTime = false)
    {
        if (overTime)
        {
            currentHealth -= damage * Time.deltaTime;
        }
        else
        {
            switch(damageType)
            {
                case DamageType.Percant:
                    damage -= (currentArmourPointsPercant / 100);
                    break;
                case DamageType.Contendant:
                    damage -= (currentArmourPointsContendant / 100);
                    break;
                case DamageType.Tranchant:
                    damage -= (currentArmourPointsTranchant / 100);
                    break;
                case DamageType.Feu:
                    damage -= (currentArmourPointsFire / 100);
                    break;
                case DamageType.Glace:
                    damage -= (currentArmourPointsIce / 100);
                    break;
                case DamageType.Foudre:
                    damage -= (currentArmourPointsElectric / 100);
                    break;
            }
            currentHealth -= damage;
            //CameraEvents.OnCameraShake?.Invoke(cameraShakeIntensity, cameraShakeDuration);
        }
        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            Die();
        }
        AnimTakeDamage();
    }

    private void Die()
    {
        isDead = true;
        playerMovementScript.canMove = false;

        animator.SetTrigger("Die");
    }

    public void OpenDeathPanel()
    {
        deathPanel.SetActive(true);
        deathAnimator.SetTrigger("Open");
    }

    public void AnimTakeDamage() => healthBarAnimator.SetTrigger("TakeDamage");
    public void AnimStaminaLow() => staminaBarAnimator.SetTrigger("StaminaLow");
    public void UpdateHealthBar()
    {
        float ratio = currentHealth / maxHealth;
        healthBarFill.fillAmount = ratio;

        if (ratio >= 0.6f)
        {
            // On passe de Orange (à 0.6) vers Vert (à 1.0)
            // La plage est de 0.4 (1.0 - 0.6)
            float t = (ratio - 0.6f) / 0.4f;
            healthBarFill.color = Color.Lerp(colorMedium, colorFull, t);
        }
        else if (ratio >= 0.2f)
        {
            // On passe de Rouge (à 0.2) vers Orange (à 0.6)
            // La plage est de 0.4 (0.6 - 0.2)
            float t = (ratio - 0.2f) / 0.4f;
            healthBarFill.color = Color.Lerp(colorLow, colorMedium, t);
        }
        else
        {
            // En dessous de 20%, on reste rouge
            healthBarFill.color = colorLow;
        }
    }


    public void UpdateEndurance(float amount)
    {
        currentEndurance = Mathf.Clamp(currentEndurance + amount, 0, maxEndurance);
        enduranceBarFill.fillAmount = currentEndurance / maxEndurance;
    }

    public void ConsumeItem(float health)
    {
        if (currentHealth != maxHealth)
            healthEffect.Play();
        currentHealth = Mathf.Min(currentHealth + health, maxHealth);
        UpdateHealthBar();
    }


    public void UpddateArmorText()
    {
        armorPercantText.text = currentArmourPointsPercant.ToString() + "%";
        armorContendantText.text = currentArmourPointsContendant.ToString() + "%";
        armorTranchantText.text = currentArmourPointsTranchant.ToString() + "%";
        armorFireText.text = currentArmourPointsFire.ToString() + "%";
        armorIceText.text = currentArmourPointsIce.ToString() + "%";
        armorElectricText.text = currentArmourPointsElectric.ToString() + "%";
    }

    public void EquipAndActiveItem()
    {
        equipmentToEquip.itemPrefab.SetActive(true);
        equipmentToEquip = null;
    }
    public void DesequipAndDesactiveItem()
    {
        equipmentToDesequip.itemPrefab.SetActive(false);
        equipmentToDesequip = null;
    }

    public void ActiveIsEquiping()=> isEquiping = true;

    public void DesactiveIsEquiping() => isEquiping = false;

    public PlayerStatsSaveData GetSaveData()
    {
        return new PlayerStatsSaveData
        {
            currentHealth = currentHealth,
            currentEndurance = currentEndurance,
            gold = goldAmount,

            position = transform.position,

            armourPercant = currentArmourPointsPercant,
            armourContendant = currentArmourPointsContendant,
            armourTranchant = currentArmourPointsTranchant,
            armourFire = currentArmourPointsFire,
            armourIce = currentArmourPointsIce,
            armourElectric = currentArmourPointsElectric
        };
    }

    public void LoadSaveData(PlayerStatsSaveData data)
    {
        currentHealth = data.currentHealth;
        currentEndurance = data.currentEndurance;
        goldAmount = data.gold;

        currentArmourPointsPercant = data.armourPercant;
        currentArmourPointsContendant = data.armourContendant;
        currentArmourPointsTranchant = data.armourTranchant;
        currentArmourPointsFire = data.armourFire;
        currentArmourPointsIce = data.armourIce;
        currentArmourPointsElectric = data.armourElectric;

        transform.position = data.position;

        UpdateHealthBar();
        UpdateEndurance(0);
        UpdateGoldText();
        UpddateArmorText();
    }

}

[System.Serializable]
public class PlayerStatsSaveData
{
    public float currentHealth;
    public float currentEndurance;
    public int gold;

    public Vector3 position;

    public float armourPercant;
    public float armourContendant;
    public float armourTranchant;
    public float armourFire;
    public float armourIce;
    public float armourElectric;
}
