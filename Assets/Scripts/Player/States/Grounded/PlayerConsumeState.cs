using UnityEngine;

public class PlayerConsumeState : PlayerState
{
    private float _stateTimer;
    private float _animationDuration = 0.5f; // 🟢 Légèrement augmenté pour laisser le temps à l'animation de jouer
    private int _targetSlot = -1;
    private ItemData _consumableItem;

    public PlayerConsumeState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        _stateTimer = 0f;

        var palette = PaletteSystem.instance;
        if (palette == null || palette.slotManager == null)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            return;
        }

        // 1. Récupération de l'objet et du slot équipé
        if (palette.slotManager.objectSlots[0].isEquipped)
        {
            _targetSlot = 1;
            _consumableItem = palette.slotManager.objectSlots[0].slotItemData;
        }
        else if (palette.slotManager.objectSlots[1].isEquipped)
        {
            _targetSlot = 2;
            _consumableItem = palette.slotManager.objectSlots[1].slotItemData;
        }

        if (_consumableItem == null)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            return;
        }

        // 2. Application immédiate de la consommation
        ExecuteImmediateConsumption();
        player.Animator.SetTrigger("UnequipConsumable");
    }

    public override void Update()
    {
        base.Update();

        _stateTimer += Time.deltaTime;

        if (_stateTimer >= _animationDuration)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    private void ExecuteImmediateConsumption()
    {
        // 1. Application du soin
        if (player.Health != null)
        {
            player.Health.Heal(_consumableItem.healthEffect);
        }

        var palette = PaletteSystem.instance;
        if (palette == null || palette.equipmentManager == null) return;

        // 2. Retrait de l'objet de l'inventaire D'ABORD
        palette.equipmentManager.RemoveObject(_targetSlot);

        // 3. Vérification de la quantité RESTANTE dans l'inventaire
        ItemInInventory itemInInventory = FindItemInInventory(_consumableItem);

        // 🟢 Si l'item n'existe plus ou que son stock est tombé à 0 :
        if (itemInInventory == null || itemInInventory.count <= 0)
        {
            // Désactivation visuelle du modèle 3D en main
            EquipmentLibraryItem libraryItem = player.equipmentLibrary?.Get(_consumableItem);
            if (libraryItem != null && libraryItem.itemPrefab != null)
            {
                libraryItem.itemPrefab.SetActive(false);
            }

            player.Animator.SetBool("CarryingConsumable", false);

            // Libération de l'état "isEquipped" du slot de la palette
            int slotIndex = _targetSlot - 1;
            if (slotIndex >= 0 && slotIndex < palette.slotManager.objectSlots.Length)
            {
                palette.slotManager.objectSlots[slotIndex].isEquipped = false;
            }

            // 🟢 NETTOYAGE CRUCIAL : Réinitialisation des variables du PlayerController !
            // Sans ceci, le Player garde en mémoire qu'il tient toujours cet objet.
            if (player.PendingWeaponItem == _consumableItem)
            {
                player.PendingWeaponItem = null;
                player.PendingLibraryItem = null;
            }
        }

        // 4. Rafraîchissement de l'affichage UI
        palette.slotManager.UpdateImageSeleted();
    }

    private ItemInInventory FindItemInInventory(ItemData item)
    {
        var palette = PaletteSystem.instance;
        if (palette == null || palette.slotManager == null || palette.slotManager.objects == null)
            return null;

        foreach (var invItem in palette.slotManager.objects)
        {
            if (invItem != null && invItem.itemData == item)
            {
                return invItem;
            }
        }
        return null;
    }

    public override void Exit()
    {
        base.Exit();
        _consumableItem = null;
        _targetSlot = -1;
    }
}