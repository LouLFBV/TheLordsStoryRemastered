using System.Collections.Generic;
using UnityEngine;

public class ChestInventory : MonoBehaviour
{
    public static ChestInventory Instance;

    [Header("Chest Inventory (Data Arrays & UI Parents)")]
    public Transform inventoryChestSlotsRessourecesParent;
    public ItemInInventory[] contentRessourcesChest = new ItemInInventory[32];

    public Transform inventoryChestSlotsCraftParent;
    public ItemInInventory[] contentCraftChest = new ItemInInventory[32];

    [Header("Player Inventory (Data Arrays & UI Parents)")]
    public Transform inventoryPlayerSlotsRessourcesParent;
    public ItemInInventory[] contentRessourcePlayer = new ItemInInventory[20];

    public Transform inventoryPlayerSlotsCraftParent;
    public ItemInInventory[] contentCraftPlayer = new ItemInInventory[20];

    [Header("Others")]
    [SerializeField] private GameObject objectsToDisable;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnEnable()
    {
        if (objectsToDisable != null) objectsToDisable.SetActive(false);

        FetchAndSyncPlayerInventory();

        RefreshContentChestInventory();
        RefreshContentPlayerInventory();
    }

    // --- ASPIRATION ET CONVERSION DES LISTES JOUEUR EN TABLEAUX FIXES ---
    public void FetchAndSyncPlayerInventory()
    {
        if (InventorySystem.instance == null) return;

        List<ItemInInventory> playerRessources = InventorySystem.instance.GetPlayerRessourcesList();
        List<ItemInInventory> playerCraft = InventorySystem.instance.GetPlayerCraftList();

        System.Array.Clear(contentRessourcePlayer, 0, contentRessourcePlayer.Length);
        System.Array.Clear(contentCraftPlayer, 0, contentCraftPlayer.Length);

        int ressourcesToCopy = Mathf.Min(playerRessources.Count, contentRessourcePlayer.Length);
        for (int i = 0; i < ressourcesToCopy; i++)
        {
            contentRessourcePlayer[i] = playerRessources[i];
        }

        int craftToCopy = Mathf.Min(playerCraft.Count, contentCraftPlayer.Length);
        for (int i = 0; i < craftToCopy; i++)
        {
            contentCraftPlayer[i] = playerCraft[i];
        }
    }

    // --- REFRESH DE TOUT L'INVENTAIRE JOUEUR DANS L'UI DU COFFRE ---
    public void RefreshContentPlayerInventory()
    {
        if (InventorySystem.instance == null) return;
        Sprite emptySprite = InventorySystem.instance.emptySlotVisual;

        // On synchronise les données depuis la source réelle (Le joueur)
        FetchAndSyncPlayerInventory();

        RefreshGrid(inventoryPlayerSlotsRessourcesParent, contentRessourcePlayer, emptySprite, false, true);
        RefreshGrid(inventoryPlayerSlotsCraftParent, contentCraftPlayer, emptySprite, false, false);
    }

    // --- REFRESH DE TOUT LE COFFRE ---
    public void RefreshContentChestInventory()
    {
        if (InventorySystem.instance == null) return;
        Sprite emptySprite = InventorySystem.instance.emptySlotVisual;

        RefreshGrid(inventoryChestSlotsRessourecesParent, contentRessourcesChest, emptySprite, true, true);
        RefreshGrid(inventoryChestSlotsCraftParent, contentCraftChest, emptySprite, true, false);
    }

    // --- MÉTHODE GÉNÉRIQUE POUR METTRE À JOUR L'UI ---
    private void RefreshGrid(Transform slotsParent, ItemInInventory[] dataArray, Sprite emptySprite, bool isChest, bool isResource)
    {
        if (slotsParent == null) return;

        int childCount = slotsParent.childCount;
        int iterations = Mathf.Min(dataArray.Length, childCount);

        for (int i = 0; i < iterations; i++)
        {
            SlotChest currentSlot = slotsParent.GetChild(i).GetComponent<SlotChest>();
            if (currentSlot == null) continue;

            currentSlot.button.onClick.RemoveAllListeners();

            ItemInInventory itemInInv = dataArray[i];

            if (itemInInv == null || itemInInv.itemData == null)
            {
                currentSlot.item = null;
                currentSlot.itemVisual.sprite = emptySprite;
                currentSlot.countTexte.enabled = false;
                currentSlot.count = 0;
                currentSlot.SetSlotState(false);
            }
            else
            {
                currentSlot.item = itemInInv.itemData;
                currentSlot.itemVisual.sprite = itemInInv.itemData.visual;

                if (currentSlot.item.stackable)
                {
                    currentSlot.countTexte.text = itemInInv.count.ToString();
                    currentSlot.count = itemInInv.count;
                    currentSlot.countTexte.enabled = true;
                }
                else
                {
                    currentSlot.countTexte.enabled = false;
                }

                currentSlot.SetSlotState(true);

                int index = i;
                currentSlot.button.onClick.AddListener(() => HandleSlotClick(index, isChest, isResource));
            }
        }
    }

