using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerController player; // Pour trouver les systèmes

    [Header("Health UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Animator healthBarAnimator;

    [Header("Stamina UI")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private Animator staminaBarAnimator;
    [SerializeField] private Animator lowHealthVolume;

    [Header("Colors")]
    private Color colorFull = new Color32(0x2F, 0x62, 0x26, 0xFF);   // Vert
    private Color colorMedium = new Color32(0xC0, 0x88, 0x34, 0xFF); // Orange
    private Color colorLow = new Color32(0x99, 0x46, 0x46, 0xFF);    // Rouge

    [Header("Death Panel")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Animator deathPanelAnimator;

    private bool _feedbackLocked;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateHealth(player.Health.CurrentHealth, player.Health.CurrentHealth); // Initialisation de la barre
        // On s'abonne aux événements des systèmes
        player.Health.OnHealthChanged += UpdateHealth;
        player.Stamina.OnStaminaChanged += UpdateStaminaBar;
        player.Stamina.OnStaminaEmpty += HandleEmptyFeedback;

        // On s'abonne aussi au "Hit" pour l'animation de la barre
        player.Health.OnHit += () => healthBarAnimator.SetTrigger("TakeDamage");
        player.Health.OnHit += UpdateLowHealthVolume;
    }
    private void Update()
    {
        // Si le joueur lâche la touche de sprint, on déverrouille le feedback
        if (!player.Input.SprintHeld)
        {
            _feedbackLocked = false;
        }
    }

    private void HandleEmptyFeedback()
    {
        // On ne joue l'animation que si le feedback n'est pas verrouillé
        if (!_feedbackLocked)
        {
            _feedbackLocked = true; // On verrouille jusqu'à ce qu'il lâche la touche
        }
    }

    private void UpdateLowHealthVolume()
    {
        Debug.Log("<color=orange> Ecran Rouge Méthode</color>");
        if (lowHealthVolume == null || player.Health.CurrentHealth >= player.Health.MaxHealth/2) return;
        Debug.Log("<color=orange> Ecran Rouge Affiché</color>");
        lowHealthVolume.SetTrigger("TakeDamage");
    }

    private void UpdateHealth(float current, float max)
    {
        float ratio = current / max;
        healthBarFill.fillAmount = ratio;
        UpdateHealthBarColor(ratio);
    }
    public void UpdateHealthBar()
    {
        float ratio =  player.Health.CurrentHealth / player.Health.MaxHealth;
        healthBarFill.fillAmount = ratio;
        UpdateHealthBarColor(ratio);

    }

    private void UpdateHealthBarColor(float ratio)
    {
        // Ta logique de dégradé de couleurs
        if (ratio >= 0.6f)
            healthBarFill.color = Color.Lerp(colorMedium, colorFull, (ratio - 0.6f) / 0.4f);
        else if (ratio >= 0.2f)
            healthBarFill.color = Color.Lerp(colorLow, colorMedium, (ratio - 0.2f) / 0.4f);
        else
            healthBarFill.color = colorLow;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        staminaBarFill.fillAmount = current / max;

        // Si stamina très basse, on peut trigger l'anim "StaminaLow"
        if (current <= 0)
            staminaBarAnimator.SetTrigger("StaminaLow");
    }

    public void OpenDeathPanel()
    {
        deathPanel.SetActive(true);
        deathPanelAnimator.SetTrigger("Open");
        player.StateMachine.ChangeState(PlayerStateType.UI);
    }

    public void CloseDeathPanel()
    {
        deathPanel.SetActive(false);
        player.Animator.SetTrigger("Relive");
        player.StateMachine.ChangeState(PlayerStateType.Idle);
    }
}