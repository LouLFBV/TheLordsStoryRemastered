using UnityEngine;
using System.Collections.Generic;

public class ArmorSystem : MonoBehaviour
{
    [Header("Physical Resistances (0 to 100 %)")]
    public float armorClassique = 10f;
    public float armorTranchant = 5f;
    public float armorContendant = 5f;
    public float armorPercant = 0f;

    /// <summary>
    /// Calcule la réduction des dégâts physiques sur une base linéaire où 100 armure = 100% de réduction.
    /// Les effets (Feu, Glace...) ignorent cette réduction.
    /// </summary>
    public float CalculateReducedDamage(DamageInfo damageInfo, out float physicalReduced)
    {
        float physicalDefensePoints = 0f;

        // 1. On récupère les points de la bonne défense selon le type de dégât reçu
        switch (damageInfo.physicalType)
        {
            case DamageType.Tranchant: physicalDefensePoints = armorTranchant; break;
            case DamageType.Contendant: physicalDefensePoints = armorContendant; break;
            case DamageType.Percant: physicalDefensePoints = armorPercant; break;
            case DamageType.Classique: physicalDefensePoints = armorClassique; break;
        }

        float reductionPercent = Mathf.Clamp(physicalDefensePoints, 0f, 100f) / 100f;

        // Calcul du multiplicateur : si 100% de réduction, le multiplicateur vaut 0 (0 dégât)
        float physicalMultiplier = 1f - reductionPercent;

        // Application sur les dégâts bruts
        float finalPhysicalDamage = damageInfo.rawPhysicalDamage * physicalMultiplier;

        // Quantité de dégâts absorbée par l'armure
        physicalReduced = damageInfo.rawPhysicalDamage - finalPhysicalDamage;
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