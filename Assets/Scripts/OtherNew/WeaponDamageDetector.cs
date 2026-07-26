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

    [Header("Pour éviter que les ennemis s'attaquent tous seuls")]
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private bool ignoreSelfDamage = true;

    private void Awake()
    {
        FetchCollider();
    }

    public void SetDamageFrame(float amount) => damageForThisFrame = amount;

    private void FetchCollider()
    {
        if (myCollider == null)
        {
            myCollider = GetComponent<Collider>();
        }
    }

    public void ToggleCollider(bool state)
    {
        FetchCollider();
        if (myCollider != null) myCollider.enabled = state;
        if (!state) alreadyHit.Clear();
    }

    public void DisableDamage()
    {
        FetchCollider();
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }
        else
        {
            Debug.LogWarning($"[WeaponDamageDetector] Impossible de désactiver le collider sur {gameObject.name} car aucun Collider n'est présent !", gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Évite de se frapper soi-même ou de frapper 2x la même cible
        if (other.gameObject == transform.root.gameObject || alreadyHit.Contains(other.gameObject))
            return;

        bool isArrow = itemData != null && itemData.equipmentType == EquipmentType.Arrow;
        bool isDamageable = other.TryGetComponent<IDamageable>(out var target);

        // 2. Évite que les flèches ne s'accrochent aux zones invisibles/déclencheurs (Trigger Zones)
        if (!isDamageable && other.isTrigger)
            return;

        // 3. Si la cible prend des dégâts (Ennemi, Joueur, PNJ...)
        if (isDamageable)
        {
            if (ignoreSelfDamage && (damageLayers.value & (1 << other.gameObject.layer)) != 0)
            {
                Debug.Log($"[Dégâts Ignorés] {transform.root.name} a touché un objet du même groupe : {other.gameObject.name}");
                return;
            }

            alreadyHit.Add(other.gameObject);
            ExecuteHitLogic(other, target);
        }
        // 4. Si ce n'est PAS un IDamageable (Mur, Sol, Arbre...) mais que c'est une flèche : elle s'accroche sans faire de sang
        else if (isArrow)
        {
            alreadyHit.Add(other.gameObject);
            StickToTarget(other.transform);
        }
    }

    private void ExecuteHitLogic(Collider other, IDamageable target)
    {
        // 1. Détermination des dégâts
        float dmg = hasDamageCollider ? (itemData.equipmentType == EquipmentType.Arrow ? itemData.attackPoints : colliderDamage) : damageForThisFrame;
        Debug.Log(dmg);
        // 2. Création et envoi des dégâts
        DamageInfo info = new DamageInfo(dmg, itemData.damageType, itemData.effet, itemData.poiseDamage, itemData.stunDuration, transform.root.gameObject);
        target.TakeDamage(info);

        // 3. Camera Shake
        CameraEvents.OnCameraShake?.Invoke(itemData.cameraShakeIntensity, itemData.cameraShakeDuration);

        // 4. Instanciation du sang UNIQUEMENT sur les cibles IDamageable
        if (bloodPrefab != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Instantiate(bloodPrefab, hitPoint, Quaternion.identity);
        }

        // 5. Si c'est une flèche, on l'accroche aussi à la cible vivante/mobile
        if (itemData != null && itemData.equipmentType == EquipmentType.Arrow)
        {
            StickToTarget(other.transform);
        }
    }

    /// <summary>
    /// Stoppe la physique et attache le projectile à l'objet percuté.
    /// </summary>
    private void StickToTarget(Transform targetTransform)
    {
        // Arrête le Rigidbody de la flèche s'il existe
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Plante légèrement la flèche dans la surface
        transform.position -= transform.forward * 0.2f;

        // Maintient la flèche attachée à l'objet (suit les mouvements des ennemis/plateformes)
        transform.parent = targetTransform;

        // Désactive le collider pour ne plus redéclencher d'impacts
        if (myCollider != null)
            myCollider.enabled = false;
    }
}