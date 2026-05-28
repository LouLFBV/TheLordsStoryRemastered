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

    [Header("Inventory System Variables")]

    [SerializeField] private Transform inventoryRessourcesSlotsParent;
    [SerializeField] private List<ItemInInventory> contentRessources = new List<ItemInInventory>();

    [SerializeField] private Transform inventoryEquipmentSlotsParent;
    [SerializeField] private List<ItemInInventory> contentEquipment = new List<ItemInInventory>();

    [SerializeField] private Transform inventoryCraftSlotsParent;
    [SerializeField] private List<ItemInInventory> contentCraft = new List<ItemInInventory>();

    public Sprite emptySlotVisual;

    [SerializeField] private UINavigationManager navManager;


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
    }

    private void Start()
    {
        //CloseInventory();
        RefreshContent();
    }


    //public void OpenInventory()
    //{
    //    inventoryPanel.SetActive(true);
    //    player.StateMachine.ChangeState(PlayerStateType.UI);
    //    RefreshContent();

    //    // On force la State Machine à passer dans un état "Menu" ou "Idle" 
    //    // pour empêcher le joueur de frapper/courir pendant qu'il trie ses objets
    //    //player.StateMachine.ChangeState(PlayerStateType.Idle);

    //    // Si tu as un état spécifique "UI" ou "Pause", c'est encore mieux :
    //    // player.StateMachine.ChangeState(PlayerStateType.InventoryOpen);

    //    PaletteSystem.instance.UpdateEquipmentsDesequipButtons();

    //    if (navManager != null) navManager.onCancel = CloseInventory;
    //}

    //public void CloseInventory()
    //{
    //    if (itemActionsSystem.actionPanel.activeSelf) return;

    //    inventoryPanel.SetActive(false); 
    //    itemActionsSystem.actionPanel.SetActive(false);
    //    TooltipSystem.instance.Hide();

    //    PaletteSystem.instance.UpdateEquipmentsDesequipButtons();

    //    if (navManager != null) navManager.onCancel = null;
    //}


    public void AddItem(ItemData item)
    {
        Debug.Log("Adding item: " + item.itemName);

        List<ItemInInventory> targetList = null;

        switch (item.itemType)
        {
            case ItemType.Equipment:
            case ItemType.Consumable:
                if (IsFullEquipment())
                {
                    Debug.LogWarning("Cannot add item, equipment inventory is full.");
                    return;
                }
                targetList = contentEquipment;
                break;

            case ItemType.Ressource:
                if (IsFullRessources())
                {
                    Debug.LogWarning("Cannot add item, ressources inventory is full.");
                    return;
                }
                targetList = contentRessources;
                break;

            case ItemType.Craft:
                if (IsFullCraft())
                {
                    Debug.LogWarning("Cannot add item, craft inventory is full.");
                    return;
                }
                targetList = contentCraft;
                break;

            default:
                if (IsFullRessources())
                {
                    Debug.LogWarning("Cannot add item, ressources inventory is full.");
                    return;
                }
                targetList = contentRessources;
                break;
        }

        // Cas spécial flèches
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

       

        // Recherche de stacks existants
        var stacks = targetList.Where(i => i.itemData == item).ToList();

        bool itemAdded = false;

        if (stacks.Count > 0 && item.stackable)
        {
            foreach (var stack in stacks)
            {
                if (stack.count < item.maxStack)
                {
                    stack.count++;
                    itemAdded = true;
                    break;
                }
            }
        }

        if (!itemAdded)
        {
            targetList.Add(new ItemInInventory
            {
                itemData = item,
                count = 1
            });
        }

        RefreshContent();
    }

    public void RemoveItem(ItemData item)
    {
        List<ItemInInventory> targetList = null;

        switch (item.itemType)
        {
            case ItemType.Equipment:
            case ItemType.Consumable:
                targetList = contentEquipment;
                break;

            case ItemType.Ressource:
                targetList = contentRessources;
                break;

            case ItemType.Craft:
                targetList = contentCraft;
                break;

            default:
                targetList = contentRessources;
                break;
        }

        ItemInInventory itemInInventory = targetList
            .FirstOrDefault(i => i.itemData == item);

        if (itemInInventory == null)
            return;

        if (itemInInventory.count > 1)
            itemInInventory.count--;
        else
            targetList.Remove(itemInInventory);

        RefreshContent();
    }

    public List<ItemInInventory> GetContent()
    {
        List<ItemInInventory> content = new List<ItemInInventory>();

        content.AddRange(contentRessources);
        content.AddRange(contentCraft);
        content.AddRange(contentEquipment);

        return content;
    }

    public List<ItemInInventory> GetContentEquipment()
    {
        return contentEquipment;
    }

    public List<ItemInInventory> GetPlayerRessourcesList()
    {
        return contentRessources;
    }

    public List<ItemInInventory> GetPlayerCraftList()
    {
        return contentCraft;
    }
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;

        // On récupère la liste cible selon le type
        List<ItemInInventory> targetList = item.itemType switch
        {
            ItemType.Equipment or ItemType.Consumable => contentEquipment,
            ItemType.Ressource => contentRessources,
            ItemType.Craft => contentCraft,
            _ => null // Cas inconnu
        };

        if (targetList == null)
        {
            Debug.LogWarning($"Type d'item {item.itemType} non géré dans GetItemCount");
            return 0;
        }

        // Version boucle simple (plus performante que LINQ si appelée souvent)
        int total = 0;
        foreach (var slot in targetList)
        {
            if (slot.itemData == item)
                total += slot.count;
        }
        return total;
    }

    public void RefreshContent()
    {
        RefreshCraftContent();
        RefreshRessourcesContent();
        RefreshEquipmentContent();
    }
    public void RefreshCraftContent()
    {
        //On vide tous les slots / visuels
        for (int i = 0; i < inventoryCraftSlotsParent.childCount; i++)
        {
            Slot currentSlot = inventoryCraftSlotsParent.GetChild(i).GetComponent<Slot>();

            currentSlot.item = null;
            currentSlot.itemVisual.sprite = emptySlotVisual;
            currentSlot.countTexte.enabled = false;
        }

        //On peuple le visuel des slots selon le contenu de l'inventaire
        for (int i = 0; i < contentCraft.Count; i++)
        {
            Slot currentSlot = inventoryCraftSlotsParent.GetChild(i).GetComponent<Slot>();
            if (contentCraft[i].itemData == null)
            {
                RemoveItem(contentCraft[i].itemData);
                continue;
            }
            currentSlot.item = contentCraft[i].itemData;
            currentSlot.itemVisual.sprite = contentCraft[i].itemData.visual;

            if (currentSlot.item.stackable)
            {
                currentSlot.countTexte.text = contentCraft[i].count.ToString();
                currentSlot.countTexte.enabled = true;
            }
        }
    }
    public void RefreshRessourcesContent()
    {
        //On vide tous les slots / visuels
        for (int i = 0; i < inventoryRessourcesSlotsParent.childCount; i++)
        {
            Slot currentSlot = inventoryRessourcesSlotsParent.GetChild(i).GetComponent<Slot>();

            currentSlot.item = null;
            currentSlot.itemVisual.sprite = emptySlotVisual;
            currentSlot.countTexte.enabled = false;
        }

        //On peuple le visuel des slots selon le contenu de l'inventaire
        for (int i = 0; i < contentRessources.Count; i++)
        {
            Slot currentSlot = inventoryRessourcesSlotsParent.GetChild(i).GetComponent<Slot>();
            if (contentRessources[i].itemData == null)
            {
                RemoveItem(contentRessources[i].itemData);
                continue;
            }
            currentSlot.item = contentRessources[i].itemData;
            currentSlot.itemVisual.sprite = contentRessources[i].itemData.visual;

            if (currentSlot.item.stackable)
            {
                currentSlot.countTexte.text = contentRessources[i].count.ToString();
                currentSlot.countTexte.enabled = true;
            }
        }
    }
    public void RefreshEquipmentContent()
    {
        //On vide tous les slots / visuels
        for (int i = 0; i < inventoryEquipmentSlotsParent.childCount; i++)
        {
            Slot currentSlot = inventoryEquipmentSlotsParent.GetChild(i).GetComponent<Slot>();

            currentSlot.item = null;
            currentSlot.itemVisual.sprite = emptySlotVisual;
            currentSlot.countTexte.enabled = false;
        }

        //On peuple le visuel des slots selon le contenu de l'inventaire
        for (int i = 0; i < contentEquipment.Count; i++)
        {
            Slot currentSlot = inventoryEquipmentSlotsParent.GetChild(i).GetComponent<Slot>();
            if (contentEquipment[i].itemData == null)
            {
                RemoveItem(contentEquipment[i].itemData);
                continue;
            }
            currentSlot.item = contentEquipment[i].itemData;
            currentSlot.itemVisual.sprite = contentEquipment[i].itemData.visual;

            if (currentSlot.item.stackable)
            {
                currentSlot.countTexte.text = contentEquipment[i].count.ToString();
                currentSlot.countTexte.enabled = true;
            }
        }
    }

    public bool IsFullRessources()
    {
        return contentRessources.Count == InventoryRessourcesCraftSize;
    }
    public bool IsFullCraft()
    {
        return contentCraft.Count == InventoryRessourcesCraftSize;
    }
    public bool IsFullEquipment()
    {
        return contentEquipment.Count == EquipmentSize;
    }

    public void ClearContent()
    {
        ClearContentRessources();
        ClearContentCarft();
        ClearContentEquipment();
    }
    public void ClearContentRessources()
    {
        contentRessources.Clear();
    }
    public void ClearContentCarft()
    {
        contentCraft.Clear();
    }
    public void ClearContentEquipment()
    {
        contentEquipment.Clear();
    }

    public bool KeyIsInInventory(ItemData itemData)
    {
        return contentRessources.Any(i => i.itemData == itemData);
    }

    #region SaveSystem

    void SaveList(List<ItemInInventory> source, List<ItemInInventorySave> destination)
    {
        foreach (var item in source)
        {
            destination.Add(new ItemInInventorySave
            {
                itemID = item.itemData.itemID,
                count = item.count
            });
        }
    }
    public InventorySaveData GetSaveData()
    {
        InventorySaveData data = new InventorySaveData
        {
            ressources = new List<ItemInInventorySave>(contentRessources.Count),
            craft = new List<ItemInInventorySave>(contentCraft.Count),
            equipment = new List<ItemInInventorySave>(contentEquipment.Count)
        };

        SaveList(contentRessources, data.ressources);
        SaveList(contentCraft, data.craft);
        SaveList(contentEquipment, data.equipment);

        return data;
    }

    void LoadList(List<ItemInInventorySave> source, List<ItemInInventory> destination)
    {
        foreach (var savedItem in source)
        {
            ItemData itemData = itemDatabase.GetItemByID(savedItem.itemID);
            if (itemData == null) continue;

            destination.Add(new ItemInInventory
            {
                itemData = itemData,
                count = savedItem.count
            });
        }
    }
    public void LoadSaveData(InventorySaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("InventorySaveData is null");
            return;
        }

        contentRessources.Clear();
        contentCraft.Clear();
        contentEquipment.Clear();

        if (data.ressources != null)
            LoadList(data.ressources, contentRessources);

        if (data.craft != null)
            LoadList(data.craft, contentCraft);

        if (data.equipment != null)
            LoadList(data.equipment, contentEquipment);

        RefreshContent();
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
}

[System.Serializable]
public class InventorySaveData
{
    public List<ItemInInventorySave> ressources;
    public List<ItemInInventorySave> craft;
    public List<ItemInInventorySave> equipment;
}