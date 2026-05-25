using UnityEngine;

public class PlayerConsumeState : PlayerState
{
    private float _stateTimer;
    private float _animationDuration = 0.1f; // Durée de ton animation en secondes (à ajuster)
    private int _targetSlot;
    private ItemData _consumableItem;

    public PlayerConsumeState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        _stateTimer = 0f;

        var palette = PaletteSystem.instance;

        // 1. On récupère l'item du bon slot d'objet
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

        // 2. EFFET IMMÉDIAT : On applique la logique de soin tout de suite
        ExecuteImmediateConsumption();
        player.Animator.SetTrigger("UnequipConsumable");
    }

    public override void Update()
    {
        base.Update();

        // On incrémente le timer pour bloquer le joueur le temps de l'anim
        _stateTimer += Time.deltaTime;

        if (_stateTimer >= _animationDuration)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    private void ExecuteImmediateConsumption()
    {
        // Application du soin
        player.Health.Heal(_consumableItem.healthEffect);

        var palette = PaletteSystem.instance;

        // Recherche de l'objet dans l'inventaire (Boucle sans LINQ)
        ItemInInventory[] objectsList = palette.slotManager.objects;
        ItemInInventory itemInInventory = null;

        for (int i = 0; i < objectsList.Length; i++)
        {
            if (objectsList[i] != null && objectsList[i].itemData == _consumableItem)
            {
                itemInInventory = objectsList[i];
                break;
            }
        }

        // Nettoyage visuel si c'était le dernier exemplaire
        if (itemInInventory != null && itemInInventory.count == 1)
        {
            EquipmentLibraryItem libraryItem = player.equipmentLibrary.Get(_consumableItem);
            if (libraryItem != null && libraryItem.itemPrefab != null)
            {
                libraryItem.itemPrefab.SetActive(false);
            }

            player.Animator.SetBool("CarryingConsumable", false);

            if (_targetSlot == 1) palette.slotManager.objectSlots[0].isEquipped = false;
            else palette.slotManager.objectSlots[1].isEquipped = false;
        }

        // Retrait de l'objet et rafraîchissement UI
        palette.equipmentManager.RemoveObject(_targetSlot);
        palette.slotManager.UpdateImageSeleted();
    }

    public override void Exit()
    {
        base.Exit();
        _consumableItem = null;
    }
}