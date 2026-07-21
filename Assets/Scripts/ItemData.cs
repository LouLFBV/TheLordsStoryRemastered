using UnityEngine;

[CreateAssetMenu(fileName = "Item",menuName = "Items/New Item")]
public class ItemData : ScriptableObject
{
    [Header("Data")]
    public string itemID;
    public string itemName;
    [TextArea] public string description;
    public Sprite visual;
    public GameObject prefab;
    public bool stackable;
    public int maxStack;
    public int prix = 10;
    public int levelAmelioration = 0;
    public int metalCost = 15;
    public RecipeData recipe;
    public GameObject iconeMap;
    public bool isVendable = true;

    [SerializeField] private int purchaseAmount = 1;
    public int PurchaseAmount => purchaseAmount;
    public int TotalPrice => prix * PurchaseAmount;

    [Header("Effects")]
    public float healthEffect;

    [Header("Armor Stats")]
    public float armorPoints;
    public DamageType armorType;

    [Header("Attack Stats")]
    public WeaponCombatData combatData;
    public float attackPoints;
    public float poiseDamage;
    public HandWeapon handWeaponType;
    public DamageType damageType;
    public Effet effet;
    public float stunDuration = 1.5f;

    [Header("Bow Stats")]
    public float rangeMin;
    public float rangeMax;

    [Header("Types")]
    public ItemType itemType;
    public EquipmentType equipmentType;

    [Header("Camera Shake")]
    public float cameraShakeIntensity = 0.15f;
    public float cameraShakeDuration = 0.2f;


    [HideInInspector] public bool isInstance = false; // Permet de savoir si c'est une copie

    private void OnValidate()
    {
        // Si l'ID est vide, null ou composé uniquement d'espaces
        if (string.IsNullOrWhiteSpace(itemID))
        {
            GenerateUniqueID();
        }
    }


    // Fonction pour créer une copie unique
    public ItemData CreateInstance()
    {
        ItemData clone = Instantiate(this);
        clone.name = this.itemName; // Nettoie le nom (enlève le "(Clone)")
        clone.isInstance = true;
        return clone;
    }

    // Fonction pour recalculer les stats quand on charge une sauvegarde
    public void RestoreLevel(int savedLevel)
    {
        // On simule les améliorations du forgeron pour retomber sur les bonnes stats
        for (int i = 0; i < savedLevel; i++)
        {
            levelAmelioration++;
            if (equipmentType != EquipmentType.Weapon)
                armorPoints += 10;
            else
                attackPoints += 10;

            // Si tu as d'autres stats qui montent (comme la portée de l'arc), ajoute-les ici
            if (handWeaponType == HandWeapon.Bow)
                rangeMax += 5;
        }
    }


    [ContextMenu("Forcer la génération d'un nouvel ID")]
    public void GenerateUniqueID()
    {
        // Crée un identifiant unique universel (ex: "f7b3a21a-614d-4e93-b26a-9b769f3a9d9c")
        itemID = System.Guid.NewGuid().ToString();

#if UNITY_EDITOR
        // Force Unity à enregistrer la modification du fichier ScriptableObject sur le disque
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

}


public enum ItemType
{
    Consumable,
    Equipment,
    QuestItem,
    Ressource,
    Key,
    Constructible,
    Destructible,
    Recipe,
    Map,
    None, 
    Craft
}

public enum EquipmentType
{
    Head,
    Chest,
    Legs,
    Feet,
    Hands,
    Weapon,
    Shield,
    None,
    Arrow
}

public enum HandWeapon
{
    OneHanded,
    TwoHanded,
    Bow
}


public enum DamageType
{
    Classique,
    Percant,
    Contendant,
    Tranchant,
}

public enum Effet
{
    None,
    Feu,
    Foudre,
    Glace,
}