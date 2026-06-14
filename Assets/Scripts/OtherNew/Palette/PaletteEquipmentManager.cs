using UnityEngine;

public class PaletteEquipmentManager : MonoBehaviour
{
    [SerializeField] private NewItemActionsSystem newItemActionsSystem;
    [SerializeField] private EquipmentLibrary equipmentLibrary;
    [SerializeField] private InteractSystem interactSystem;
    [SerializeField] private PaletteSlotManager slotManager;


    private void UseObject(int numberOfObject, PlayerController player)
    {
        slotManager.weaponSlots[0].isEquipped = false;
        slotManager.weaponSlots[1].isEquipped = false;
        player.Animator.SetBool("IsTwoHandedWeapon", false);
        player.Animator.SetBool("IsOneHandedWeapon", false);
        if (numberOfObject == 0)
        {
            EquipmentLibraryItem equipmentLibraryItem1 = equipmentLibrary.Get(slotManager.objectSlots[0].slotItemData);
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
            equipmentLibraryItem2.itemPrefab.SetActive(true);
            interactSystem.SetCurrentEquippedItem(equipmentLibraryItem2);


            if (slotManager.objects[0].itemData != null && slotManager.objectSlots[1].slotItemData != slotManager.objects[0].itemData)
                DisableObject(slotManager.objectSlots[0].slotItemData );

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


        if (slotManager.weaponSlots[numberOfWeapon-1].isEquipped)
        {
            Debug.Log("Desequipping currently equipped weapon in slot 1");
            DisableWeapon(currentItem);
        }

        // animations + model disable

        // remettre le slot visuellement vide
        if (numberOfWeapon == 1)
        {
            slotManager.weaponSlots[0].slotItemData = null;
            slotManager.weaponSlots[0].slotInEquipment.item = null;
            slotManager.weaponSlots[0].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[0].SlotImage.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[0].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
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
            slotManager.weaponSlots[1].slotInEquipment.itemVisual.sprite = InventorySystem.instance.emptySlotVisual;
            slotManager.weaponSlots[1].imageSelected.SetActive(false); 
            if (slotManager.weaponSlots[1].isEquipped)
            {
                PlayerController.Instance.Animator.SetBool("BowEquipped", false);
                PlayerController.Instance.Animator.SetBool("IsTwoHandedWeapon", false);
                PlayerController.Instance.Animator.SetBool("IsOneHandedWeapon", false);
            }
            slotManager.weaponSlots[1].isEquipped = false;
        }

        // remettre dans l'inventaire
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
        ItemData itemToEquip = slotManager.weapons[slot].itemData;
        if (itemToEquip == null) return;

        // 1. Est-ce qu'on a déjà quelque chose en main (Arme ou Objet) ?
        bool somethingIsEquipped = slotManager.weaponSlots[0].isEquipped ||
                                   slotManager.weaponSlots[1].isEquipped ||
                                   slotManager.objectSlots[0].isEquipped ||
                                   slotManager.objectSlots[1].isEquipped;

        if (!slotManager.weaponSlots[slot].isEquipped)
        {
            if (somethingIsEquipped)
            {
                // --- CAS DU SWAP ---
                player.ItemQueuedToEquip = itemToEquip; // On met l'épée en file d'attente

                // On identifie ce qu'il faut ranger pour lancer la bonne anim
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
    // Méthode helper pour éviter la répétition
    private void EquipNewWeapon(int slot, ItemData item, PlayerController player)
    {
        slotManager.weaponSlots[slot].isEquipped = true;
        slotManager.weaponSlots[0].isEquipped = (slot == 0);
        slotManager.weaponSlots[1].isEquipped = (slot == 1);
        player.PendingWeaponType = item.handWeaponType;
        player.PrepareEquip(item);
        player.StateMachine.ChangeState(PlayerStateType.Equip);
    }

    public void ToggleObject(int slot, PlayerController player)
    {
        ItemData itemToEquip = slotManager.objects[slot].itemData;
        if (itemToEquip == null) return;

        bool isCurrentlyEquipped = slotManager.objectSlots[slot].isEquipped;

        // 1. Est-ce qu'on a déjà quelque chose en main ?
        bool somethingIsEquipped = slotManager.weaponSlots[0].isEquipped ||
                                   slotManager.weaponSlots[1].isEquipped ||
                                   slotManager.objectSlots[0].isEquipped ||
                                   slotManager.objectSlots[1].isEquipped;

        if (!isCurrentlyEquipped)
        {
            if (somethingIsEquipped)
            {
                // --- CAS DU SWAP ---
                player.ItemQueuedToEquip = itemToEquip; // On met l'objet en attente

                // On identifie ce qu'il faut ranger
                if (slotManager.weaponSlots[0].isEquipped) DesequipCurrentActiveWeapon(0, player);
                else if (slotManager.weaponSlots[1].isEquipped) DesequipCurrentActiveWeapon(1, player);
                else if (slotManager.objectSlots[0].isEquipped) ToggleObject(0, player); // Récursif pour déséquiper l'autre slot objet
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
        }

        slotManager.UpdateImageSeleted();
    }

    // Méthode helper pour l'équipement d'objet
    private void EquipNewObject(int slot, ItemData item, PlayerController player)
    {
        slotManager.objectSlots[slot].isEquipped = true;
        slotManager.objectSlots[0].isEquipped = (slot == 0);
        slotManager.objectSlots[1].isEquipped = (slot == 1);

        player.PendingWeaponType = item.handWeaponType;
        player.PrepareEquip(item);

        // On passe à l'état Equip
        player.StateMachine.ChangeState(PlayerStateType.Equip);

        // On applique la logique visuelle spécifique aux objets
       // UseObject(slot, player);
    }

    public void RemoveObject(int numberOfObject)
    {
        if (numberOfObject == 1)
        {

            if (slotManager.objects[0].count > 1)
            {
                slotManager.objects[0].count--;
                slotManager.objectSlots[0].countText.text = slotManager.objects[0].count.ToString();
                slotManager.objectSlots[0].slotInEquipment.countTexte.text = slotManager.objects[0].count.ToString();
            }
            else
            {
                slotManager.objects[0].itemData = null;
                slotManager.objects[0].count = 0;
                slotManager.objectSlots[0].slotItemData = null;

                slotManager.objectSlots[0].slotInEquipment.item = null;
                slotManager.objectSlots[0].slotInEquipment.countTexte.text = "";
                newItemActionsSystem.CloseActionPanel();
            }
        }
        else
        {
            if (slotManager.objects[1].count > 1)
            {
                slotManager.objects[1].count--;
                slotManager.objectSlots[1].countText.text = slotManager.objects[1].count.ToString();
                slotManager.objectSlots[1].slotInEquipment.countTexte.text = slotManager.objects[1].count.ToString();
            }
            else
            {
                slotManager.objects[1].itemData = null;
                slotManager.objects[1].count = 0;
                slotManager.objectSlots[1].slotItemData = null;
                slotManager.objectSlots[1].slotInEquipment.item = null;
                slotManager.objectSlots[1].slotInEquipment.countTexte.text = "";
                newItemActionsSystem.CloseActionPanel();
            }
        }
        slotManager.RefreshAffichage();
    }

    public void RemoveWeapon(int numberOfWeapon)
    {
        if (numberOfWeapon == 1)
        {
            slotManager.weaponSlots[0].slotItemData = null;
            slotManager.weapons[0].itemData = null;
            slotManager.weapons[0].count = 0;
            slotManager.weaponSlots[0].slotInEquipment.item = null;

        }
        else
        {
            slotManager.weaponSlots[1].slotItemData = null;
            slotManager.weapons[1].itemData = null;
            slotManager.weapons[1].count = 0;
            slotManager.weaponSlots[1].slotInEquipment.item = null;
        }
        slotManager.RefreshAffichage();
    }

    public void DisableObject(ItemData item)
    {
        if (item == null) return;

        var lib = equipmentLibrary.Get(item);
        if (lib?.itemPrefab.activeSelf == true)
            lib.itemPrefab.SetActive(false);
    }

    private void DisableWeapon(ItemData item)
    {
        if (item == null) return;

        var lib = equipmentLibrary.Get(item);
        lib?.itemPrefab.SetActive(false);
    }

    private void DesequipCurrentActiveWeapon(int slot, PlayerController player)
    {
        ItemData item = (slot == 1) ? slotManager.weaponSlots[1].slotItemData : slotManager.weaponSlots[0].slotItemData;
        if (item == null) return;

        PaletteSlot slotData = slotManager.weaponSlots[slot];

        PlayerController.Instance.PrepareUnequip(item);
        slotData.isEquipped = false;
    }
}