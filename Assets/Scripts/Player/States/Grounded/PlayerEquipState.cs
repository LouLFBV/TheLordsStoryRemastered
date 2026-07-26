using UnityEngine;

public class PlayerEquipState : PlayerGroundedState
{
    private Vector2 cachedInput;
    private float runSpeed = 4f;
    private float sprintSpeed = 7f;
    private bool isSprinting;
    private bool changedFOV;
    private bool hasSwitchedWeapon; // 🟢 Sécurité pour suivre si l'event d'animation a été appelé

    public PlayerEquipState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        hasSwitchedWeapon = false;
        player.Animator.applyRootMotion = true;

        // 🟢 Sécurité Null Check sur le PendingItem
        if (player.PendingWeaponItem == null)
        {
            Debug.LogError("[PlayerEquipState] PendingWeaponItem est NULL à l'entrée de l'état !");
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            return;
        }

        if (player.PendingWeaponItem.itemType == ItemType.Consumable)
        {
            player.Animator.SetTrigger("EquipConsumable");
            return;
        }

        PlayEquipAnimation(player.PendingWeaponType);
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

        // FOV Sprint (Sécurisé avec ?.)
        if (ThirdPersonCameraController.Instance != null)
        {
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

    /// <summary>
    /// Appelé via Animation Event quand le joueur attrape l'arme.
    /// </summary>
    public void HandleWeaponSwitch()
    {
        hasSwitchedWeapon = true;

        if (player.PendingLibraryItem != null && player.PendingLibraryItem.itemPrefab != null)
        {
            GameObject weaponObj = player.PendingLibraryItem.itemPrefab;
            weaponObj.SetActive(true);

            if (weaponObj.TryGetComponent<WeaponDamageDetector>(out var newDetector))
            {
                player.Combat.UpdateWeaponDetector(newDetector);
            }
            else if (player.PendingWeaponItem != null && player.PendingWeaponItem.itemType != ItemType.Consumable)
            {
                Debug.LogWarning($"[PlayerEquipState] Le prefab {weaponObj.name} n'a pas de WeaponDamageDetector !");
            }

            if (player.PendingLibraryItem.elementsToDisable != null)
            {
                foreach (var element in player.PendingLibraryItem.elementsToDisable)
                {
                    if (element != null) element.SetActive(false);
                }
            }

            player.StateMachine.ChangeState(player.Input.MoveInput != Vector2.zero
                ? PlayerStateType.Move : PlayerStateType.Idle);
        }
        else
        {
            Debug.LogError("[PlayerEquipState] PendingLibraryItem ou itemPrefab est NULL !");
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        base.Exit();

        ThirdPersonCameraController.Instance?.ResetFOV();
        changedFOV = false;

        // 🟢 Fallback de sécurité : Si l'état est interrompu (coup subi, chute...) avant l'événement d'animation, 
        // on applique l'activation pour éviter d'avoir une arme invisible.
        if (!hasSwitchedWeapon && player.PendingLibraryItem != null && player.PendingLibraryItem.itemPrefab != null)
        {
            player.PendingLibraryItem.itemPrefab.SetActive(true);
        }
    }

    private bool CanSprint(Vector2 input)
    {
        return input.magnitude > 0.1f
               && player.Input.SprintHeld
               && player.Stamina.HasStamina();
    }
}