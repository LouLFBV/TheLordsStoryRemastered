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
                // ACTUALISATION TOOLTIP : L'item est toujours présent dans le coffre, on met à jour l'affichage de sa quantité
                Tooltip.Instance.SetText(itemToTransfer.itemData, itemToTransfer.count);
            }
            else
            {
                Tooltip.Instance.Hide();
                sourceArray[arrayIndex] = null;
            }

            // 2. On l'ajoute ensuite au système du joueur (qui peut déclencher ses propres events)
            InventorySystem.instance.AddItem(itemToTransfer.itemData);
        }
        else
        {
            // --- TRIPLE ACTION : JOUEUR -> COFFRE ---
            bool success = AddToChestArray(itemToTransfer.itemData, 1);

            if (success)
            {
                InventorySystem.instance.RemoveItem(itemToTransfer.itemData);

                int remainingPlayerCount = InventorySystem.instance.GetItemCount(itemToTransfer.itemData);
                if (remainingPlayerCount <= 0)
                {
                    Tooltip.Instance.Hide();
                }
                else
                {
                    // ACTUALISATION TOOLTIP : L'item est toujours présent chez le joueur, on met à jour l'affichage de sa quantité restante
                    Tooltip.Instance.SetText(itemToTransfer.itemData, remainingPlayerCount);
                }
            }
        }

        // 3. RAFRAÎCHISSEMENT VISUEL RIGIDE (On s'assure que le joueur n'écrase pas le coffre)
        // On met à jour les scripts du joueur d'abord
        InventorySystem.instance.RefreshContent();

        // On synchronise et reconstruit l'UI du joueur dans le coffre
        RefreshContentPlayerInventory();

        // On applique l'UI du coffre EN DERNIER pour qu'elle ait le dernier mot sur l'affichage
        RefreshContentChestInventory();
    }

    private bool AddToChestArray(ItemData data, int amount)
    {
        ItemInInventory[] targetArray = (data.itemType == ItemType.Ressource) ? contentRessourcesChest : contentCraftChest;
        string arrayName = (data.itemType == ItemType.Ressource) ? "Ressources" : "Craft";

        Debug.Log($"[AddToChestArray] Début de l'ajout. Item: {data.itemName} | Quantité à ajouter: {amount} | Tableau ciblé: {arrayName} (Taille: {targetArray.Length})");

        // --- ÉTAPE 1 : TENTATIVE D'EMPILAGE (STACK) ---
        if (data.stackable)
        {
            Debug.Log($"[AddToChestArray] L'item est stackable (MaxStack: {data.maxStack}). Recherche d'un stack existant...");
            for (int i = 0; i < targetArray.Length; i++)
            {
                if (targetArray[i] != null && targetArray[i].itemData == data && targetArray[i].count < data.maxStack)
                {
                    int spaceLeft = data.maxStack - targetArray[i].count;
                    int amountToAdd = Mathf.Min(spaceLeft, amount);

                    Debug.Log($"[AddToChestArray] Stack trouvé à l'index {i}. Quantité actuelle: {targetArray[i].count} | Espace libre: {spaceLeft} | Ajout de: {amountToAdd}");

                    targetArray[i].count += amountToAdd;
                    amount -= amountToAdd;

                    if (amount <= 0)
                    {
                        Debug.Log("[AddToChestArray] Tout l'item a été empilé avec succès. Retour TRUE.");
                        return true;
                    }
                }
            }
        }
        else
        {
            Debug.Log("[AddToChestArray] L'item n'est pas stackable.");
        }

        // --- ÉTAPE 2 : RECHERCHE DE PLACES VIDES ---
        if (amount > 0)
        {
            Debug.Log($"[AddToChestArray] Il reste {amount} unités à placer. Recherche d'emplacements vides...");

            while (amount > 0)
            {
                int emptyIndex = -1;
                for (int i = 0; i < targetArray.Length; i++)
                {
                    // SÉCURITÉ : On vérifie si la case complète est null 
                    // OU si l'objet ItemInInventory à l'intérieur n'a pas de Data
                    if (targetArray[i] == null || targetArray[i].itemData == null)
                    {
                        emptyIndex = i;
                        break;
                    }
                }

                if (emptyIndex == -1)
                {
                    Debug.LogWarning($"[AddToChestArray] ÉCHEC : Plus aucune place vide trouvée dans le tableau {arrayName}. Retour FALSE.");
                    return false;
                }

                int currentStackCount = data.stackable ? Mathf.Min(data.maxStack, amount) : 1;

                Debug.Log($"[AddToChestArray] Emplacement vide trouvé à l'index {emptyIndex}. Création / Remplacement par un nouvel ItemInInventory (Quantité: {currentStackCount}).");

                // On instancie proprement pour écraser l'ancien état (qu'il ait été null ou avec un itemData null)
                targetArray[emptyIndex] = new ItemInInventory { itemData = data, count = currentStackCount };
                amount -= currentStackCount;
            }
        }

        Debug.Log("[AddToChestArray] Tous les nouveaux emplacements ont été créés avec succès. Retour TRUE.");
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