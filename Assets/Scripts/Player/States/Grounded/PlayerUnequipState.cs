using UnityEngine;

public class PlayerUnequipState : PlayerGroundedState
{
    private Vector2 cachedInput;
    private float runSpeed = 4f;
    private float sprintSpeed = 7f;
    private bool isSprinting;
    private bool changedFOV;

    public PlayerUnequipState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        player.Animator.applyRootMotion = true;

        if (player.PendingWeaponItem != null && player.PendingWeaponItem.itemType == ItemType.Consumable)
        {
            player.Animator.SetTrigger("UnequipConsumable");
            return;
        }

        PlayUnequipAnimation(player.PendingUnequipType);
    }

    public override void Update()
    {
        base.Update();

        Vector2 input = player.Input.MoveInput;
        cachedInput = input;

        // --- GESTION DU DÉPLACEMENT & ROTATION ---
        player.Motor.RotateTowardsInput(input);

        // Sprint
        isSprinting = CanSprint(input);
        float animSpeed = isSprinting ? sprintSpeed : runSpeed;

        // FOV Sprint
        if (isSprinting && !changedFOV)
        {
            ThirdPersonCameraController.Instance.SetFOV(ThirdPersonCameraController.Instance.SprintFOV);
            changedFOV = true;
        }
        else if (!isSprinting && changedFOV)
        {
            ThirdPersonCameraController.Instance.ResetFOV();
            changedFOV = false;
        }

        // Mise à jour de l'Animator
        player.Animator.SetFloat(AnimatorHashes.hHash, input.x, 0.1f, Time.deltaTime);
        player.Animator.SetFloat(AnimatorHashes.vHash, input.y, 0.1f, Time.deltaTime);
        player.Animator.SetFloat(AnimatorHashes.speedHash, input.magnitude * (animSpeed / sprintSpeed), 0.1f, Time.deltaTime);

        // Stamina
        if (isSprinting)
        {
            player.Stamina.Spend(player.Stamina.consommationRate * Time.deltaTime);
        }
        else if (player.Input.SprintHeld && !player.Stamina.HasStamina())
        {
            player.Stamina.RequestEmptyFeedback();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        player.Motor.RotateTowardsInput(cachedInput);
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
            player.PendingLibraryItem.itemPrefab.SetActive(false);
            foreach (var element in player.PendingLibraryItem.elementsToDisable)
            {
                element.SetActive(true);
            }

            if (player.ItemQueuedToEquip != null)
            {
                ItemData nextItem = player.ItemQueuedToEquip;
                player.ItemQueuedToEquip = null;

                player.PendingWeaponType = nextItem.handWeaponType;
                player.PrepareEquip(nextItem);

                player.StateMachine.ChangeState(PlayerStateType.Equip);
            }
            else
            {
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

        ThirdPersonCameraController.Instance.ResetFOV();
        changedFOV = false;

        if (player.PendingLibraryItem.itemPrefab.activeSelf)
        {
            player.PendingLibraryItem.itemPrefab.SetActive(false);
        }
    }

    private bool CanSprint(Vector2 input)
    {
        return input.magnitude > 0.1f
               && player.Input.SprintHeld
               && player.Stamina.HasStamina();
    }
}