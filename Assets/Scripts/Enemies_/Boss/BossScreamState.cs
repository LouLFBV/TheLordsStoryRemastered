using UnityEngine;

public class BossScreamState : EnemyState
{
    private BossController _boss;

    private float _armorClassiqueDefault;
    private float _armorTranchantDefault;
    private float _armorContendantDefault;
    private float _armorPercantDefault;

    public BossScreamState(EnemyControllerBase enemy) : base(enemy)
    {
        _boss = enemy as BossController;
    }

    public override void Enter()
    {
        Debug.Log($"<color=red>[BOSS SCREAM]</color> Enter State.");
        _boss.isScreaming = true;
        _boss.Animator.SetBool("IsScreaming", true);

        _boss.Animator.ResetTrigger("Hit");

        _boss.ForceDisableActiveVisual();

        // Sauvegarde des stats d'armure d'origine
        _armorClassiqueDefault = _boss.armor.armorClassique;
        _armorTranchantDefault = _boss.armor.armorTranchant;
        _armorContendantDefault = _boss.armor.armorContendant;
        _armorPercantDefault = _boss.armor.armorPercant;

        // Boost d'armure
        _boss.armor.armorClassique = 9999f;
        _boss.armor.armorTranchant = 9999f;
        _boss.armor.armorContendant = 9999f;
        _boss.armor.armorPercant = 9999f;

        // Immobilisation totale
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        _boss.Animator.SetFloat("Speed", 0f);

        _boss.PlayScreamVFXAndAudio();
    }



    public override void Update()
    {
        if (!_boss.isScreaming) _boss.StateMachine.ChangeState(EnemyStateType.Follow);
    }

    public override void Exit()
    {
        // Restauration des armures d'origine à la sortie de l'état
        _boss.armor.armorClassique = _armorClassiqueDefault;
        _boss.armor.armorTranchant = _armorTranchantDefault;
        _boss.armor.armorContendant = _armorContendantDefault;
        _boss.armor.armorPercant = _armorPercantDefault;

        _boss.Animator.SetBool("IsScreaming", false);

        _boss.isScreaming = false;

        // On libère l'agent NavMesh
        agent.isStopped = false;
    }
}