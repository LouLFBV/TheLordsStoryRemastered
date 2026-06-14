using UnityEngine;

public class PaletteSaveSystem : MonoBehaviour
{
    [SerializeField] private EquipmentLibrary equipmentLibrary;
    [SerializeField] private InteractSystem interactSystem;
    [SerializeField] private PaletteSlotManager slotManager;
    [SerializeField] private PaletteEquipmentManager equipmentManager;

    public PaletteSaveData GetSaveData()
    {
        // CORRECTION : On utilise "CreateSlotSave" pour injecter le statut 'isEquipped' provenant des slots réels
        return new PaletteSaveData
        {
            weapon1 = CreateSlotSave(slotManager.weapons[0]?.itemData, slotManager.weapons[0], slotManager.weaponSlots[0].isEquipped),
            weapon2 = CreateSlotSave(slotManager.weapons[1]?.itemData, slotManager.weapons[1], slotManager.weaponSlots[1].isEquipped),
            object1 = CreateSlotSave(slotManager.objects[0]?.itemData, slotManager.objects[0], slotManager.objectSlots[0].isEquipped),
            object2 = CreateSlotSave(slotManager.objects[1]?.itemData, slotManager.objects[1], slotManager.objectSlots[1].isEquipped),
        };
    }

    private PaletteSlotSave CreateSlotSave(ItemData item, ItemInInventory slot, bool equipped)
    {
        if (item == null || slot == null)
            return null;

        return new PaletteSlotSave
        {
            itemID = item.itemID,
            count = slot.count,
            isEquipped = equipped
        };
    }

    public void LoadSaveData(PaletteSaveData data)
    {
        slotManager.ClearPalette();

        LoadWeaponSlot(1, data.weapon1);
        LoadWeaponSlot(2, data.weapon2);

        LoadObjectSlot(1, data.object1);
        LoadObjectSlot(2, data.object2);

        slotManager.RefreshAffichage();
        slotManager.UpdateImageSeleted();

        // On applique l'équipement une fois que toute la palette est prête
        ApplyEquippedStateAfterLoad();
    }

    private void LoadWeaponSlot(int slot, PaletteSlotSave save)
    {
        if (save == null) return;

        ItemData item = InventorySystem.instance.itemDatabase.GetItemByID(save.itemID);
        if (item == null) return;

        int index = slot - 1;

        slotManager.weapons[index] = new ItemInInventory
        {
            itemData = item,
            count = save.count
        };

        PaletteSlot slotData = slotManager.weaponSlots[index];
        slotData.slotItemData = item;
        slotData.isEquipped = save.isEquipped; // Récupère enfin le vrai état !
    }

    private void LoadObjectSlot(int slot, PaletteSlotSave save)
    {
        if (save == null) return;

        ItemData item = InventorySystem.instance.itemDatabase.GetItemByID(save.itemID);
        if (item == null) return;

        int index = slot - 1;

        slotManager.objects[index] = new ItemInInventory
        {
            itemData = item,
            count = save.count
        };

        PaletteSlot slotData = slotManager.objectSlots[index];
        slotData.slotItemData = item;
        slotData.isEquipped = save.isEquipped;
        slotManager.UpdateSlotUI(index, save.count);
    }

    private void ApplyEquippedStateAfterLoad()
    {
        // Priorité aux armes
        if (slotManager.weaponSlots[0].isEquipped && slotManager.weaponSlots[0].slotItemData != null)
        {
            EquipFromSave(slotManager.weaponSlots[0].slotItemData);
        }
        else if (slotManager.weaponSlots[1].isEquipped && slotManager.weaponSlots[1].slotItemData != null)
        {
            EquipFromSave(slotManager.weaponSlots[1].slotItemData);
        }
        // Sinon objets
        else if (slotManager.objectSlots[0].isEquipped && slotManager.objectSlots[0].slotItemData != null)
        {
            EquipObjectFromSave(slotManager.objectSlots[0].slotItemData);
        }
        else if (slotManager.objectSlots[1].isEquipped && slotManager.objectSlots[1].slotItemData != null)
        {
            EquipObjectFromSave(slotManager.objectSlots[1].slotItemData);
        }
    }

    private void EquipFromSave(ItemData item)
    {
        // Désactiver les objets rapides pour éviter les conflits visuels dans les mains
        if (slotManager.objectSlots[0].slotItemData != null) equipmentManager.DisableObject(slotManager.objectSlots[0].slotItemData);
        if (slotManager.objectSlots[1].slotItemData != null) equipmentManager.DisableObject(slotManager.objectSlots[1].slotItemData);

        EquipmentLibraryItem libItem = equipmentLibrary.Get(item);

        if (libItem != null && libItem.itemPrefab != null)
        {
            // Activer instantanément le visuel de l'arme au spawn
            libItem.itemPrefab.SetActive(true);

            if (PlayerController.Instance != null)
            {
                PlayerController player = PlayerController.Instance;

                player.PendingLibraryItem = libItem;
                player.PendingWeaponItem = item;
                player.PendingWeaponType = item.handWeaponType;

                player.Animator.SetBool("BowEquipped", item.handWeaponType == HandWeapon.Bow);
                player.Animator.SetBool("IsTwoHandedWeapon", item.handWeaponType == HandWeapon.TwoHanded);
                player.Animator.SetBool("IsOneHandedWeapon", item.handWeaponType == HandWeapon.OneHanded);

                if (libItem.itemPrefab.TryGetComponent<WeaponDamageDetector>(out var newDetector))
                {
                    player.Combat.UpdateWeaponDetector(newDetector);
                }
                else if (item.itemType != ItemType.Consumable)
                {
                    Debug.LogWarning($"[SAVE] Le prefab {libItem.itemPrefab.name} n'a pas de WeaponDamageDetector au chargement !");
                }

            }
        }

        Debug.Log($"[SAVE] Equipped weapon from save successfully with animations and hitboxes: {item.name}");
    }

    private void EquipObjectFromSave(ItemData item)
    {
        EquipmentLibraryItem libItem = equipmentLibrary.Get(item);

        if (libItem != null && libItem.itemPrefab != null)
        {
            libItem.itemPrefab.SetActive(true);
            if (interactSystem != null) interactSystem.SetCurrentEquippedItem(libItem);

            // Si tu as une animation d'attente quand le joueur tient une potion :
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.Animator.SetBool("CarryingConsumable", true);
            }
        }

        Debug.Log($"[SAVE] Equipped object from save: {item.name}");
    }
}