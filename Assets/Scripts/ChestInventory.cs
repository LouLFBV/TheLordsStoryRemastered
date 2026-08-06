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


    private SlotChest _currentSlotChest;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnEnable()
    {
        FetchAndSyncPlayerInventory();

        RefreshContentChestInventory();
        RefreshContentPlayerInventory();
    }

    private void Update()
    {
        if (_currentSlotChest != null && _currentSlotChest.item != null && PlayerController.Instance.Input.EquipActionPressed)
        {
            Debug.Log("Slot clicked: " + _currentSlotChest.item.itemName + " | In Chest: " + _currentSlotChest.isInChest + " | Is Resource: " + _currentSlotChest.isResource);
            HandleSlotClick(_currentSlotChest.arrayIndex, _currentSlotChest.isInChest, _currentSlotChest.isResource);

            if (_currentSlotChest.item == null)
            {
                _currentSlotChest = null;
            }
            PlayerController.Instance.Input.UseEquipActionInput();
        }
    }

    public void SetCurrentSlot(SlotChest slot)
    {
        _currentSlotChest = slot;
    }

    // --- ASPIRATION ET CONVERSION DES LISTES JOUEUR EN TABLEAUX FIXES ---
    public void FetchAndSyncPlayerInventory()
    {
        if (InventorySystem.instance == null) return;

        ItemInInventory[] playerRessources = InventorySystem.instance.GetPlayerRessourcesList();
        ItemInInventory[] playerCraft = InventorySystem.instance.GetPlayerCraftList();

        System.Array.Clear(contentRessourcePlayer, 0, contentRessourcePlayer.Length);
        System.Array.Clear(contentCraftPlayer, 0, contentCraftPlayer.Length);

        int ressourcesToCopy = Mathf.Min(playerRessources.Length, contentRessourcePlayer.Length);
        for (int i = 0; i < ressourcesToCopy; i++)
        {
            contentRessourcePlayer[i] = playerRessources[i];
        }

        int craftToCopy = Mathf.Min(playerCraft.Length, contentCraftPlayer.Length);
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

            currentSlot.button?.onClick.RemoveAllListeners();

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
                currentSlot.count = itemInInv.count;
                currentSlot.itemVisual.sprite = itemInInv.itemData.visual;

                currentSlot.isInChest = isChest;
                currentSlot.isResource = isResource;
                currentSlot.arrayIndex = i;

                if (currentSlot.item.stackable)
                {
                    currentSlot.countTexte.text = "x" + itemInInv.count.ToString();
                    currentSlot.countTexte.enabled = true;
                }
                else
                {
                    currentSlot.countTexte.enabled = false;
                }

                currentSlot.SetSlotState(true);

                int index = i;
                currentSlot.button?.onClick.AddListener(() => HandleSlotClick(index, isChest, isResource));
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
            bool isFull = itemToTransfer.itemData.itemType == ItemType.Ressource
                ? InventorySystem.instance.IsFullRessources()
                : InventorySystem.instance.IsFullCraft();

            if (isFull)
            {
                Debug.LogWarning("L'inventaire du joueur est plein !");
                return;
            }

            // 1. On modifie D'ABORD les données locales du coffre
            if (itemToTransfer.count > 1)
            {
                itemToTransfer.count--;
                Tooltip.Instance.SetText(itemToTransfer.itemData, itemToTransfer.count);
            }
            else
            {
                Tooltip.Instance.Hide();
                sourceArray[arrayIndex] = null;
            }

            InventorySystem.instance.AddItem(itemToTransfer.itemData);
        }
        else
        {
            // --- TRIPLE ACTION : JOUEUR -> COFFRE ---
            bool success = AddToChestArray(itemToTransfer.itemData, 1, out int modifiedChestSlotIndex);

            if (success)
            {
                Debug.Log($"Item envoyé dans le coffre au slot n°{modifiedChestSlotIndex}");

                // 🟢 CORRECTION 1 : On cible le slot précis au lieu du premier item trouvé !
                InventorySystem.instance.RemoveItemFromSlot(itemToTransfer.itemData.itemType, arrayIndex, 1);

                // Note : RemoveItemFromSlot modifie automatiquement itemToTransfer.count car ce sont les mêmes références mémoires.
                if (itemToTransfer.itemData == null || itemToTransfer.count <= 0)
                {
                    Tooltip.Instance.Hide();
                }
                else
                {
                    Tooltip.Instance.SetText(itemToTransfer.itemData, itemToTransfer.count);
                }
            }
        }

        // 3. RAFRAÎCHISSEMENT VISUEL RIGIDE (On s'assure que le joueur n'écrase pas le coffre)
        InventorySystem.instance.RefreshContent();
        RefreshContentPlayerInventory();
        RefreshContentChestInventory();
    }

    /// <summary>
    /// Ajoute un item dans le tableau du coffre.
    /// </summary>
    /// <param name="data">Données de l'item à ajouter</param>
    /// <param name="amount">Quantité à ajouter</param>
    /// <param name="modifiedSlotIndex">Index du slot qui a été modifié (-1 si échec)</param>
    /// <returns>True si l'ajout a réussi, False si le coffre est plein</returns>
    private bool AddToChestArray(ItemData data, int amount, out int modifiedSlotIndex)
    {
        modifiedSlotIndex = -1;
        ItemInInventory[] targetArray = (data.itemType == ItemType.Ressource) ? contentRessourcesChest : contentCraftChest;
        string arrayName = (data.itemType == ItemType.Ressource) ? "Ressources" : "Craft";

        Debug.Log($"[AddToChestArray] Début de l'ajout. Item: {data.itemName} | Quantité: {amount} | Tableau: {arrayName}");

        // --- ÉTAPE 1 : TENTATIVE D'EMPILAGE (STACK) ---
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
                    modifiedSlotIndex = i; // Enregistre l'index du slot empilé

                    Debug.Log($"[AddToChestArray] Empilé dans le slot {modifiedSlotIndex}. Nouvelle quantité: {targetArray[i].count}");

                    if (amount <= 0)
                    {
                        return true;
                    }
                }
            }
        }

        // --- ÉTAPE 2 : RECHERCHE DE PLACES VIDES ---
        if (amount > 0)
        {
            while (amount > 0)
            {
                int emptyIndex = -1;
                for (int i = 0; i < targetArray.Length; i++)
                {
                    if (targetArray[i] == null || targetArray[i].itemData == null)
                    {
                        emptyIndex = i;
                        break;
                    }
                }

                if (emptyIndex == -1)
                {
                    Debug.LogWarning($"[AddToChestArray] ÉCHEC : Plus de place dans le tableau {arrayName}.");
                    return false;
                }

                int currentStackCount = data.stackable ? Mathf.Min(data.maxStack, amount) : 1;

                targetArray[emptyIndex] = new ItemInInventory { itemData = data, count = currentStackCount };
                amount -= currentStackCount;
                modifiedSlotIndex = emptyIndex; // Enregistre l'index du nouveau slot rempli

                Debug.Log($"[AddToChestArray] Placé dans le nouveau slot {modifiedSlotIndex} (Quantité: {currentStackCount}).");
            }
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
                count = contentRessourcesChest[i].count,
                slotIndex = i
            });
        }

        for (int i = 0; i < contentCraftChest.Length; i++)
        {
            if (contentCraftChest[i] == null || contentCraftChest[i].itemData == null) continue;

            data.craftItems.Add(new ItemInInventorySave
            {
                itemID = contentCraftChest[i].itemData.itemID,
                count = contentCraftChest[i].count,
                slotIndex = i
            });
        }
        return data;
    }

    public void LoadSaveData(ChestInventoryData data)
    {
        if (data == null) return;

        System.Array.Clear(contentRessourcesChest, 0, contentRessourcesChest.Length);
        System.Array.Clear(contentCraftChest, 0, contentCraftChest.Length);

        foreach (var savedItem in data.ressourcesItems) // Utilise un foreach ici
        {
            ItemData itemData = ItemDataDatabase.Instance.GetItemByID(savedItem.itemID);
            if (itemData != null && savedItem.slotIndex < contentRessourcesChest.Length)
            {
                contentRessourcesChest[savedItem.slotIndex] = new ItemInInventory { itemData = itemData, count = savedItem.count };
            }
        }

        foreach (var savedItem in data.craftItems) // Utilise un foreach ici
        {
            ItemData itemData = ItemDataDatabase.Instance.GetItemByID(savedItem.itemID);
            if (itemData != null && savedItem.slotIndex < contentCraftChest.Length)
            {
                contentCraftChest[savedItem.slotIndex] = new ItemInInventory { itemData = itemData, count = savedItem.count };
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