    // --- GESTION UNIQUE DES TRANSFERTS DIRECTS CORRIGÉE ---
    private void HandleSlotClick(int arrayIndex, bool sourceIsChest, bool sourceIsResource)
    {
        if (InventorySystem.instance == null) return;

        // Détermination du tableau source
        ItemInInventory[] sourceArray = sourceIsChest
            ? (sourceIsResource ? contentRessourcesChest : contentCraftChest)
            : (sourceIsResource ? contentRessourcePlayer : contentCraftPlayer);

        if (sourceArray == null || arrayIndex >= sourceArray.Length) return;

        ItemInInventory itemToTransfer = sourceArray[arrayIndex];
        if (itemToTransfer == null || itemToTransfer.itemData == null) return;

        if (sourceIsChest)
        {
            // --- TRIPLE ACTION : COFFRE -> JOUEUR ---
            // 1. On vérifie d'abord si l'inventaire du joueur a de la place
            bool isFull = itemToTransfer.itemData.itemType == ItemType.Ressource
                ? InventorySystem.instance.IsFullRessources()
                : InventorySystem.instance.IsFullCraft();

            if (isFull)
            {
                Debug.LogWarning("L'inventaire du joueur est plein !");
                return;
            }

            // 2. On l'ajoute directement au vrai système du joueur
            InventorySystem.instance.AddItem(itemToTransfer.itemData);

            // 3. On le retire du tableau fixe du coffre
            if (itemToTransfer.count > 1)
            {
                itemToTransfer.count--;
            }
            else
            {
                sourceArray[arrayIndex] = null;
            }
        }
        else
        {
            // --- TRIPLE ACTION : JOUEUR -> COFFRE ---
            // 1. On tente de l'ajouter dans le tableau fixe du coffre
            bool success = AddToChestArray(itemToTransfer.itemData, 1);

            if (success)
            {
                // 2. On l'enlève de la liste réelle du joueur via sa propre méthode native
                InventorySystem.instance.RemoveItem(itemToTransfer.itemData);
            }
        }

        // Rafraîchissement global des deux entités
        InventorySystem.instance.RefreshContent();
        RefreshContentChestInventory();
        RefreshContentPlayerInventory();
    }

    // --- LOGIQUE D'AJOUT DANS LE COFFRE ---
    private bool AddToChestArray(ItemData data, int amount)
    {
        ItemInInventory[] targetArray = (data.itemType == ItemType.Ressource) ? contentRessourcesChest : contentCraftChest;

        if (data.stackable)
        {
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i] != null && targetArray[i].itemData == data && targetArray[i].count < data.maxStack)
                {
                    int spaceLeft = data.maxStack - targetArray[i].count;
                    int amountToAdd = Mathf.Min(spaceLeft, amount);

                    targetArray[i].count += amountToAdd;
                    amount -= amountToAdd;

                    if (amount <= 0) return true;
                }
            }
        }

        while (amount > 0)
        {
            int emptyIndex = -1;
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex == -1) return false;

            int currentStackCount = data.stackable ? Mathf.Min(data.maxStack, amount) : 1;
            targetArray[emptyIndex] = new ItemInInventory { itemData = data, count = currentStackCount };
            amount -= currentStackCount;
        }

        return true;
    }

    #region Save System
    public ChestInventoryData GetSaveData()
    {
        ChestInventoryData data = new ChestInventoryData();
        data.ressourcesItems = new List<ItemInInventorySave>();
        data.craftItems = new List<ItemInInventorySave>();

        for (int i = 0; i < contentRessourcesChest.Length; i++)
        {
            if (contentRessourcesChest[i] == null || contentRessourcesChest[i].itemData == null) continue;

            data.ressourcesItems.Add(new ItemInInventorySave
            {
                itemID = contentRessourcesChest[i].itemData.itemID,
                count = contentRessourcesChest[i].count
            });
        }

        for (int i = 0; i < contentCraftChest.Length; i++)
        {
            if (contentCraftChest[i] == null || contentCraftChest[i].itemData == null) continue;

            data.craftItems.Add(new ItemInInventorySave
            {
                itemID = contentCraftChest[i].itemData.itemID,
                count = contentCraftChest[i].count
            });
        }
        return data;
    }

    public void LoadSaveData(ChestInventoryData data)
    {
        if (data == null) return;

        System.Array.Clear(contentRessourcesChest, 0, contentRessourcesChest.Length);
        System.Array.Clear(contentCraftChest, 0, contentCraftChest.Length);

        if (data.ressourcesItems != null)
        {
            for (int i = 0; i < Mathf.Min(data.ressourcesItems.Count, contentRessourcesChest.Length); i++)
            {
                ItemData itemData = ItemDataDatabase.Instance.GetItemByID(data.ressourcesItems[i].itemID);
                if (itemData == null) continue;

                contentRessourcesChest[i] = new ItemInInventory { itemData = itemData, count = data.ressourcesItems[i].count };
            }
        }

        if (data.craftItems != null)
        {
            for (int i = 0; i < Mathf.Min(data.craftItems.Count, contentCraftChest.Length); i++)
            {
                ItemData itemData = ItemDataDatabase.Instance.GetItemByID(data.craftItems[i].itemID);
                if (itemData == null) continue;

                contentCraftChest[i] = new ItemInInventory { itemData = itemData, count = data.craftItems[i].count };
            }
        }
    }
    #endregion
}
[System.Serializable]
public class ChestInventoryData
{
    // Séparation des données de sauvegarde pour reconstruire les listes fidèlement au chargement
    public List<ItemInInventorySave> ressourcesItems = new List<ItemInInventorySave>();
    public List<ItemInInventorySave> craftItems = new List<ItemInInventorySave>();
}