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
    [SerializeField] private Transform inventoryRessourcesSlotsParent;
    [SerializeField] private ItemInInventory[] contentRessources;

    [SerializeField] private Transform inventoryEquipmentSlotsParent;
    [SerializeField] private ItemInInventory[] contentEquipment;

    [SerializeField] private Transform inventoryCraftSlotsParent;
    [SerializeField] private ItemInInventory[] contentCraft;

    public Sprite emptySlotVisual;

    public Sprite itemLevel1Icon, itemLevel2Icon, itemLevel3Icon;
    public Sprite itemLevel1IconWhite, itemLevel2IconWhite, itemLevel3IconWhite;

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

        if (contentRessources == null || contentRessources.Length != InventoryRessourcesCraftSize)
            contentRessources = new ItemInInventory[InventoryRessourcesCraftSize];

        if (contentCraft == null || contentCraft.Length != InventoryRessourcesCraftSize)
            contentCraft = new ItemInInventory[InventoryRessourcesCraftSize];

        if (contentEquipment == null || contentEquipment.Length != EquipmentSize)
            contentEquipment = new ItemInInventory[EquipmentSize];

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

    public void AddItem(ItemData item, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            AddItem(item);
        }
    }

    // 🟢 1. Comparaison personnalisée pour les flèches et autres objets
    public bool IsSameItem(ItemData a, ItemData b)
    {
        if (a == null || b == null) return false;

        // Pour les flèches : Même Effet (ou même ID) ET Même Niveau d'amélioration
        if (a.equipmentType == EquipmentType.Arrow && b.equipmentType == EquipmentType.Arrow)
        {
            return (a.effet == b.effet || a.itemID == b.itemID) && a.levelAmelioration == b.levelAmelioration;
        }

        // Pour les autres objets
        return (a == b || a.itemID == b.itemID) && a.levelAmelioration == b.levelAmelioration;
    }

    public void AddItem(ItemData item)
    {
        Debug.Log("Adding item: " + item.itemName);

        // Si l'objet n'est pas stackable et pas une instance, on crée une instance
        if (!item.stackable && !item.isInstance && item.itemType != ItemType.Key)
        {
            item = item.CreateInstance();
        }

        // Slot de flèches équipées sur l'arc
        if (item.equipmentType == EquipmentType.Arrow && equipment != null && equipment.arrowItemInInventory != null && equipment.arrowItemInInventory.itemData != null)
        {
            if (IsSameItem(item, equipment.arrowItemInInventory.itemData))
            {
                Debug.Log("Flèche ajoutée directement dans le slot d'équipement d'arc !");
                equipment.arrowItemInInventory.count++;
                equipment.UpdateArrowsText();
                if (BowBehaviour.instance != null)
                {
                    BowBehaviour.instance.UpdateQuiverVisual(equipment.arrowItemInInventory.count);
                }
                RefreshContent();
                return;
            }
        }

        ItemInInventory[] targetArray = GetTargetArray(item.itemType);
        bool itemAdded = false;

        // 1️⃣ Tentative d'empilement dans un stack déjà existant de MÊME effet et MÊME niveau
        if (item.stackable)
        {
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i].itemData != null &&
                   IsSameItem(targetArray[i].itemData, item) &&
                   targetArray[i].count < item.maxStack)
                {
                    targetArray[i].count++;
                    itemAdded = true;
                    break;
                }
            }
        }

        // 2️⃣ Si aucun stack correspondant, placement dans une nouvelle case vide
        if (!itemAdded)
        {
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i].itemData == null)
                {
                    // 🔴 CRUCIAL : Si ce sont des flèches et qu'elles ne sont pas encore une instance runtime,
                    // on crée une instance pour que leur niveau d'amélioration soit isolé !
                    if (!item.isInstance && (item.equipmentType == EquipmentType.Arrow || !item.stackable))
                    {
                        item = item.CreateInstance();
                    }

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

    public bool CanAddItem(ItemData item, int quantity)
    {
        if (item == null) return false;

        if (item.equipmentType == EquipmentType.Arrow && equipment != null && equipment.arrowItemInInventory != null && equipment.arrowItemInInventory.itemData != null)
        {
            if (IsSameItem(item, equipment.arrowItemInInventory.itemData))
            {
                return true;
            }
        }

        ItemInInventory[] targetArray = GetTargetArray(item.itemType);
        int remaining = quantity;

        if (item.stackable)
        {
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i].itemData != null && IsSameItem(targetArray[i].itemData, item))
                {
                    int space = item.maxStack - targetArray[i].count;

                    if (space > 0)
                    {
                        remaining -= space;

                        if (remaining <= 0)
                            return true;
                    }
                }
            }
        }

        int emptySlots = 0;

        for (int i = 0; i < targetArray.Length; i++)
        {
            if (targetArray[i].itemData == null)
                emptySlots++;
        }

        int capacity = emptySlots * (item.stackable ? item.maxStack : 1);

        return remaining <= capacity;
    }

    public void RemoveItemFromSlot(ItemType itemType, int slotIndex, int amount = 1)
    {
        ItemInInventory[] targetArray = GetTargetArray(itemType);

        if (slotIndex < 0 || slotIndex >= targetArray.Length) return;

        if (targetArray[slotIndex].itemData != null)
        {
            targetArray[slotIndex].count -= amount;

            if (targetArray[slotIndex].count <= 0)
            {
                targetArray[slotIndex].itemData = null;
                targetArray[slotIndex].count = 0;
            }

            RefreshContent();
        }
    }

    public void RemoveItem(ItemData item)
    {
        if (item == null) return;
        ItemInInventory[] targetArray = GetTargetArray(item.itemType);

        for (int i = 0; i < targetArray.Length; i++)
        {
            if (targetArray[i].itemData != null && IsSameItem(targetArray[i].itemData, item))
            {
                if (targetArray[i].count > 1)
                {
                    targetArray[i].count--;
                }
                else
                {
                    targetArray[i].itemData = null;
                    targetArray[i].count = 0;
                }
                break;
            }
        }

        RefreshContent();
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        ItemInInventory[] targetArray = GetTargetArray(item.itemType);

        int total = 0;
        for (int i = 0; i < targetArray.Length; i++)
        {
            if (targetArray[i].itemData != null && IsSameItem(targetArray[i].itemData, item))
            {
                total += targetArray[i].count;
            }
        }
        return total;
    }

    public void MoveItem(ItemType type, int fromIndex, int toIndex)
    {
        ItemInInventory[] targetArray = GetTargetArray(type);

        if (fromIndex < 0 || fromIndex >= targetArray.Length || toIndex < 0 || toIndex >= targetArray.Length) return;

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
        List<ItemInInventory> content = new List<ItemInInventory>();
        content.AddRange(contentRessources.Where(i => i.itemData != null));
        content.AddRange(contentCraft.Where(i => i.itemData != null));
        content.AddRange(contentEquipment.Where(i => i.itemData != null));
        return content;
    }

    public ItemInInventory[] GetContentEquipment() => contentEquipment;
    public ItemInInventory[] GetPlayerRessourcesList() => contentRessources;
    public ItemInInventory[] GetPlayerCraftList() => contentCraft;

    public void ConsolidateStacks()
    {
        ConsolidateArray(contentEquipment);
        ConsolidateArray(contentRessources);
        ConsolidateArray(contentCraft);
        RefreshContent();
    }

    private void ConsolidateArray(ItemInInventory[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].itemData == null || !array[i].itemData.stackable) continue;

            for (int j = i + 1; j < array.Length; j++)
            {
                if (array[j].itemData != null && IsSameItem(array[i].itemData, array[j].itemData))
                {
                    int maxStack = array[i].itemData.maxStack;
                    int spaceLeft = maxStack - array[i].count;

                    if (spaceLeft > 0)
                    {
                        int transfer = Mathf.Min(spaceLeft, array[j].count);
                        array[i].count += transfer;
                        array[j].count -= transfer;

                        if (array[j].count <= 0)
                        {
                            array[j].itemData = null;
                            array[j].count = 0;
                        }
                    }
                }
            }
        }
    }
    public void RefreshContent()
    {
        RefreshResourcesAndCraftContent(inventoryRessourcesSlotsParent, contentRessources, true);
        RefreshResourcesAndCraftContent(inventoryCraftSlotsParent, contentCraft, false);
        RefreshEquipmentContent(inventoryEquipmentSlotsParent, contentEquipment);
    }

    private void RefreshResourcesAndCraftContent(Transform slotsParent, ItemInInventory[] contentArray, bool isResource)
    {
        int childCount = slotsParent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            SlotInventory currentSlot = slotsParent.GetChild(i).GetComponent<SlotInventory>();
            if (currentSlot == null) continue;

            currentSlot.arrayIndex = i;
            currentSlot.isResource = isResource;

            if (i < contentArray.Length && contentArray[i] != null && contentArray[i].itemData != null)
            {
                currentSlot.item = contentArray[i].itemData;
                currentSlot.count = contentArray[i].count;
                currentSlot.itemVisual.sprite = contentArray[i].itemData.visual;
                currentSlot.SetSlotState(true);

                currentSlot.countTexte.text = contentArray[i].count.ToString();
                currentSlot.countTexte.enabled = true;
            }
            else
            {
                currentSlot.item = null;
                currentSlot.count = 0;
                currentSlot.itemVisual.sprite = emptySlotVisual;
                currentSlot.countTexte.enabled = false;
                currentSlot.SetSlotState(false);
            }
        }
    }

    private void RefreshEquipmentContent(Transform slotsParent, ItemInInventory[] contentArray)
    {
        int childCount = slotsParent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Slot currentSlot = slotsParent.GetChild(i).GetComponent<Slot>();
            if (currentSlot == null) continue;

            if (i < contentArray.Length && contentArray[i] != null && contentArray[i].itemData != null)
            {
                currentSlot.item = contentArray[i].itemData;
                currentSlot.itemVisual.sprite = contentArray[i].itemData.visual;

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
                currentSlot.item = null;
                currentSlot.itemVisual.sprite = emptySlotVisual;
                if (currentSlot.countTexte != null) currentSlot.countTexte.gameObject.SetActive(false);
            }
        }
    }

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
        if (itemData == null) return false;
        Debug.Log($"Checking if item {itemData.itemName} is in inventory...");
        return contentRessources.Any(i => i.itemData != null && (i.itemData == itemData || i.itemData.itemID == itemData.itemID));
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
            savedList.Add(new ItemInInventorySave
            {
                itemID = array[i].itemData != null ? array[i].itemData.itemID : "",
                count = array[i].count,
                slotIndex = i,
                levelAmelioration = array[i].itemData != null ? array[i].itemData.levelAmelioration : 0
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

            ItemData baseItemData = itemDatabase.GetItemByID(savedItem.itemID);
            if (baseItemData == null) continue;

            ItemData finalItem = baseItemData;

            // 🟢 2. Si c'est un équipement OU une flèche, on crée une instance et on restaure son niveau
            if (!baseItemData.stackable || baseItemData.equipmentType == EquipmentType.Arrow)
            {
                finalItem = baseItemData.CreateInstance();
                finalItem.RestoreLevel(savedItem.levelAmelioration);
            }

            array[savedItem.slotIndex].itemData = finalItem;
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
    public int levelAmelioration;
}

[System.Serializable]
public class InventorySaveData
{
    public List<ItemInInventorySave> ressources;
    public List<ItemInInventorySave> craft;
    public List<ItemInInventorySave> equipment;
}