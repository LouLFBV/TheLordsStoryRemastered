using UnityEngine;

public class PlayerConsumeState : PlayerState
{
    private float _stateTimer;
    private float _animationDuration = 0.5f;
    private int _targetSlot = -1;
    private ItemData _consumableItem;
    private bool _isEmptyAfterConsume;

    public PlayerConsumeState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        _stateTimer = 0f;
        _isEmptyAfterConsume = false;

        var palette = PaletteSystem.instance;
        if (palette == null || palette.slotManager == null)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            return;
        }

        // 1. Récupération de l'objet et du slot équipé (Slot 0 -> 1, Slot 1 -> 2)
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

        // 2. Application de la consommation et vérification du stock restant
        ExecuteImmediateConsumption();
    }

    public override void Update()
    {
        base.Update();

        _stateTimer += Time.deltaTime;

        if (_stateTimer >= _animationDuration)
        {
            if (_isEmptyAfterConsume)
            {
                // 🟢 PLUS DE STOCK : On passe par PrepareUnequip pour transitionner vers UnequipState.
                // UnequipState jouera le déséquipement et l'AE_UnequipWeapon coupera le modèle 3D.
                player.PrepareUnequip(_consumableItem);
            }
            else
            {
                // 🟢 IL RESTE DES POTIONS : L'item reste équipé en main, on repasse simplement en Idle.
                player.StateMachine.ChangeState(PlayerStateType.Idle);
            }
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

        // 2. Retrait d'une potion de l'inventaire
        palette.equipmentManager.RemoveObject(_targetSlot);

        // 3. Vérification de la quantité restante
        ItemInInventory itemInInventory = FindItemInInventory(_consumableItem);

        // Si le stock est tombé à 0 ou que l'item n'est plus présent :
        if (itemInInventory == null || itemInInventory.count <= 0)
        {
            _isEmptyAfterConsume = true;

            // Libération du slot dans la palette
            int slotIndex = _targetSlot - 1;
            if (slotIndex >= 0 && slotIndex < palette.slotManager.objectSlots.Length)
            {
                palette.slotManager.objectSlots[slotIndex].isEquipped = false;
            }

            // Nettoyage des références d'attente sur le Player
            if (player.PendingWeaponItem == _consumableItem)
            {
                player.PendingWeaponItem = null;
            }
            player.Animator.SetTrigger("UnequipConsumable");
        }
        else
        {
            _isEmptyAfterConsume = false;
        }

        // 4. Rafraîchissement de l'UI
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