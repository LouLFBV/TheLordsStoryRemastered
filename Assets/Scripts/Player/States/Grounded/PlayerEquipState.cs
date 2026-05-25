using UnityEngine;
public class PlayerEquipState : PlayerGroundedState
{

    public PlayerEquipState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        player.Rigidbody.linearVelocity = Vector3.zero;

        if (player.PendingWeaponItem.itemType == ItemType.Consumable)
        {
            player.Animator.SetTrigger("EquipConsumable");
            return;
        }
        PlayEquipAnimation(player.PendingWeaponType);
    }

    private void PlayEquipAnimation(HandWeapon type)
    {
        switch (type)
        {
            case HandWeapon.Bow:
                player.Animator.SetTrigger("EquipBow");
                player.Animator.SetBool("BowEquipped", true);
                break;
            case HandWeapon.TwoHanded:
                player.Animator.SetTrigger("EquipLongSword");
                player.Animator.SetBool("IsTwoHandedWeapon", true);
                break;
            case HandWeapon.OneHanded:
                player.Animator.SetTrigger("EquipSword");
                player.Animator.SetBool("IsOneHandedWeapon", true);
                break;
        }
    }

    public void HandleWeaponSwitch()
    {
        if (player.PendingLibraryItem != null)
        {
            // 1. Activer le nouveau prefab
            GameObject weaponObj = player.PendingLibraryItem.itemPrefab;
            weaponObj.SetActive(true);

            // 2. EXTRACTION ET MISE À JOUR DU DETECTOR
            // On récupère le detector sur le nouveau prefab
            WeaponDamageDetector newDetector = weaponObj.GetComponent<WeaponDamageDetector>();

            if (newDetector != null)
            {
                // On informe le CombatSystem qu'il doit maintenant piloter cette hitbox
                player.Combat.UpdateWeaponDetector(newDetector);
            }
            else if (player.PendingWeaponItem.itemType != ItemType.Consumable)
            {
                Debug.LogWarning($"Le prefab {weaponObj.name} n'a pas de WeaponDamageDetector!");
            }

            // 3. Désactiver les éléments visuels inutiles
            foreach (var element in player.PendingLibraryItem.elementsToDisable)
            {
                element.SetActive(false);
            }

            // 4. Transition
            player.StateMachine.ChangeState(player.Input.MoveInput != Vector2.zero
                ? PlayerStateType.Move : PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (!player.PendingLibraryItem.itemPrefab.activeSelf)
        {
            player.PendingLibraryItem.itemPrefab.SetActive(true);
        }
    }
}