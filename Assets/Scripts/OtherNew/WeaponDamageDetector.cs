using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponDamageDetector : MonoBehaviour
{
    [Header("Settings")]
    public ItemData itemData; // Ton ScriptableObject d'arme
    [SerializeField] private GameObject bloodPrefab;

    private float damageForThisFrame;
    private Collider myCollider;
    private List<GameObject> alreadyHit = new List<GameObject>();

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    public void SetDamageFrame(float amount) => damageForThisFrame = amount;

    public void ToggleCollider(bool state)
    {
        myCollider.enabled = state;
        if (!state) alreadyHit.Clear();
    }
    public void DisableDamage() => myCollider.enabled = false;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collision détectée avec {other.gameObject.name} sur {gameObject.name} (Dégâts: {damageForThisFrame})");
        // On évite de se frapper soi-même ou de frapper 2x la même cible
        if (other.gameObject == transform.root.gameObject || alreadyHit.Contains(other.gameObject))
        {
            Debug.Log("Collision ignorée : " + other.gameObject.name);
            return;
        }

        // On cherche une interface de dégâts (plus propre que Tag "AI")
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            Debug.Log($"Cible valide touchée : {other.gameObject.name} avec {damageForThisFrame} dégâts.");
            alreadyHit.Add(other.gameObject);
            ExecuteHitLogic(other, target);
        }
    }

    private void ExecuteHitLogic(Collider other, IDamageable target)
    {
        // 1. Dégâts de base calculés par le CombatSystem
        target.TakeDamage(damageForThisFrame, itemData.poiseDamage, itemData.damageType);

        // 2. Camera Shake (Game Feel)
        CameraEvents.OnCameraShake?.Invoke(itemData.cameraShakeIntensity, itemData.cameraShakeDuration);

        // 3. Logique spécifique aux Flèches (Arrow)
        if (itemData.equipmentType == EquipmentType.Arrow)
        {
            HandleArrowCollision(other);
            if (other.TryGetComponent<EnemyParent>(out var enemy))
                StartCoroutine(ApplyArrowEffect(enemy));
        }
        else if (bloodPrefab != null) // Sang pour le corps à corps
        {
            Instantiate(bloodPrefab, other.ClosestPoint(transform.position), Quaternion.identity);
        }
    }

    private void HandleArrowCollision(Collider other)
    {
        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        transform.position -= transform.forward * 0.1f;
        transform.parent = other.transform;
        myCollider.enabled = false;
        if (bloodPrefab != null) Instantiate(bloodPrefab, transform.position, Quaternion.identity);
    }

    // On garde ton IEnumerator pour les effets élémentaires (c'est top !)
    private IEnumerator ApplyArrowEffect(EnemyParent enemyAI)
    {
        switch (itemData.damageType)
        {
            case DamageType.Feu:
                enemyAI.TakeDamage(itemData.attackPoints, itemData.poiseDamage, itemData.damageType);
                for (int i = 0; i < 5; i++)
                {
                    enemyAI.TakeDamage(itemData.attackPoints * 0.2f, itemData.poiseDamage, itemData.damageType);
                    yield return new WaitForSeconds(1f);
                }
                break;

            case DamageType.Glace:
                enemyAI.TakeDamage(itemData.attackPoints, itemData.poiseDamage, itemData.damageType);
                enemyAI.UpdateSpeedWitchCoefficient(0.5f);
                yield return new WaitForSeconds(3f);
                enemyAI.UpdateSpeedWitchCoefficient(2f);
                break;

            case DamageType.Foudre:
                enemyAI.TakeDamage(itemData.attackPoints, itemData.poiseDamage, itemData.damageType);
                if (enemyAI.IsDead) yield break;
                enemyAI.agent.isStopped = true;
                yield return new WaitForSeconds(1.5f);
                enemyAI.agent.isStopped = false;
                break;

            default:
                enemyAI.TakeDamage(itemData.attackPoints, itemData.poiseDamage, itemData.damageType);
                CameraEvents.OnCameraShake?.Invoke(
                    itemData.cameraShakeIntensity,
                    itemData.cameraShakeDuration
                );
                break;
        }
    }
}