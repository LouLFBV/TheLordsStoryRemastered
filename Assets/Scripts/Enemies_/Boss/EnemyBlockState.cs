using UnityEngine;

public class EnemyBlockState : EnemyState
{
    private float _timer;
    private bool _animationFinished;

    private float _armorClassiqueDefault;
    private float _armorTranchantDefault;
    private float _armorContendantDefault;
    private float _armorPercantDefault;

    public EnemyBlockState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        Debug.Log($"<color=yellow>[GUARD]</color> {enemy.gameObject.name} lève sa garde !");
        _timer = 0f;
        _animationFinished = false; // 🟢 On réinitialise le flag

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        enemy.Animator.SetFloat("Speed", 0f);

        // Déclenche l'animation (ex: un Trigger "Block" ou Bool "IsBlocking")
        enemy.Animator.SetBool("IsBlocking", true);

        // Armure Divine
        _armorClassiqueDefault = enemy.armor.armorClassique;
        _armorTranchantDefault = enemy.armor.armorTranchant;
        _armorContendantDefault = enemy.armor.armorContendant;
        _armorPercantDefault = enemy.armor.armorPercant;

        enemy.armor.armorClassique = 9999f;
        enemy.armor.armorTranchant = 9999f;
        enemy.armor.armorContendant = 9999f;
        enemy.armor.armorPercant = 9999f;
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        if (enemy.target != null)
        {
            Vector3 dir = (enemy.target.position - enemy.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            }
        }

        if (_animationFinished || _timer >= enemy.enemyData.blockDuration)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Follow);
        }
    }

    public void NotifyAnimationFinished()
    {
        _animationFinished = true;
    }

    public override void Exit()
    {
        Debug.Log($"<color=yellow>[GUARD]</color> {enemy.gameObject.name} baisse sa garde.");

        enemy.armor.armorClassique = _armorClassiqueDefault;
        enemy.armor.armorTranchant = _armorTranchantDefault;
        enemy.armor.armorContendant = _armorContendantDefault;
        enemy.armor.armorPercant = _armorPercantDefault;

        enemy.Animator.SetBool("IsBlocking", false);
        agent.isStopped = false;

        enemy.nextBlockTime = Time.time + enemy.enemyData.blockCooldown;
    }
}