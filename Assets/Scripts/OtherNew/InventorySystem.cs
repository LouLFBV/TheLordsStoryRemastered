using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem instance;

    public ItemDataDatabase itemDatabase;

    [Header("Other scripts References")]
    [SerializeField] private EquipmentSystem equipment;
    public NewItemActionsSystem itemActionsSystem;
    [SerializeField] private PlayerController player;

    [Header("Inventory System Variables (Fixed Arrays)")]
    // 🔴 On utilise désormais des tableaux de taille fixe au lieu de listes dynamiques
    [SerializeField] private Transform inventoryRessourcesSlotsParent;
    [SerializeField] private ItemInInventory[] contentRessources;

    [SerializeField] private Transform inventoryEquipmentSlotsParent;
    [SerializeField] private ItemInInventory[] contentEquipment;

    [SerializeField] private Transform inventoryCraftSlotsParent;
    [SerializeField] private ItemInInventory[] contentCraft;

    public Sprite emptySlotVisual;

    [SerializeField] private UINavigationManager navManager;

    // Constantes de tailles
    const int InventoryRessourcesCraftSize = 20;
    const int EquipmentSize = 18;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialisation des tableaux avec leurs tailles fixes si ce n'est pas déjà fait dans l'inspecteur
        if (contentRessources == null || contentRessources.Length != InventoryRessourcesCraftSize)
            contentRessources = new ItemInInventory[InventoryRessourcesCraftSize];

        if (contentCraft == null || contentCraft.Length != InventoryRessourcesCraftSize)
            contentCraft = new ItemInInventory[InventoryRessourcesCraftSize];

        if (contentEquipment == null || contentEquipment.Length != EquipmentSize)
            contentEquipment = new ItemInInventory[EquipmentSize];

        // Sécurité : On s'assure que chaque case du tableau contient bien une instance de classe (vide au début)
        InitializeArraySlots(contentRessources);
        InitializeArraySlots(contentCraft);
        InitializeArraySlots(contentEquipment);
    }

    private void InitializeArraySlots(ItemInInventory[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null) array[i] = new ItemInInventory();
        }
    }

    private void Start()
    {
        RefreshContent();
    }

    public void AddItem(ItemData item)
    {
        Debug.Log("Adding item: " + item.itemName);

        ItemInInventory[] targetArray = GetTargetArray(item.itemType);

        // Cas spécial flèches (On garde ta logique)
        if (equipment.arrowItemInInventory.itemData != null)
        {
            if (item.damageType == equipment.arrowItemInInventory.itemData.damageType
                && item.equipmentType == EquipmentType.Arrow)
            {
                equipment.arrowItemInInventory.count++;
                equipment.UpdateArrowsText();
                BowBehaviour.instance.UpdateQuiverVisual(equipment.arrowItemInInventory.count);
                return;
            }
        }

        bool itemAdded = false;

        // 1. Recherche d'un stack existant non plein
        if (item.stackable)
        {
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i].itemData == item && targetArray[i].count < item.maxStack)
                {
                    targetArray[i].count++;
                    itemAdded = true;
                    break;
                }
            }
        }

        // 2. Si pas trouvé de stack, on cherche le PREMIER emplacement vide (index le plus bas)
        if (!itemAdded)
        {
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i].itemData == null) // Case libre !
                {
                    targetArray[i].itemData = item;
                    targetArray[i].count = 1;
                    itemAdded = true;
                    break;
                }
            }
        }

        if (!itemAdded)
        {
            Debug.LogWarning($"Impossible d'ajouter {item.itemName}, l'inventaire de cette catégorie est plein.");
        }

        RefreshContent();
    }

    public void RemoveItem(ItemData item)
    {
        ItemInInventory[] targetArray = GetTargetArray(item.itemType);

        // On cherche le premier objet correspondant en partant de la fin (ou du début, au choix)
        for (int i = 0; i < targetArray.Length; i++)
        {
            if (targetArray[i].itemData == item)
            {
                if (targetArray[i].count > 1)
                {
                    targetArray[i].count--;
                }
                else
                {
                    // Vrai nettoyage de la case : on remet à blanc sans détruire l'index
                    targetArray[i].itemData = null;
                    targetArray[i].count = 0;
                }
                break;
            }
        }

        RefreshContent();
    }

    // Fonction de déplacement manuel (Crucial pour le glisser-déposer ou l'indexation comme dans ton coffre !)
    public void MoveItem(ItemType type, int fromIndex, int toIndex)
    {
        ItemInInventory[] targetArray = GetTargetArray(type);

        if (fromIndex < 0 || fromIndex >= targetArray.Length || toIndex < 0 || toIndex >= targetArray.Length) return;

        // Inversion classique de deux cases (Swap)
        ItemInInventory temp = targetArray[fromIndex];
        targetArray[fromIndex] = targetArray[toIndex];
        targetArray[toIndex] = temp;

        RefreshContent();
    }

    private ItemInInventory[] GetTargetArray(ItemType type)
    {
        return type switch
        {
            ItemType.Equipment or ItemType.Consumable => contentEquipment,
            ItemType.Ressource => contentRessources,
            ItemType.Craft => contentCraft,
            _ => contentRessources
        };
    }

    public List<ItemInInventory> GetContent()
    {
        // On fusionne les tableaux en ignorant les slots vides pour les scripts tiers qui demandent tout d'un coup
        List<ItemInInventory> content = new List<ItemInInventory>();
        content.AddRange(contentRessources.Where(i => i.itemData != null));
        content.AddRange(contentCraft.Where(i => i.itemData != null));
        content.AddRange(contentEquipment.Where(i => i.itemData != null));
        return content;
    }

    public ItemInInventory[] GetContentEquipment() => contentEquipment;
    public ItemInInventory[] GetPlayerRessourcesList() => contentRessources;
    public ItemInInventory[] GetPlayerCraftList() => contentCraft;

    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        ItemInInventory[] targetArray = GetTargetArray(item.itemType);

        int total = 0;
        for (int i = 0; i < targetArray.Length; i++)
        {
            if (targetArray[i].itemData == item)
                total += targetArray[i].count;
        }
        return total;
    }

    public void RefreshContent()
    {
        // 1. Ressources et Craft utilisent le composant "SlotInventory" (Tooltip)
        RefreshResourcesAndCraftContent(inventoryRessourcesSlotsParent, contentRessources, true);
        RefreshResourcesAndCraftContent(inventoryCraftSlotsParent, contentCraft, false);

        // 2. L'équipement utilise le composant "Slot" (Action Panel)
        RefreshEquipmentContent(inventoryEquipmentSlotsParent, contentEquipment);
    }

    // 🟢 Gestion des slots de type "SlotInventory" (Ressources / Craft)
    private void RefreshResourcesAndCraftContent(Transform slotsParent, ItemInInventory[] contentArray, bool isResource)
    {
        int childCount = slotsParent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            SlotInventory currentSlot = slotsParent.GetChild(i).GetComponent<SlotInventory>();
            if (currentSlot == null) continue;

            // Liaison des données d'indexation fixe
            currentSlot.arrayIndex = i;
            currentSlot.isResource = isResource;

            if (i < contentArray.Length && contentArray[i] != null && contentArray[i].itemData != null)
            {
                currentSlot.item = contentArray[i].itemData;
                currentSlot.count = contentArray[i].count;
                currentSlot.itemVisual.sprite = contentArray[i].itemData.visual;
                currentSlot.SetSlotState(true); // Active le fond/contour si implémenté


                currentSlot.countTexte.text = contentArray[i].count.ToString();
                currentSlot.countTexte.enabled = true;
            }
            else
            {
                // Case vide du tableau
                currentSlot.item = null;
                currentSlot.count = 0;
                currentSlot.itemVisual.sprite = emptySlotVisual;
                currentSlot.countTexte.enabled = false;
                currentSlot.SetSlotState(false);
            }
        }
    }

    // 🟢 Gestion des slots de type "Slot" (Équipement)
    private void RefreshEquipmentContent(Transform slotsParent, ItemInInventory[] contentArray)
    {
        int childCount = slotsParent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Slot currentSlot = slotsParent.GetChild(i).GetComponent<Slot>();
            if (currentSlot == null) continue;

            // Note : Si tu veux pouvoir déplacer tes équipements à l'index plus tard,
            // tu pourras ajouter un "public int arrayIndex;" dans ton script Slot.cs

            if (i < contentArray.Length && contentArray[i] != null && contentArray[i].itemData != null)
            {
                currentSlot.item = contentArray[i].itemData;
                currentSlot.itemVisual.sprite = contentArray[i].itemData.visual;

                // Gestion de l'affichage de la quantité (ex: consommables empilés dans l'onglet équipement)
                if (contentArray[i].itemData.stackable && contentArray[i].count > 1)
                {
                    currentSlot.countTexte.text = contentArray[i].count.ToString();
                    currentSlot.countTexte.gameObject.SetActive(true);
                }
                else
                {
                    if (currentSlot.countTexte != null) currentSlot.countTexte.gameObject.SetActive(false);
                }
            }
            else
            {
                // Case vide du tableau
                currentSlot.item = null;
                currentSlot.itemVisual.sprite = emptySlotVisual;
                if (currentSlot.countTexte != null) currentSlot.countTexte.gameObject.SetActive(false);
            }
        }
    }

    // Les vérifications de remplissage comptent désormais les slots occupés (non-null)
    public bool IsFullRessources() => contentRessources.Count(i => i.itemData != null) >= InventoryRessourcesCraftSize;
    public bool IsFullCraft() => contentCraft.Count(i => i.itemData != null) >= InventoryRessourcesCraftSize;
    public bool IsFullEquipment() => contentEquipment.Count(i => i.itemData != null) >= EquipmentSize;

    public void ClearContent()
    {
        ResetArray(contentRessources);
        ResetArray(contentCraft);
        ResetArray(contentEquipment);
        RefreshContent();
    }
    private void ResetArray(ItemInInventory[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i].itemData = null;
            array[i].count = 0;
        }
    }

    public bool KeyIsInInventory(ItemData itemData)
    {
        Debug.Log($"Checking if item {itemData.itemName} is in inventory...");
        return contentRessources.Any(i => i.itemData == itemData);
    }

    #region SaveSystem (Index-Safe)
    public InventorySaveData GetSaveData()
    {
        InventorySaveData data = new InventorySaveData
        {
            ressources = SaveArray(contentRessources),
            craft = SaveArray(contentCraft),
            equipment = SaveArray(contentEquipment)
        };
        return data;
    }

    private List<ItemInInventorySave> SaveArray(ItemInInventory[] array)
    {
        List<ItemInInventorySave> savedList = new List<ItemInInventorySave>();
        for (int i = 0; i < array.Length; i++)
        {
            // On sauvegarde même les cases vides en mettant un ID à "" ou null 
            // pour mémoriser l'emplacement exact (l'index) de chaque objet !
            savedList.Add(new ItemInInventorySave
            {
                itemID = array[i].itemData != null ? array[i].itemData.itemID : "",
                count = array[i].count,
                slotIndex = i // 🟢 On stocke l'index de la case !
            });
        }
        return savedList;
    }

    public void LoadSaveData(InventorySaveData data)
    {
        if (data == null) return;

        ResetArray(contentRessources);
        ResetArray(contentCraft);
        ResetArray(contentEquipment);

        if (data.ressources != null) LoadArray(data.ressources, contentRessources);
        if (data.craft != null) LoadArray(data.craft, contentCraft);
        if (data.equipment != null) LoadArray(data.equipment, contentEquipment);

        RefreshContent();
    }

    private void LoadArray(List<ItemInInventorySave> savedList, ItemInInventory[] array)
    {
        foreach (var savedItem in savedList)
        {
            if (savedItem.slotIndex < 0 || savedItem.slotIndex >= array.Length) continue;
            if (string.IsNullOrEmpty(savedItem.itemID)) continue;

            ItemData itemData = itemDatabase.GetItemByID(savedItem.itemID);
            if (itemData == null) continue;

            array[savedItem.slotIndex].itemData = itemData;
            array[savedItem.slotIndex].count = savedItem.count;
        }
    }
    #endregion
}

[System.Serializable]
public class ItemInInventory
{
    public ItemData itemData;
    public int count;
}

[System.Serializable]
public class ItemInInventorySave
{
    public string itemID;
    public int count;
    public int slotIndex; 
}

[System.Serializable]
public class InventorySaveData
{
    public List<ItemInInventorySave> ressources;
    public List<ItemInInventorySave> craft;
    public List<ItemInInventorySave> equipment;
}