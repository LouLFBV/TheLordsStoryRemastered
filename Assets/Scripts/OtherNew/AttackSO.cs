using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/Attack")]
public class AttackSO : ScriptableObject
{
    public int AnimationHash => Animator.StringToHash(animationName);
    public string animationName;      // Le nom du clip (ou le paramètre trigger)
    public float damageMultiplier = 1f; // Multiplicateur de dégâts
    public int animatorLayer = 9;
    public bool isSimpleAttack = false;
    //public float staminaCost = 15f;   // Coût en endurance

    [Header("Combo Logic")]
    public AttackSO nextAttack;       // L'attaque suivante si on reclique

    [Header("AI Conditions")]
    public float minDistance;
    public float maxDistance;
    public float attackCooldown; 
    public float postAttackDelay = 0.5f; 
    [Range(1, 100)] public int weight = 50;

    [Header("Custom Movements")]
    public bool useAdvancedMovement = false;
    public float movementDuration = 0.5f;

    [Tooltip("Vitesse / Distance vers l'avant au cours du temps (0 à 1)")]
    public AnimationCurve forwardMovementCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Tooltip("Hauteur (Axe Y) au cours du temps (0 à 1). Dessine une cloche pour un saut !")]
    public AnimationCurve verticalMovementCurve = AnimationCurve.Constant(0, 1, 0);

    public float forwardForce = 10f;  // Multiplicateur pour le mouvement horizontal
    public float verticalForce = 5f;   // Multiplicateur pour la hauteur du saut

    [Header("Audio")]
    public AudioClip attackSound;

    //[HideInInspector] public float nextAttackTime; 
}