using UnityEngine;

public class PlayerAttackState : PlayerGroundedState
{
    private bool animationFinished; 

    public PlayerAttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        animationFinished = false;

        // On récupère la première attaque de l'arme
        var combatData = player.PendingWeaponItem?.combatData;
        if (player.CurrentAttack == null)
        {
            if (player.usingSpecialAttack)
            {
                player.CurrentAttack = combatData?.specialAttack;
                Debug.Log($"Starting Special Attack: {player.CurrentAttack}");
            }
            else
            {
                player.CurrentAttack = combatData?.startingAttack;
                Debug.Log($"Starting Attack: {player.CurrentAttack}");
            }
        }
        if (player.CurrentAttack == null)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            return;
        }
        if (player.CurrentAttack.attackSound != null)
        {
            player.PlayLocalSound(player.CurrentAttack.attackSound);
        }
        // On ordonne l'exécution
        player.Combat.ExecuteAttack(player.CurrentAttack);
        player.Animator.SetLayerWeight(player.CurrentAttack.animatorLayer, 1f);
    }

    public override void Update()
    {
        //base.Update();
        // 1. Buffer d'input : si on clique pendant que canCombo est vrai
        if (player.Input.AttackSpecialPressed && player.Combat.CanComboNext())
        {
            Debug.Log("Input d'attaque enregistré pour le combo !");
            player.Input.UseAttackInput();
            player.Input.UseAttackSpecialInput();
            if (player.CurrentAttack.nextAttack != null)
            {
                // On prépare la suite
                player.CurrentAttack = player.CurrentAttack.nextAttack;
                if (player.CurrentAttack.attackSound != null)
                {
                    player.PlayLocalSound(player.CurrentAttack.attackSound);
                }
                player.Combat.ExecuteAttack(player.CurrentAttack);
            }
        }

        if (player.Input.RollPressed && player.Stamina.HasStamina())
        {
            player.Input.UseRollInput();
            player.Combat.InterruptAttack();
            player.StateMachine.ChangeState(PlayerStateType.Roll);
            return;
        }


        if (animationFinished)
        {
            Debug.Log("AttackFinished");
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }
    public override void FixedUpdate()
    {
        if (!player.CurrentAttack.isSimpleAttack) return;
        Vector2 input = player.Input.MoveInput;
        player.Motor.RotateTowardsInput(input);
        // Paramètres Animator pour Root Motion
        player.Animator.SetFloat(AnimatorHashes.hHash, input.x, 0.1f, Time.deltaTime);
        player.Animator.SetFloat(AnimatorHashes.vHash, input.y, 0.1f, Time.deltaTime);
        player.Animator.SetFloat(AnimatorHashes.speedHash, input.magnitude * 3f/7f, 0.1f, Time.deltaTime);
    }

    public override void Exit()
    {
        base.Exit();
        player.usingSpecialAttack = false;

        // Si on quitte l'attaque normalement, on reset aussi le poids
        if (player.CurrentAttack != null)
            player.Animator.SetLayerWeight(player.CurrentAttack.animatorLayer, 0f);

        animationFinished = true;
        player.CurrentAttack = null;
    }

    public void OnAnimationFinished() => animationFinished = true;
}