using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    #region Champs
    public static EquipmentSystem instance;

    [Header("Other Scripts References")]
    [SerializeField] private NewItemActionsSystem itemActionsSystem;
    [SerializeField] private PlayerController player;
    [SerializeField] private PaletteSystem palette;

    [Header("Equipment Panel References")]
    [SerializeField] private EquipmentLibrary equipmentLibrary;
    public Slot headSlot, chestSlot, handsSlot, legsSlot, feetSlot, arrowSlot;
    public Slot[] equipmentSlots;
    [SerializeField] private GameObject[] quiverArrowsInEquipment = new GameObject[10];

    [HideInInspector]
    public ItemInInventory arrowItemInInventory;

    public AudioSource audioSource;
    public AudioClip equipSound;

    private bool isLoading = false;
    #endregion

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

        equipmentSlots = new Slot[] { headSlot, chestSlot, handsSlot, legsSlot, feetSlot, arrowSlot };

        if (audioSource != null)
        {
            audioSource.ignoreListenerPause = true;
        }
    }

    private void RefreshPlayerArmor()
    {
        if (player != null && player.Armor != null)
        {
            List<ItemData> equippedArmors = new List<ItemData>();

            if (headSlot.item != null) equippedArmors.Add(headSlot.item);
            if (chestSlot.item != null) equippedArmors.Add(chestSlot.item);
            if (handsSlot.item != null) equippedArmors.Add(handsSlot.item);
            if (legsSlot.item != null) equippedArmors.Add(legsSlot.item);
            if (feetSlot.item != null) equippedArmors.Add(feetSlot.item);

            player.Armor.RefreshArmorStats(equippedArmors);
        }
    }

    public bool IsEquipped(ItemData item)
    {
        return headSlot.item == item ||
               chestSlot.item == item ||
               handsSlot.item == item ||
               legsSlot.item == item ||
               feetSlot.item == item ||
               arrowItemInInventory.itemData == item;
    }

    private void DisablePreviousEquipedEquipment(ItemData itemToDisable)
    {
        if (itemToDisable == null) return;

        EquipmentLibraryItem equipmentLibraryItem = equipmentLibrary.Get(itemToDisable);

        if (equipmentLibraryItem != null)
        {
            ActiveItemVisuel(equipmentLibraryItem, false);
        }

        if (itemToDisable.equipmentType == EquipmentType.Arrow)
        {
            if (arrowItemInInventory.count > 0)
            {
                int count = arrowItemInInventory.count;
                arrowItemInInventory.count = 0;
                arrowItemInInventory.itemData = null;
                for (int i = 0; i < count; i++)
                {
                    InventorySystem.instance.AddItem(itemToDisable);
                }
            }
        }
        else
        {
            InventorySystem.instance.AddItem(itemToDisable);
        }
    }

    // Ajout du paramètre "bool putBackInInventory = true"
    public void DesequipEquipment(EquipmentType equipmentType, bool putBackInInventory = true)
    {
        // On ne bloque l'action pour inventaire plein QUE si on essaie de le ranger
        if (putBackInInventory && InventorySystem.instance.IsFullEquipment())
        {
            Debug.LogWarning("Cannot desequip item, inventory is full.");
            return;
        }

        ItemData currentItem = null;
        int arrowsToReturn = 0;

        switch (equipmentType)
        {
            case EquipmentType.Head:
                currentItem = headSlot.item;
                headSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                headSlot.item = null;
                headSlot.itemTypeVisual.gameObject.SetActive(true);
                break;
            case EquipmentType.Chest:
                currentItem = chestSlot.item;
                chestSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                chestSlot.item = null;
                chestSlot.itemTypeVisual.gameObject.SetActive(true);
                break;
            case EquipmentType.Hands:
                currentItem = handsSlot.item;
                handsSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                handsSlot.item = null;
                handsSlot.itemTypeVisual.gameObject.SetActive(true);
                break;
            case EquipmentType.Legs:
                currentItem = legsSlot.item;
                legsSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                legsSlot.item = null;
                legsSlot.itemTypeVisual.gameObject.SetActive(true);
                break;
            case EquipmentType.Feet:
                currentItem = feetSlot.item;
                feetSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                feetSlot.item = null;
                feetSlot.itemTypeVisual.gameObject.SetActive(true);
                break;
            case EquipmentType.Arrow:
                currentItem = arrowItemInInventory.itemData;
                arrowsToReturn = arrowItemInInventory.count; // 2. On sauvegarde le compte actuel
                arrowItemInInventory.count = 0;              // 3. On peut maintenant le mettre à 0 sans danger
                arrowSlot.item = null;
                arrowSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                arrowSlot.itemTypeVisual.gameObject.SetActive(true);
                arrowItemInInventory.itemData = null;

                if (palette != null && palette.slotManager != null && palette.slotManager.arrowSlot != null)
                {
                    palette.slotManager.arrowSlot.slotItemData = null;
                    palette.slotManager.arrowSlot.SlotImage.sprite = InventorySystem.instance.emptySlotVisual;

                    if (palette.slotManager.arrowSlot.slotInEquipment != null)
                    {
                        palette.slotManager.arrowSlot.slotInEquipment.item = null;
                        palette.slotManager.arrowSlot.slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                    }
                    if (palette.slotManager.arrowSlot.countText != null)
                    {
                        palette.slotManager.arrowSlot.countText.text = "";
                    }
                }
                break;
        }

        EquipmentLibraryItem equipmentLibraryItem = equipmentLibrary.Get(currentItem);

        if (equipmentLibraryItem != null)
        {
            ActiveItemVisuel(equipmentLibraryItem, false);
        }

        if (currentItem)
        {
            if (currentItem.handWeaponType == HandWeapon.TwoHanded)
            {
                player.Animator.SetBool("IsTwoHandedWeapon", false);
            }

            RefreshPlayerArmor();

            if (currentItem.equipmentType == EquipmentType.Arrow)
            {
                // 4. On utilise notre sauvegarde "arrowsToReturn" pour la boucle
                if (putBackInInventory && arrowsToReturn > 0)
                {
                    for (int i = 0; i < arrowsToReturn; i++)
                    {
                        InventorySystem.instance.AddItem(currentItem);
                    }
                }
                UpdateArrowsText();
            }
            else
            {
                if (putBackInInventory)
                {
                    InventorySystem.instance.AddItem(currentItem);
                }
            }
        }
    }

    public void UpdateArrowsText()
    {
        arrowSlot.countTexte.gameObject.SetActive(arrowItemInInventory.itemData != null);
        arrowSlot.countTexte.text = arrowItemInInventory.count.ToString();
    }

    public void EquipAction(ItemData equipment = null)
    {
        ItemData itemToEquip = equipment ? equipment : itemActionsSystem.itemCurrentlySelected;
        print("Equip item : " + itemToEquip.name);

        EquipmentLibraryItem equipmentLibraryItem = equipmentLibrary.Get(itemToEquip);

        if (equipmentLibraryItem != null)
        {
            switch (itemToEquip.equipmentType)
            {
                case EquipmentType.Head:
                    DisablePreviousEquipedEquipment(headSlot.item);
                    headSlot.itemVisual.sprite = itemToEquip.visual;
                    headSlot.item = itemToEquip;
                    headSlot.itemTypeVisual.gameObject.SetActive(false);
                    ActiveItemVisuel(equipmentLibraryItem);
                    break;
                case EquipmentType.Chest:
                    DisablePreviousEquipedEquipment(chestSlot.item);
                    chestSlot.itemVisual.sprite = itemToEquip.visual;
                    chestSlot.item = itemToEquip;
                    chestSlot.itemTypeVisual.gameObject.SetActive(false);
                    ActiveItemVisuel(equipmentLibraryItem);
                    break;
                case EquipmentType.Hands:
                    DisablePreviousEquipedEquipment(handsSlot.item);
                    handsSlot.itemVisual.sprite = itemToEquip.visual;
                    handsSlot.item = itemToEquip;
                    handsSlot.itemTypeVisual.gameObject.SetActive(false);
                    ActiveItemVisuel(equipmentLibraryItem);
                    break;
                case EquipmentType.Legs:
                    DisablePreviousEquipedEquipment(legsSlot.item);
                    legsSlot.itemVisual.sprite = itemToEquip.visual;
                    legsSlot.item = itemToEquip;
                    legsSlot.itemTypeVisual.gameObject.SetActive(false);
                    ActiveItemVisuel(equipmentLibraryItem);
                    break;
                case EquipmentType.Feet:
                    DisablePreviousEquipedEquipment(feetSlot.item);
                    feetSlot.itemVisual.sprite = itemToEquip.visual;
                    feetSlot.item = itemToEquip;
                    feetSlot.itemTypeVisual.gameObject.SetActive(false);
                    ActiveItemVisuel(equipmentLibraryItem);
                    break;
                case EquipmentType.Weapon:
                    palette.slotManager.AddWeapon(itemToEquip);
                    break;
                case EquipmentType.Arrow:
                    DisablePreviousEquipedEquipment(arrowItemInInventory.itemData);
                    arrowSlot.itemVisual.sprite = itemToEquip.visual;
                    arrowSlot.item = itemToEquip; // 🟢 FIX : Assigner le ItemData au Slot
                    arrowSlot.itemTypeVisual.gameObject.SetActive(false); // 🟢 FIX : Masquer l'icône de fond !
                    arrowItemInInventory.itemData = itemToEquip;

                    var inventoryEntry = InventorySystem.instance.GetContent().Find(x => x.itemData == itemToEquip);
                    if (inventoryEntry != null)
                    {
                        arrowItemInInventory.count = inventoryEntry.count;
                    }

                    PaletteSystem.instance.slotManager.AddArrow(itemToEquip);
                    PaletteSystem.instance.slotManager.UpdateCountArrow(arrowItemInInventory.count);
                    for (int i = 0; i < arrowItemInInventory.count; i++)
                    {
                        InventorySystem.instance.RemoveItem(itemToEquip);
                    }
                    UpdateArrowsText();
                    ActiveItemVisuel(equipmentLibraryItem);
                    if (BowBehaviour.instance != null)
                    {
                        BowBehaviour.instance.UpdateQuiverVisual(arrowItemInInventory.count);
                    }
                    break;
            }

            RefreshPlayerArmor();

            if (itemToEquip.itemType == ItemType.Consumable)
            {
                palette.slotManager.AddObject(itemToEquip);
            }

            if (!isLoading && itemToEquip.equipmentType != EquipmentType.Arrow)
                InventorySystem.instance.RemoveItem(itemToEquip);

            if (!isLoading)
            {
                Debug.Log("<color=red> EquipSound</color>");
                audioSource.PlayOneShot(equipSound);
            }

            if (InventorySystem.instance.GetItemCount(itemToEquip) <= 0)
                itemActionsSystem.CloseActionPanel();
        }
        else
        {
            Debug.LogWarning("Item not found in equipment library: " + itemToEquip.name);
        }
    }

    public void UpdateQuiverVisual(int currentArrowCount)
    {
        for (int i = 0; i < quiverArrowsInEquipment.Length; i++)
        {
            quiverArrowsInEquipment[i].SetActive(i < currentArrowCount);
        }
    }

    #region Save/Load
    public EquipmentSaveData GetSaveData()
    {
        return new EquipmentSaveData
        {
            headID = headSlot.item ? headSlot.item.itemID : null,
            headLevel = headSlot.item ? headSlot.item.levelAmelioration : 0,

            chestID = chestSlot.item ? chestSlot.item.itemID : null,
            chestLevel = chestSlot.item ? chestSlot.item.levelAmelioration : 0,

            handsID = handsSlot.item ? handsSlot.item.itemID : null,
            handsLevel = handsSlot.item ? handsSlot.item.levelAmelioration : 0,

            legsID = legsSlot.item ? legsSlot.item.itemID : null,
            legsLevel = legsSlot.item ? legsSlot.item.levelAmelioration : 0,

            feetID = feetSlot.item ? feetSlot.item.itemID : null,
            feetLevel = feetSlot.item ? feetSlot.item.levelAmelioration : 0,

            arrowID = arrowItemInInventory.itemData ? arrowItemInInventory.itemData.itemID : null,
            arrowLevel = arrowItemInInventory.itemData ? arrowItemInInventory.itemData.levelAmelioration : 0,
            arrowCount = arrowItemInInventory.count
        };
    }

    public void LoadSaveData(EquipmentSaveData data)
    {
        isLoading = true;

        headSlot.item = null;
        chestSlot.item = null;
        handsSlot.item = null;
        legsSlot.item = null;
        feetSlot.item = null;
        arrowSlot.item = null; // 🟢 FIX : Réinitialiser le slot de flèche
        arrowItemInInventory.itemData = null;
        arrowItemInInventory.count = 0;

        headSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        chestSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        handsSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        legsSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        feetSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        arrowSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;

        // 🟢 FIX : Réactiver par défaut les icônes de fond de tous les slots au démarrage de la réinitialisation
        headSlot.itemTypeVisual.gameObject.SetActive(true);
        chestSlot.itemTypeVisual.gameObject.SetActive(true);
        handsSlot.itemTypeVisual.gameObject.SetActive(true);
        legsSlot.itemTypeVisual.gameObject.SetActive(true);
        feetSlot.itemTypeVisual.gameObject.SetActive(true);
        arrowSlot.itemTypeVisual.gameObject.SetActive(true);

        if (data == null)
        {
            isLoading = false;
            RefreshPlayerArmor();
            return;
        }

        EquipByID(data.headID, data.headLevel);
        EquipByID(data.chestID, data.chestLevel);
        EquipByID(data.handsID, data.handsLevel);
        EquipByID(data.legsID, data.legsLevel);
        EquipByID(data.feetID, data.feetLevel);

        if (!string.IsNullOrEmpty(data.arrowID))
        {
            ItemData arrowBase = ItemDataDatabase.Instance.GetItemByID(data.arrowID);
            if (arrowBase != null)
            {
                ItemData finalArrow = arrowBase;

                arrowSlot.item = finalArrow; // 🟢 FIX 1 : Assigner l'item au Slot
                arrowSlot.itemVisual.sprite = finalArrow.visual;
                arrowSlot.itemTypeVisual.gameObject.SetActive(false); // 🟢 FIX 2 : Désactiver l'icône de fond

                arrowItemInInventory.itemData = finalArrow;
                arrowItemInInventory.count = data.arrowCount;

                // 🟢 FIX 3 : Synchroniser avec la palette rapide
                if (PaletteSystem.instance != null && PaletteSystem.instance.slotManager != null)
                {
                    PaletteSystem.instance.slotManager.AddArrow(finalArrow);
                    PaletteSystem.instance.slotManager.UpdateCountArrow(data.arrowCount);
                }

                // 🟢 FIX 4 : Activer le visuel 3D du carquois/flèche si configuré dans la bibliothèque
                EquipmentLibraryItem equipmentLibraryItem = equipmentLibrary.Get(finalArrow);
                if (equipmentLibraryItem != null)
                {
                    ActiveItemVisuel(equipmentLibraryItem);
                }

                UpdateArrowsText();
                if (BowBehaviour.instance != null)
                {
                    BowBehaviour.instance.UpdateQuiverVisual(data.arrowCount);
                }
            }
        }

        isLoading = false;
        RefreshPlayerArmor();
    }

    private void EquipByID(string id, int savedLevel)
    {
        if (string.IsNullOrEmpty(id)) return;

        ItemData baseItem = ItemDataDatabase.Instance.GetItemByID(id);
        if (baseItem != null)
        {
            ItemData finalItem = baseItem;

            if (!baseItem.stackable)
            {
                finalItem = baseItem.CreateInstance();
                finalItem.RestoreLevel(savedLevel);
            }

            EquipAction(finalItem);
        }
    }
    #endregion

    private void ActiveItemVisuel(EquipmentLibraryItem equipmentLibraryItem, bool actived = true)
    {
        if (equipmentLibraryItem.elementsToDisable != null)
        {
            foreach (GameObject element in equipmentLibraryItem.elementsToDisable)
            {
                if (element != null)
                    element.SetActive(!actived);
            }
        }
        if (equipmentLibraryItem.bodyPartsToEnable != null)
        {
            foreach (GameObject bodyPart in equipmentLibraryItem.bodyPartsToEnable)
            {
                if (bodyPart != null)
                    bodyPart.SetActive(actived);
            }
        }
        if (equipmentLibraryItem.itemPrefab != null)
        {
            equipmentLibraryItem.itemPrefab.SetActive(actived);
        }

        ActiveItemVisuelInEquipment(equipmentLibraryItem, actived);
    }

    private void ActiveItemVisuelInEquipment(EquipmentLibraryItem equipmentLibraryItem, bool actived)
    {
        if (equipmentLibraryItem.elementsToDisableEquipment != null)
        {
            foreach (GameObject element in equipmentLibraryItem.elementsToDisableEquipment)
            {
                if (element != null)
                    element.SetActive(!actived);
            }
        }

        if (equipmentLibraryItem.bodyPartsToEnableEquipment != null)
        {
            foreach (GameObject bodyPart in equipmentLibraryItem.bodyPartsToEnableEquipment)
            {
                if (bodyPart != null)
                    bodyPart.SetActive(actived);
            }
        }
        if (equipmentLibraryItem.itemPrefabEquipment != null)
        {
            equipmentLibraryItem.itemPrefabEquipment.SetActive(actived);
        }
    }
}

[System.Serializable]
public class EquipmentSaveData
{
    public string headID; public int headLevel;
    public string chestID; public int chestLevel;
    public string handsID; public int handsLevel;
    public string legsID; public int legsLevel;
    public string feetID; public int feetLevel;

    public string arrowID; public int arrowLevel;
    public int arrowCount;
}
