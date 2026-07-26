using UnityEngine;

public class PaletteInputHandler : MonoBehaviour
{
    [SerializeField] private PaletteSlotManager slotManager;
    [SerializeField] private PaletteEquipmentManager equipmentManager;

    public void HandleInput(PlayerController player)
    {
        if (player == null || equipmentManager == null) return;

        if (!CanChangeWeapon(player)) return;

        if (player.Input.Weapon1Pressed)
        {
            equipmentManager.ToggleWeapon(0, player);
            player.Input.UseWeapon1Pressed();
        }
        else if (player.Input.Weapon2Pressed)
        {
            equipmentManager.ToggleWeapon(1, player);
            player.Input.UseWeapon2Pressed();
        }
        else if (player.Input.Object1Pressed)
        {
            equipmentManager.ToggleObject(0, player);
            player.Input.UseObject1Pressed();
        }
        else if (player.Input.Object2Pressed)
        {
            equipmentManager.ToggleObject(1, player);
            player.Input.UseObject2Pressed();
        }
    }

    private bool CanChangeWeapon(PlayerController player)
    {
        // Empêche le changement d'arme pendant Equip/Unequip et s'assure qu'on est au sol
        var currentState = player.StateMachine.CurrentState;

        if (currentState is PlayerEquipState || currentState is PlayerUnequipState)
            return false;

        // Si tu as un état d'attaque, d'esquive ou de dégât, ajoute-le ici ou utilise une propriété player.CanSwitchWeapon
        return currentState is PlayerGroundedState;
    }
}