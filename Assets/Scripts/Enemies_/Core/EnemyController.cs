using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyController : EnemyControllerBase
{
    [Header("States")]
    public EnemyAttackState AttackState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        AttackState = new EnemyAttackState(this);
        StateMachine.AddState(EnemyStateType.Attack, AttackState);
    }

    public void AE_OnAttackFinished() => (StateMachine.CurrentState as EnemyAttackState)?.OnAnimationFinished();
}