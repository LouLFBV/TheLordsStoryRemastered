using UnityEngine;

public class PlayerRollState : PlayerGroundedState
{
    private bool isRollFinished;
    private Vector3 rollDirection;

    public PlayerRollState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        isRollFinished = false;
        // --- RESET TOTAL ---
        player.Rigidbody.isKinematic = true;  // On "éteint" la physique
        player.Rigidbody.isKinematic = false; // On la rallume immédiatement
        player.Rigidbody.linearVelocity = Vector3.zero;
        player.Rigidbody.angularVelocity = Vector3.zero;

        player.Motor.SetFriction(false); // Utilise ton SlipperyMaterial à 0 friction
        player.Animator.applyRootMotion = false;

        player.Stamina.Spend(30f); 
        player.Animator.SetTrigger(AnimatorHashes.rollTrigger);
        player.Motor.StartRollCollider();
        player.Health.SetInvulnerable(true);

        CalculateRollDirection();
    }

    public override void Update()
    {
        if (isRollFinished)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    public override void FixedUpdate()
    {
        if (!isRollFinished)
        {
            // LOG 3 : On regarde si la vélocité a changé entre la frame précédente et celle-ci (pollution externe ?)
            Vector3 velBefore = player.Rigidbody.linearVelocity;

            Vector3 targetVelocity = rollDirection * player.rollForce;
            targetVelocity.y = player.Rigidbody.linearVelocity.y;

            player.Rigidbody.linearVelocity = targetVelocity;

        }
    }

    public override void Exit()
    {
        base.Exit();
        player.Motor.EndRollCollider();
        player.Health.SetInvulnerable(false);
        player.Animator.applyRootMotion = true;
    }

    public void OnRollAnimationEnd()
    {
        isRollFinished = true;
    }

    private void CalculateRollDirection()
    {
        Vector2 input = player.Input.MoveInput;

        if (input.sqrMagnitude > 0.1f)
            rollDirection = player.Motor.GetDirectionFromInput(input).normalized;
        else
            rollDirection = player.transform.forward.normalized;

        player.transform.rotation = Quaternion.LookRotation(rollDirection);
    }
}