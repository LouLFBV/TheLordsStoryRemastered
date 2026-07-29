using UnityEngine;
using System.Collections.Generic;

public class ArmorSystem : MonoBehaviour
{
    [Header("Physical Resistances")]
    public float armorClassique = 10f;
    public float armorTranchant = 5f;
    public float armorContendant = 5f;
    public float armorPercant = 0f;

    [Header("Équilibrage de la Réduction Naturelle")]
    [Tooltip("Constante K. Plus elle est élevée, plus il faut d'armure pour réduire les dégâts.\n50 = 50% de réduction à 50 d'armure.\n60 = 50% de réduction à 60 d'armure.")]
    [SerializeField] private float armorFactor = 50f;

    /// <summary>
    /// Calcule la réduction des dégâts via une courbe à rendement décroissant (pas de hard cap).
    /// </summary>
    public float CalculateReducedDamage(DamageInfo damageInfo, out float physicalReduced)
    {
        float physicalDefensePoints = 0f;

        // 1. Récupération des points d'armure correspondants
        switch (damageInfo.physicalType)
        {
            case DamageType.Tranchant: physicalDefensePoints = armorTranchant; break;
            case DamageType.Contendant: physicalDefensePoints = armorContendant; break;
            case DamageType.Percant: physicalDefensePoints = armorPercant; break;
            case DamageType.Classique: physicalDefensePoints = armorClassique; break;
        }

        // 2. Formule à rendement décroissant : Multiplicateur = K / (K + Armure)
        float armor = Mathf.Max(0f, physicalDefensePoints);
        float physicalMultiplier = armorFactor / (armorFactor + armor);

        // 3. Calcul des dégâts finaux
        float finalPhysicalDamage = damageInfo.rawPhysicalDamage * physicalMultiplier;

        // Dégâts absorbés par l'armure
        physicalReduced = damageInfo.rawPhysicalDamage - finalPhysicalDamage;

        // Garantit au moins 1 dégât minimum par coup
        return Mathf.Max(finalPhysicalDamage, 1f);
    }

    /// <summary>
    /// Recalcule l'armure totale quand le joueur change d'équipement
    /// </summary>
    public void RefreshArmorStats(List<ItemData> equippedItems)
    {
        armorClassique = 0f;
        armorTranchant = 0f;
        armorContendant = 0f;
        armorPercant = 0f;

        if (equippedItems == null) return;

        foreach (ItemData piece in equippedItems)
        {
            if (piece == null || piece.itemType != ItemType.Equipment) continue;

            switch (piece.armorType)
            {
                case DamageType.Classique: armorClassique += piece.armorPoints; break;
                case DamageType.Tranchant: armorTranchant += piece.armorPoints; break;
                case DamageType.Contendant: armorContendant += piece.armorPoints; break;
                case DamageType.Percant: armorPercant += piece.armorPoints; break;
            }
        }
    }
}