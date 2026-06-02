using UnityEngine;
using System.Collections.Generic;

public class ArmorSystem : MonoBehaviour
{
    [Header("Physical Resistances (Points)")]
    public float armorClassique = 10f;
    public float armorTranchant = 5f;
    public float armorContendant = 5f;
    public float armorPercant = 0f; // Pas de réduction pour les dégâts perçants (selon ton com')

    /// <summary>
    /// Calcule la réduction des dégâts physiques uniquement.
    /// Les effets (Feu, Glace...) ignorent cette réduction.
    /// </summary>
    public float CalculateReducedDamage(DamageInfo damageInfo, out float physicalReduced)
    {
        float physicalDefensePoints = 0f;

        // 1. On récupère la bonne défense selon le type de dégât de l'arme
        switch (damageInfo.physicalType)
        {
            case DamageType.Tranchant: physicalDefensePoints = armorTranchant; break;
            case DamageType.Contendant: physicalDefensePoints = armorContendant; break;
            case DamageType.Percant: physicalDefensePoints = armorPercant; break; // Vaut 0
            case DamageType.Classique: physicalDefensePoints = armorClassique; break;
        }

        // 2. Application de la formule à rendement décroissant (évite le "0 dégât")
        float physicalMultiplier = 100f / (100f + physicalDefensePoints);
        float finalPhysicalDamage = damageInfo.rawPhysicalDamage * physicalMultiplier;
        physicalReduced = damageInfo.rawPhysicalDamage - finalPhysicalDamage;

        // Sécurité : au moins 1 point de dégât physique est infligé
        return Mathf.Max(finalPhysicalDamage, 1f);
    }

    /// <summary>
    /// Recalcule l'armure totale quand le joueur change d'équipement
    /// </summary>
    public void RefreshArmorStats(List<ItemData> equippedItems)
    {
        // On remet à zéro avant de recalculer
        armorClassique = 0f; armorTranchant = 0f; armorContendant = 0f; armorPercant = 0f;

        if (equippedItems == null) return;

        foreach (ItemData piece in equippedItems)
        {
            if (piece == null || piece.itemType != ItemType.Equipment) continue;

            // On cumule les points d'armure selon l'armorType de la pièce d'armure
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