using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    private Vector2 cachedInput;

    // Vitesses
    private float runSpeed = 4f;
    private float sprintSpeed = 7f;

    private bool isSprinting;
    private bool changedFOV;

    public PlayerMoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("Enter: Move");
        base.Enter();
        player.Animator.applyRootMotion = true;
    }

    public override void Update()
    {
        base.Update();
        Vector2 input = player.Input.MoveInput;
        cachedInput = input;

        if (input == Vector2.zero)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            return;
        }

        player.Motor.RotateTowardsInput(input);

        // Sprint
        isSprinting = CanSprint(input);
        float animSpeed = isSprinting ? sprintSpeed : runSpeed;

        // FOV sprint
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

        // ─── NOUVEAUTÉ : APPLICATION DU SPEED MODIFIER (Root Motion) ───
        float currentSlow = 1f;
        if (player.DmgReceiver != null)
        {
            currentSlow = player.DmgReceiver.SpeedModifier;
        }

        player.Animator.speed = currentSlow;


        // Paramètres Animator pour Root Motion
        player.Animator.SetFloat(AnimatorHashes.hHash, input.x, 0.1f, Time.deltaTime);
        player.Animator.SetFloat(AnimatorHashes.vHash, input.y, 0.1f, Time.deltaTime);

        // On garde le calcul de base ici pour le Blend Tree, l'Animator.speed s'occupe du reste
        player.Animator.SetFloat(AnimatorHashes.speedHash, input.magnitude * (animSpeed / sprintSpeed), 0.1f, Time.deltaTime);

        // Stamina
        if (isSprinting)
            player.Stamina.Spend(player.Stamina.consommationRate * Time.deltaTime);

        if (player.Input.SprintHeld)
        {
            if (isSprinting)
            {
                player.Stamina.Spend(player.Stamina.consommationRate * Time.deltaTime);
            }
            else if (!player.Stamina.HasStamina())
            {
                player.Stamina.RequestEmptyFeedback();
            }
        }
    }

    public override void FixedUpdate()
    {
        player.Motor.RotateTowardsInput(cachedInput);
    }

    public override void Exit()
    {
        base.Exit();
        ThirdPersonCameraController.Instance.ResetFOV();
        changedFOV = false;

        if (player.Animator != null)
            player.Animator.speed = 1f;
    }

    private bool CanSprint(Vector2 input)
    {
        return input.magnitude > 0.1f
               && player.Input.SprintHeld
               && player.Stamina.HasStamina();
    }
}