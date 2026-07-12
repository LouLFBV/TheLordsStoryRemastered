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

    public void DesequipEquipment(EquipmentType equipmentType)
    {
        if (InventorySystem.instance.IsFullEquipment())
        {
            Debug.LogWarning("Cannot desequip item, inventory is full.");
            return;
        }

        ItemData currentItem = null;

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
                arrowSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                arrowSlot.itemTypeVisual.gameObject.SetActive(true);
                arrowItemInInventory.itemData = null;
                palette.slotManager.arrowSlot.slotItemData = null;
                palette.slotManager.arrowSlot.SlotImage.sprite = InventorySystem.instance.emptySlotVisual;
                palette.slotManager.arrowSlot.slotInEquipment.item = null;
                palette.slotManager.arrowSlot.slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
                palette.slotManager.arrowSlot.countText.text = "";
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
                if (arrowItemInInventory.count > 0)
                {
                    for (int i = 0; i < arrowItemInInventory.count; i++)
                    {
                        InventorySystem.instance.AddItem(currentItem);
                    }
                }
                UpdateArrowsText();
            }
            else
            {
                InventorySystem.instance.AddItem(currentItem);
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
                    arrowItemInInventory.itemData = itemToEquip;
                    arrowItemInInventory.count = InventorySystem.instance.GetContent().Find(x => x.itemData == itemToEquip).count;
                    PaletteSystem.instance.slotManager.AddArrow(itemToEquip);
                    PaletteSystem.instance.slotManager.UpdateCountArrow(arrowItemInInventory.count);
                    for (int i = 0; i < arrowItemInInventory.count; i++)
                    {
                        InventorySystem.instance.RemoveItem(itemToEquip);
                    }
                    UpdateArrowsText();
                    ActiveItemVisuel(equipmentLibraryItem);
                    BowBehaviour.instance.UpdateQuiverVisual(arrowItemInInventory.count);
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
                audioSource.PlayOneShot(equipSound);

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
        arrowItemInInventory.itemData = null;
        arrowItemInInventory.count = 0;

        headSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        chestSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        handsSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        legsSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        feetSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
        arrowSlot.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;

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
            ItemData finalArrow = arrowBase;

            // Si tes flèches peuvent être améliorées (et ne sont pas stackables), décommente ceci :
            // if (!arrowBase.stackable) {
            //     finalArrow = arrowBase.CreateInstance();
            //     finalArrow.RestoreLevel(data.arrowLevel);
            // }

            arrowItemInInventory.itemData = finalArrow;
            arrowItemInInventory.count = data.arrowCount;

            arrowSlot.itemVisual.sprite = finalArrow.visual;
            UpdateArrowsText();
            BowBehaviour.instance.UpdateQuiverVisual(data.arrowCount);
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

            // On clone si c'est un équipement unique
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
        foreach (GameObject element in equipmentLibraryItem.elementsToDisable)
        {
            element.SetActive(!actived);
        }
        equipmentLibraryItem.itemPrefab.SetActive(actived);

        ActiveItemVisuelInEquipment(equipmentLibraryItem, actived);
    }

    private void ActiveItemVisuelInEquipment(EquipmentLibraryItem equipmentLibraryItem, bool actived)
    {
        foreach (GameObject element in equipmentLibraryItem.elementsToDisableEquipment)
        {
            element.SetActive(!actived);
        }
        equipmentLibraryItem.itemPrefabEquipment.SetActive(actived);
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
