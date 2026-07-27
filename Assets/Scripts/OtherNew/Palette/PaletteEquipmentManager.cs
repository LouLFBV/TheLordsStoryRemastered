using UnityEngine;

public class PaletteEquipmentManager : MonoBehaviour
{
    [SerializeField] private NewItemActionsSystem newItemActionsSystem;
    [SerializeField] private EquipmentLibrary equipmentLibrary;
    [SerializeField] private InteractSystem interactSystem;
    [SerializeField] private PaletteSlotManager slotManager;

    private void CleanInvalidEquippedStates()
    {
        //  Sécurité : Si un slot est marqué "équipé" mais n'a plus d'item, on force isEquipped à false
        if (slotManager.weaponSlots != null)
        {
            foreach (var slot in slotManager.weaponSlots)
            {
                if (slot != null && slot.slotItemData == null) slot.isEquipped = false;
            }
        }

        if (slotManager.objectSlots != null)
        {
            foreach (var slot in slotManager.objectSlots)
            {
                if (slot != null && slot.slotItemData == null) slot.isEquipped = false;
            }
        }
    }

    private void UseObject(int numberOfObject, PlayerController player)
    {
        slotManager.weaponSlots[0].isEquipped = false;
        slotManager.weaponSlots[1].isEquipped = false;
        player.Animator.SetBool("IsTwoHandedWeapon", false);
        player.Animator.SetBool("IsOneHandedWeapon", false);

        if (numberOfObject == 0)
        {
            EquipmentLibraryItem equipmentLibraryItem1 = equipmentLibrary.Get(slotManager.objectSlots[0].slotItemData);
            if (equipmentLibraryItem1 != null && equipmentLibraryItem1.itemPrefab != null)
                equipmentLibraryItem1.itemPrefab.SetActive(true);

            interactSystem.SetCurrentEquippedItem(equipmentLibraryItem1);

            if (slotManager.objects[1].itemData != null)
                DisableObject(slotManager.objectSlots[1].slotItemData);

            DisableWeapon(slotManager.weaponSlots[0].slotItemData);
            DisableWeapon(slotManager.weaponSlots[1].slotItemData);
        }
        else
        {
            EquipmentLibraryItem equipmentLibraryItem2 = equipmentLibrary.Get(slotManager.objectSlots[1].slotItemData);
            if (equipmentLibraryItem2 != null && equipmentLibraryItem2.itemPrefab != null)
                equipmentLibraryItem2.itemPrefab.SetActive(true);

            interactSystem.SetCurrentEquippedItem(equipmentLibraryItem2);

            if (slotManager.objects[0].itemData != null && slotManager.objectSlots[1].slotItemData != slotManager.objects[0].itemData)
                DisableObject(slotManager.objectSlots[0].slotItemData);

            DisableWeapon(slotManager.weaponSlots[0].slotItemData);
            DisableWeapon(slotManager.weaponSlots[1].slotItemData);
        }
    }

    private void UseWeapon(int slot, PlayerController player)
    {
        ItemData itemToEquip = (slot == 1) ? slotManager.weaponSlots[1].slotItemData : slotManager.weaponSlots[0].slotItemData;
        player.PendingWeaponItem = itemToEquip;

        // 1. On cache les consommables
        DisableObject(slotManager.objectSlots[0].slotItemData);
        DisableObject(slotManager.objectSlots[1].slotItemData);
        slotManager.objectSlots[0].isEquipped = slotManager.objectSlots[1].isEquipped = false;
        player.Animator.SetBool("CarryingConsumable", false);

        player.PrepareEquip(itemToEquip);
    }

