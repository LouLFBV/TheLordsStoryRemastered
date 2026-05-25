using UnityEngine;

public class PlayerUnequipState : PlayerGroundedState
{
    public PlayerUnequipState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        player.Rigidbody.linearVelocity = Vector3.zero;

        if (player.PendingWeaponItem != null && player.PendingWeaponItem.itemType == ItemType.Consumable)
        {
            player.Animator.SetTrigger("UnequipConsumable");
            return;
        }

        PlayUnequipAnimation(player.PendingUnequipType);
    }


    private void PlayUnequipAnimation(HandWeapon type)
    {
        switch (type)
        {
            case HandWeapon.Bow:
                player.Animator.SetTrigger("DesequipBow");
                player.Animator.SetBool("BowEquipped", false);
                break;
            case HandWeapon.TwoHanded:
                player.Animator.SetTrigger("DesequipLongSword");
                player.Animator.SetBool("IsTwoHandedWeapon", false);
                break;
            case HandWeapon.OneHanded:
                player.Animator.SetTrigger("DesequipSword");
                player.Animator.SetBool("IsOneHandedWeapon", false);
                break;
        }
    }
    public void HandleWeaponRemoval()
    {
        if (player.PendingLibraryItem != null)
        {
            // 1. Désactivation visuelle habituelle
            player.PendingLibraryItem.itemPrefab.SetActive(false);
            foreach (var element in player.PendingLibraryItem.elementsToDisable)
            {
                element.SetActive(true);
            }

            // 2. CHECK : Est-ce qu'on a un item en attente (SWAP) ?
            if (player.ItemQueuedToEquip != null)
            {
                // On récupère l'item et on vide la file
                ItemData nextItem = player.ItemQueuedToEquip;
                player.ItemQueuedToEquip = null;

                // On prépare le prochain équipement
                player.PendingWeaponType = nextItem.handWeaponType;
                player.PrepareEquip(nextItem);

                // On enchaîne directement sur l'état Equip
                player.StateMachine.ChangeState(PlayerStateType.Equip);
            }
            else
            {
                // Sinon, retour classique à la locomotion
                player.StateMachine.ChangeState(player.Input.MoveInput != Vector2.zero
                    ? PlayerStateType.Move : PlayerStateType.Idle);
            }
        }
        else
        {
            Debug.LogWarning("PendingLibraryItem is null in HandleWeaponRemoval");
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (player.PendingLibraryItem.itemPrefab.activeSelf)
        {
            player.PendingLibraryItem.itemPrefab.SetActive(false);
        }
    }
}