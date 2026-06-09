using UnityEngine;

public class PlayerBowChargeState : PlayerGroundedState
{

    private float currentChargeTime;
    private float chargeDuration = 2.067f;

    public PlayerBowChargeState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        currentChargeTime = 0f;
        player.Animator.SetBool(AnimatorHashes.chargeBool, true);
        player.Animator.applyRootMotion = true;

        player.Bow.PrepareArrow();

        // On active l'état de visée sur la caméra au début pour décaler le pivot
        ThirdPersonCameraController.Instance.SetAimState(true);
    }

    public override void Update()
    {
        Debug.Log("Attaque maintenue : " + player.Input.AttackHeld);
        // 1. Gestion de la charge (Inchangé)
        if (currentChargeTime < chargeDuration)
        {
            currentChargeTime += Time.deltaTime;
        }
        float t = Mathf.Clamp01(currentChargeTime / chargeDuration);
        player.Bow.UpdateChargeProgress(t);

        // 2. LOGIQUE DE CAMÉRA DYNAMIQUE
        // On veut TOUJOURS le pivot à l'épaule quand on charge l'arc
        Vector3 currentTargetPivot = ThirdPersonCameraController.Instance.AimPivotOffset;

        // Mais on ne veut le rapprochement (CamOffset) QUE si on vise activement
        Vector3 currentTargetCamOffset = player.Input.AimHeld ?
                                         ThirdPersonCameraController.Instance.AimCamOffset :
                                         ThirdPersonCameraController.Instance.DefaultCamOffset;

        // On applique ces valeurs directement à ton contrôleur de caméra
        ThirdPersonCameraController.Instance.SetManualOffsets(currentTargetPivot, currentTargetCamOffset);

        // FOV : Zoom progressif seulement si on vise ? 
        // Ou zoom léger constant ? Ici, zoom progressif uniquement si AimPressed
        if (player.Input.AimHeld)
        {
            float dynamicFOV = Mathf.Lerp(ThirdPersonCameraController.Instance.DefaultFOV, 40f, t);
            UIManagerSystem.Instance.ShowCrosshair(true);
            ThirdPersonCameraController.Instance.SetFOV(dynamicFOV);
        }
        else
        {
            UIManagerSystem.Instance.ShowCrosshair(false);
            ThirdPersonCameraController.Instance.ResetFOV();
        }

        // 3. Rotation et Tir (Inchangé)
        RotateTowardsCamera();

        // 4. Mouvement & Animation
        Vector2 input = player.Input.MoveInput;
        player.Animator.SetFloat(AnimatorHashes.hHash, input.x, 0.1f, Time.deltaTime);
        player.Animator.SetFloat(AnimatorHashes.vHash, input.y, 0.1f, Time.deltaTime);

        float moveSpeedFactor = Mathf.Lerp(1f, 0.5f, t);
        player.Animator.SetFloat(AnimatorHashes.speedHash, input.magnitude * moveSpeedFactor, 0.1f, Time.deltaTime);

        // 5. Condition de tir (Ce qui était dans "HandleShooting")
        if (!player.Input.AttackAnyHeld)
        {
            if (player.Bow.canShoot)
            {
                player.Bow.ShootArrow();
                player.StateMachine.ChangeState(PlayerStateType.Idle);
            }
            else
            {
                player.Bow.chargeBow = false;
                player.StateMachine.ChangeState(PlayerStateType.Idle);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        player.Animator.SetBool(AnimatorHashes.chargeBool, false);
        player.Animator.SetFloat(AnimatorHashes.speedHash, 0f); 
        player.Input.ResetAllAttackInputs();
        // Reset de la caméra
        ThirdPersonCameraController.Instance.SetAimState(false);
        ThirdPersonCameraController.Instance.ResetFOV();
    }

    private void RotateTowardsCamera()
    {
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        if (camForward != Vector3.zero)
        {
            player.transform.rotation = Quaternion.Slerp(
                player.transform.rotation,
                Quaternion.LookRotation(camForward),
                10f * Time.deltaTime);
        }
    }
}