    public void DesequipWeapon(int numberOfWeapon)
    {
        if (InventorySystem.instance.IsFullEquipment())
        {
            Debug.LogWarning("Cannot desequip item, inventory is full.");
            return;
        }

        ItemData currentItem = (numberOfWeapon == 1) ? slotManager.weaponSlots[0].slotItemData : slotManager.weaponSlots[1].slotItemData;

        if (slotManager.weaponSlots[numberOfWeapon - 1].isEquipped)
        {
            Debug.Log("Desequipping currently equipped weapon");
            DisableWeapon(currentItem);
        }

        if (numberOfWeapon == 1)
        {
            slotManager.weaponSlots[0].slotItemData = null;
            slotManager.weaponSlots[0].slotInEquipment.item = null;
            slotManager.weaponSlots[0].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[0].SlotImage.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[0].imageSelected.SetActive(false);
            if (slotManager.weaponSlots[0].isEquipped)
            {
                PlayerController.Instance.Animator.SetBool("BowEquipped", false);
                PlayerController.Instance.Animator.SetBool("IsTwoHandedWeapon", false);
                PlayerController.Instance.Animator.SetBool("IsOneHandedWeapon", false);
            }
            slotManager.weaponSlots[0].isEquipped = false;
        }
        else
        {
            slotManager.weaponSlots[1].slotItemData = null;
            slotManager.weaponSlots[1].slotInEquipment.item = null;
            slotManager.weaponSlots[1].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[1].SlotImage.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[1].imageSelected.SetActive(false);
            if (slotManager.weaponSlots[1].isEquipped)
            {
                PlayerController.Instance.Animator.SetBool("BowEquipped", false);
                PlayerController.Instance.Animator.SetBool("IsTwoHandedWeapon", false);
                PlayerController.Instance.Animator.SetBool("IsOneHandedWeapon", false);
            }
            slotManager.weaponSlots[1].isEquipped = false;
        }

        InventorySystem.instance.AddItem(currentItem);
        RemoveWeapon(numberOfWeapon);
        slotManager.RefreshAffichage();
        slotManager.UpdateImageSeleted();
        newItemActionsSystem.CloseActionPanel();
    }

    public void DesequipObject(int numberOfObject, PlayerController player)
    {
        if (InventorySystem.instance.IsFullEquipment())
        {
            Debug.LogWarning("Cannot desequip item, inventory is full.");
            return;
        }

        ItemData currentItem = null;
        if (numberOfObject == 1)
        {
            currentItem = slotManager.objectSlots[0].slotItemData;
            slotManager.objectSlots[0].SlotImage.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.objectSlots[0].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.objectSlots[0].isEquipped = false;
        }
        else
        {
            currentItem = slotManager.objectSlots[1].slotItemData;
            slotManager.objectSlots[1].SlotImage.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.objectSlots[1].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.objectSlots[1].isEquipped = false;
        }

        player.Animator.SetTrigger("UnequipConsumable");
        DisableObject(currentItem);

        if (currentItem)
        {
            InventorySystem.instance.AddItem(currentItem);
        }
        RemoveObject(numberOfObject);
        slotManager.RefreshAffichage();
        slotManager.UpdateImageSeleted();
    }

    public void ToggleWeapon(int slot, PlayerController player)
    {
        CleanInvalidEquippedStates();

        ItemData itemToEquip = slotManager.weapons[slot].itemData;
        if (itemToEquip == null) return;

        bool somethingIsEquipped = slotManager.weaponSlots[0].isEquipped ||
                                   slotManager.weaponSlots[1].isEquipped ||
                                   slotManager.objectSlots[0].isEquipped ||
                                   slotManager.objectSlots[1].isEquipped;

        if (!slotManager.weaponSlots[slot].isEquipped)
        {
            if (somethingIsEquipped)
            {
                // --- CAS DU SWAP ---
                player.ItemQueuedToEquip = itemToEquip;

                if (slotManager.weaponSlots[0].isEquipped) DesequipCurrentActiveWeapon(0, player);
                else if (slotManager.weaponSlots[1].isEquipped) DesequipCurrentActiveWeapon(1, player);
                else if (slotManager.objectSlots[0].isEquipped) ToggleObject(0, player);
                else if (slotManager.objectSlots[1].isEquipped) ToggleObject(1, player);

                slotManager.weaponSlots[slot].isEquipped = true;
            }
            else
            {
                // --- CAS CLASSIQUE (Main vide) ---
                EquipNewWeapon(slot, itemToEquip, player);
            }
        }
        else
        {
            DesequipCurrentActiveWeapon(slot, player);
        }
        slotManager.UpdateImageSeleted();
    }

    private void EquipNewWeapon(int slot, ItemData item, PlayerController player)
    {
        slotManager.weaponSlots[0].isEquipped = (slot == 0);
        slotManager.weaponSlots[1].isEquipped = (slot == 1);
        player.PendingWeaponType = item.handWeaponType;
        player.PrepareEquip(item);
        player.StateMachine.ChangeState(PlayerStateType.Equip);
    }

