using UnityEngine;
using System.Collections.Generic;

public class WeaponDamageDetector : MonoBehaviour
{
    [Header("Settings")]
    public ItemData itemData; // Ton ScriptableObject d'arme
    [SerializeField] private GameObject bloodPrefab;

    private float damageForThisFrame;
    private Collider myCollider;
    private List<GameObject> alreadyHit = new List<GameObject>();

    [Header("If is object with collider (like arrow)")]
    [SerializeField] private bool hasDamageCollider = false;
    [SerializeField] private float colliderDamage = 10f;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    public void SetDamageFrame(float amount) => damageForThisFrame = amount;

    public void ToggleCollider(bool state)
    {
        if (myCollider != null) myCollider.enabled = state;
        if (!state) alreadyHit.Clear();
    }

    public void DisableDamage() => myCollider.enabled = false;

    private void OnTriggerEnter(Collider other)
    {
        // Évite de se frapper soi-même ou de frapper 2x la même cible
        if (other.gameObject == transform.root.gameObject || alreadyHit.Contains(other.gameObject))
            return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            alreadyHit.Add(other.gameObject);
            ExecuteHitLogic(other, target);
        }
    }

    private void ExecuteHitLogic(Collider other, IDamageable target)
    {
        // 1. Détermination des dégâts de cette frame
        float dmg = hasDamageCollider ? colliderDamage : damageForThisFrame;

        // 2. Création du conteneur d'informations dynamique du coup
        // On passe 'transform.root.gameObject' pour définir l'attaquant (le joueur ou le monstre global)
        DamageInfo info = new DamageInfo(dmg, itemData.damageType, itemData.effet, itemData.poiseDamage, transform.root.gameObject);

        // 3. Envoi du paquet à la cible
        target.TakeDamage(info);

        // 4. Camera Shake (Game Feel)
        CameraEvents.OnCameraShake?.Invoke(itemData.cameraShakeIntensity, itemData.cameraShakeDuration);

        // 5. Logique physique spécifique aux Flèches (Arrow)
        if (itemData.equipmentType == EquipmentType.Arrow)
        {
            HandleArrowCollision(other);
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
        if (myCollider != null) myCollider.enabled = false;
        if (bloodPrefab != null) Instantiate(bloodPrefab, transform.position, Quaternion.identity);
    }
}