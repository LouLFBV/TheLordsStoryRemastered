using UnityEngine;
public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        player.Animator.applyRootMotion = true; // Le sol reprend le contrôle via l'anim
        player.Animator.SetBool("Grounded", true);
        player.Animator.SetBool("IsFalling", false); 
        player.Motor.SetFriction(true);
        // On ne nettoie les layers que si on vient d'un état "non-grounded" 
        // ou d'une attaque, pour éviter les saccades entre Idle et Move.
    }

    public override void Update()
    {
        if (player.IsDead) return;
        base.Update();

        // 1. PRIORITÉ : La Chute
        if (!player.Motor.IsGrounded())
        {
            player.StateMachine.ChangeState(PlayerStateType.Fall);
            return;
        }

        // 2. PRIORITÉ : Le Saut
        if (player.Input.JumpPressed)
        {
            // On vérifie si assez de temps s'est écoulé depuis le dernier saut
            if (Time.time < player.lastJumpTime + player.jumpCooldown)
            {
                return; // Trop tôt, on ignore l'input
            }

            // On empêche le saut pendant les transitions d'équipement
            if (player.StateMachine.CurrentState is PlayerEquipState ||
                player.StateMachine.CurrentState is PlayerUnequipState)
            {
                return;
            }

            // Si on arrive ici, le saut est autorisé
            player.lastJumpTime = Time.time; // On enregistre le moment du saut
            player.Input.UseJumpInput();
            player.StateMachine.ChangeState(PlayerStateType.Jump);
            return;
        }

        bool hasWeapon = false;
        if (player.PendingLibraryItem != null)
            if (player.PendingLibraryItem.itemPrefab != null)
                hasWeapon = player.PendingLibraryItem.itemPrefab.activeSelf;
        // 3. PRIORITÉ : L'Attaque ou l'Arc
        if (player.Input.AttackPressed && hasWeapon/*&& player.Stamina.HasStamina()*/)
        {
            if (player.StateMachine.CurrentState is PlayerEquipState ||
                player.StateMachine.CurrentState is PlayerUnequipState)
            {
                return;
            }
            HandleAttackInput();
            return;
        }

        if (player.Input.AttackSpecialPressed && hasWeapon)
        {
            if (player.StateMachine.CurrentState is PlayerEquipState ||
                player.StateMachine.CurrentState is PlayerUnequipState)
            {
                return;
            }
            HandleAttackInput(true);
            return;
        }

        // 4. PRIORITÉ : La Visée (AimState)
        // Si on n'attaque pas, mais qu'on maintient le bouton de visée
        if (player.Input.AimHeld)
        {
            player.StateMachine.ChangeState(PlayerStateType.Aim);
            return;
        }

        // 5. PRIORITÉ : La Roulade
        if (player.Input.RollPressed && player.Stamina.HasStamina())
        {
            player.Input.UseRollInput();
            player.StateMachine.ChangeState(PlayerStateType.Roll);
            return;
        }

        // 6. PRIORITÉ : Le Crouch
        //HandleCrouchInput();

        // 7. PRIORITÉ : Le LockOn
        if (player.Input.LockOnPressed)
        {
            Debug.Log("LockOn Pressed");
            player.Input.UseLockOnInput();
            player.LockOn.ToggleLock();
            return;
        }
    }

    private void HandleAttackInput(bool isSpecialAttack = false)
    {
        // On récupère l'arme active via ton PaletteSystem
        ItemData activeWeapon = PaletteSystem.instance.slotManager.weaponSlots[0].isEquipped ?
                                PaletteSystem.instance.slotManager.weaponSlots[0].slotItemData :
                                PaletteSystem.instance.slotManager.weaponSlots[1].slotItemData;

        if (activeWeapon == null) return;
        // --- DISTINCTION ARC / MÊLÉE ---
        if (activeWeapon.handWeaponType == HandWeapon.Bow)
        {
            // On vérifie les munitions via ton BowBehaviour
            if (player.Bow.VerifIfCanShoot())
            {
                player.StateMachine.ChangeState(PlayerStateType.BowCharge);
            }
        }
        else if (isSpecialAttack)
        {
            Debug.Log("Special Attack Pressed");
            player.usingSpecialAttack = true;
            player.Input.UseAttackSpecialInput();
            player.StateMachine.ChangeState(PlayerStateType.Attack);
        }
        else
        {
            Debug.Log("Attack Pressed");
            player.Input.UseAttackInput();
            player.StateMachine.ChangeState(PlayerStateType.Attack);
        }
    }
    protected virtual void HandleCrouchInput()
    {
        if (player.Input.CrouchPressed)
        {
            player.Input.UseCrouchInput();
            player.StateMachine.ChangeState(PlayerStateType.Crouch);
        }
    }
}