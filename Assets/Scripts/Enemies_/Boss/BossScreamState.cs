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
        // 1. Sauvegarde des stats d'armure d'origine
        _armorClassiqueDefault = _boss.armor.armorClassique;
        _armorTranchantDefault = _boss.armor.armorTranchant;
        _armorContendantDefault = _boss.armor.armorContendant;
        _armorPercantDefault = _boss.armor.armorPercant;

        // 2. On booste l'armure à un niveau divin (9999 assure que même un coup critique fait 0 ou le minimum de 1)
        _boss.armor.armorClassique = 9999f;
        _boss.armor.armorTranchant = 9999f;
        _boss.armor.armorContendant = 9999f;
        _boss.armor.armorPercant = 9999f;

        // 3. SÉCURITÉ : On active le flag pour que le gestionnaire de dégâts refuse de le STUN/HIT
        _boss.isScreaming = true;

        // 4. Immobilisation totale
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        _boss.Animator.SetFloat("Speed", 0f);

        // 5. Lancement des feedbacks sonores et visuels
        _boss.PlayScreamVFXAndAudio();
    }

    public override void Update()
    {
        // On attend l'Animation Event de fin de cri
    }

    public override void Exit()
    {
        // 1. Restauration des armures d'origine à la sortie de l'état
        _boss.armor.armorClassique = _armorClassiqueDefault;
        _boss.armor.armorTranchant = _armorTranchantDefault;
        _boss.armor.armorContendant = _armorContendantDefault;
        _boss.armor.armorPercant = _armorPercantDefault;

        // 2. On libère le flag d'interruption
        _boss.isScreaming = false;

        // 3. On libère l'agent NavMesh
        agent.isStopped = false;
    }
}