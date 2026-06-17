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
    }

    public override void Update()
    {
        if (player.IsDead || player.interactSystem.isBusy)
        {
            if (player.Animator != null) player.Animator.speed = 1f; // Sécurité
            return;
        }

        // ─── NOUVEAUTÉ : APPLICATION DU SPEED MODIFIER AU SOL ───
        float currentSlow = (player.DmgReceiver != null) ? player.DmgReceiver.SpeedModifier : 1f;
        player.Animator.speed = currentSlow;
        // ────────────────────────────────────────────────────────

        // 1. PRIORITÉ : La Chute
        if (!player.Motor.IsGrounded())
        {
            player.StateMachine.ChangeState(PlayerStateType.Fall);
            return;
        }

        if (player.StateMachine.CurrentState is PlayerEquipState ||
                player.StateMachine.CurrentState is PlayerUnequipState)
        {
            return;
        }

        // 2. PRIORITÉ : Le Saut
        if (player.Input.JumpPressed)
        {
            if (Time.time < player.lastJumpTime + player.jumpCooldown)
            {
                return;
            }

            player.lastJumpTime = Time.time;
            player.Input.UseJumpInput();
            player.StateMachine.ChangeState(PlayerStateType.Jump);
            return;
        }

        if (player.Input.AttackPressed && CanEat())
        {
            Debug.Log("Consume Pressed");
            player.Input.UseAttackInput();
            player.StateMachine.ChangeState(PlayerStateType.Consume);
            return;
        }

        bool hasWeapon = false;
        if (player.PendingLibraryItem != null)
            if (player.PendingLibraryItem.itemPrefab != null)
                hasWeapon = player.PendingLibraryItem.itemPrefab.activeSelf;
        

        // 3. PRIORITÉ : L'Attaque ou l'Arc
        if (player.Input.AttackPressed && hasWeapon)
        {
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
        if (player.Input.AimHeld)
        {
            player.StateMachine.ChangeState(PlayerStateType.Aim);
            return;
        }

        // 5. PRIORITÉ : La Roulade
        if (player.Input.RollPressed && player.Stamina.CanSpend(player.rollCout))
        {
            player.Input.UseRollInput();
            player.StateMachine.ChangeState(PlayerStateType.Roll);
            return;
        }

        // 7. PRIORITÉ : Le LockOn
        if (player.Input.LockOnPressed)
        {
            Debug.Log("LockOn Pressed");
            player.Input.UseLockOnInput();
            player.LockOn.ToggleLock();
            return;
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (player.Animator != null)
            player.Animator.speed = 1f;
    }

    private void HandleAttackInput(bool isSpecialAttack = false)
    {
        ItemData activeWeapon = PaletteSystem.instance.slotManager.weaponSlots[0].isEquipped ?
                                PaletteSystem.instance.slotManager.weaponSlots[0].slotItemData :
                                PaletteSystem.instance.slotManager.weaponSlots[1].slotItemData;

        if (activeWeapon == null) return;

        if (activeWeapon.handWeaponType == HandWeapon.Bow)
        {
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

    private bool CanEat()
    {
        Debug.Log("Checking if player can consume...");
        var palette = PaletteSystem.instance;
        if (palette == null) return false;

        return palette.slotManager.objectSlots[0].isEquipped || palette.slotManager.objectSlots[1].isEquipped;
    }
}