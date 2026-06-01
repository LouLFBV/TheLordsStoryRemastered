using UnityEngine;
using System.Linq;

public class LockOnSystem : MonoBehaviour
{
    [SerializeField] private float lockRadius = 15f;
    [SerializeField] private LayerMask enemyLayer;
    private EnemyControllerBase currentTarget;
    public Transform CurrentTarget { get; private set; }
    public bool IsLocked => CurrentTarget != null;

    private void SearchTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, lockRadius, enemyLayer);

        if (hits.Length == 0) return;

        // 1. On trouve le collider le plus proche
        Collider closestHit = hits
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .First();

        // 2. On tente de récupérer le script EnemyController sur ce collider
        if (closestHit.TryGetComponent<EnemyControllerBase>(out var enemy))
        {
            // On éteint l'ancienne marque visuelle si on change de cible
            if (currentTarget != null) currentTarget.SetLockOnIndicator(false);

            // On assigne les références
            currentTarget = enemy;           // Référence au script pour l'UI
            CurrentTarget = enemy.transform; // Référence au Transform pour la caméra/mouvement

            // On active la marque visuelle
            currentTarget.SetLockOnIndicator(true);
        }
    }

    public void ToggleLock()
    {
        if (IsLocked)
        {
            // Utilise ta méthode DeselectTarget pour bien tout nettoyer
            DeselectTarget();
        }
        else
        {
            SearchTarget();
        }
    }

    public void Update()
    {
        if (!IsLocked) return;

        // Sécurité : Distance ou si l'ennemi meurt (Health <= 0)
        float distance = Vector3.Distance(transform.position, CurrentTarget.position);

        // On vérifie la distance OU si l'ennemi est mort (via son HealthSystem)
        if (distance > currentTarget.enemyData.maxRangeLockOn || (currentTarget != null && currentTarget.Health.IsDead))
        {
            DeselectTarget();
        }
    }

    public void DeselectTarget()
    {
        // On éteint le visuel avant de perdre la référence
        if (currentTarget != null)
        {
            currentTarget.SetLockOnIndicator(false);
        }

        currentTarget = null;
        CurrentTarget = null;
    }
    private void OnDrawGizmosSelected()
    {
        // Dessine le rayon de détection en bleu
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, lockRadius);

        // Dessine une ligne rouge vers la cible si verrouillé
        if (IsLocked && CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
            Gizmos.DrawWireSphere(CurrentTarget.position, 1f);
        }
    }
}