    public void ToggleObject(int slot, PlayerController player)
    {
        CleanInvalidEquippedStates();

        ItemData itemToEquip = slotManager.objects[slot].itemData;
        if (itemToEquip == null) return;

        bool isCurrentlyEquipped = slotManager.objectSlots[slot].isEquipped;

        bool somethingIsEquipped = slotManager.weaponSlots[0].isEquipped ||
                                   slotManager.weaponSlots[1].isEquipped ||
                                   slotManager.objectSlots[0].isEquipped ||
                                   slotManager.objectSlots[1].isEquipped;

        if (!isCurrentlyEquipped)
        {
            if (somethingIsEquipped)
            {
                // --- CAS DU SWAP ---
                player.ItemQueuedToEquip = itemToEquip;

                if (slotManager.weaponSlots[0].isEquipped) DesequipCurrentActiveWeapon(0, player);
                else if (slotManager.weaponSlots[1].isEquipped) DesequipCurrentActiveWeapon(1, player);
                else if (slotManager.objectSlots[0].isEquipped) ToggleObject(0, player);
                else if (slotManager.objectSlots[1].isEquipped) ToggleObject(1, player);

                slotManager.objectSlots[slot].isEquipped = true;
            }
            else
            {
                // --- CAS CLASSIQUE (Main vide) ---
                EquipNewObject(slot, itemToEquip, player);
            }
        }
        else
        {
            // On déséquipe l'objet actuel
            player.PrepareUnequip(itemToEquip);
            slotManager.objectSlots[slot].isEquipped = false;
            if (player.StateMachine.CurrentState.GetType() != typeof(PlayerUnequipState))
            {
                player.StateMachine.ChangeState(PlayerStateType.Unequip);
            }
        }

        slotManager.UpdateImageSeleted();
    }

    private void EquipNewObject(int slot, ItemData item, PlayerController player)
    {
        slotManager.objectSlots[0].isEquipped = (slot == 0);
        slotManager.objectSlots[1].isEquipped = (slot == 1);

        player.PendingWeaponType = item.handWeaponType;
        player.PrepareEquip(item);

        player.StateMachine.ChangeState(PlayerStateType.Equip);
    }

    public void RemoveObject(int numberOfObject)
    {
        int slotIndex = numberOfObject - 1;
        if (slotIndex < 0 || slotIndex >= slotManager.objects.Length) return;

        if (slotManager.objects[slotIndex].count > 1)
        {
            slotManager.objects[slotIndex].count--;
            slotManager.objectSlots[slotIndex].countText.text = slotManager.objects[slotIndex].count.ToString();
            slotManager.objectSlots[slotIndex].slotInEquipment.countTexte.text = slotManager.objects[slotIndex].count.ToString();
        }
        else
        {
            slotManager.objects[slotIndex].itemData = null;
            slotManager.objects[slotIndex].count = 0;
            slotManager.objectSlots[slotIndex].slotItemData = null;

            // CORRECTION ESSENTIELLE : Désactiver isEquipped quand le stock retombe à zéro !
            slotManager.objectSlots[slotIndex].isEquipped = false;

            slotManager.objectSlots[slotIndex].slotInEquipment.item = null;
            slotManager.objectSlots[slotIndex].slotInEquipment.countTexte.text = "";
            newItemActionsSystem.CloseActionPanel();
        }

        slotManager.RefreshAffichage();
    }

    public void RemoveWeapon(int numberOfWeapon)
    {
        int slotIndex = numberOfWeapon - 1;
        if (slotIndex < 0 || slotIndex >= slotManager.weaponSlots.Length) return;

        slotManager.weaponSlots[slotIndex].slotItemData = null;
        slotManager.weapons[slotIndex].itemData = null;
        slotManager.weapons[slotIndex].count = 0;
        slotManager.weaponSlots[slotIndex].slotInEquipment.item = null;
        slotManager.weaponSlots[slotIndex].isEquipped = false; //  Sécurité

        slotManager.RefreshAffichage();
    }

    public void DisableObject(ItemData item)
    {
        if (item == null) return;

        var lib = equipmentLibrary.Get(item);
        if (lib?.itemPrefab != null && lib.itemPrefab.activeSelf)
            lib.itemPrefab.SetActive(false);
    }

    private void DisableWeapon(ItemData item)
    {
        if (item == null) return;

        var lib = equipmentLibrary.Get(item);
        if (lib?.itemPrefab != null)
            lib.itemPrefab.SetActive(false);
    }

    private void DesequipCurrentActiveWeapon(int slot, PlayerController player)
    {
        ItemData item = slotManager.weaponSlots[slot].slotItemData;
        if (item == null) return;

        PaletteSlot slotData = slotManager.weaponSlots[slot];

        player.PrepareUnequip(item);
        slotData.isEquipped = false;
        player.StateMachine.ChangeState(PlayerStateType.Unequip);
    }
}