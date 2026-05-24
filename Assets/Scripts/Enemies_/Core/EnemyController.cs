using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyController : EnemyControllerBase
{
    [Header("States")]
    public EnemyAttackState AttackState { get; private set; }

    [Header("Music Tracking Settings")]
    private Collider _enemyCollider;
    private bool _isCurrentlyChasing = false;

    protected override void Awake()
    {
        base.Awake();
        AttackState = new EnemyAttackState(this);
        StateMachine.AddState(EnemyStateType.Attack, AttackState);

        _enemyCollider = GetComponent<Collider>();
    }

    protected override void Start()
    {
        base.Start();

        if (Health != null)
        {
            Health.OnDeath += HandleDeath;
        }
    }

    protected override void Update()
    {
        base.Update();

        // On évalue le statut uniquement si l'ennemi n'est pas mort
        if (Health != null && !Health.IsDead)
        {
            EvaluateChasingStatus();
        }
    }

    private void EvaluateChasingStatus()
    {
        if (StateMachine.CurrentState == null || CombatMusicTracker.Instance == null) return;

        // Un ennemi est en combat s'il est en Follow, Orbit ou Attack
        bool isInCombatState = StateMachine.CurrentState == FollowState ||
                               StateMachine.CurrentState == OrbitState ||
                               StateMachine.CurrentState == AttackState;

        if (isInCombatState != _isCurrentlyChasing)
        {
            _isCurrentlyChasing = isInCombatState;

            if (_isCurrentlyChasing)
            {
                CombatMusicTracker.Instance.RegisterChaser(_enemyCollider);
            }
            else
            {
                CombatMusicTracker.Instance.UnregisterChaser(_enemyCollider);
            }
        }
    }

    // Fonction appelée automatiquement par l'événement de mort
    private void HandleDeath()
    {
        // Si l'ennemi était en train de chasser le joueur au moment de sa mort, on le retire
        if (_isCurrentlyChasing && CombatMusicTracker.Instance != null)
        {
            _isCurrentlyChasing = false;
            CombatMusicTracker.Instance.UnregisterChaser(_enemyCollider);
        }
    }

    // Sécurité au cas où le joueur quitte la scène ou si l'ennemi est détruit proprement
    protected override void OnDisable()
    {
        base.OnDisable();

        // Désabonnement pour éviter les fuites de mémoire
        if (Health != null)
        {
            Health.OnDeath -= HandleDeath;
        }

        if (_isCurrentlyChasing && CombatMusicTracker.Instance != null)
        {
            _isCurrentlyChasing = false;
            CombatMusicTracker.Instance.UnregisterChaser(_enemyCollider);
        }
    }

    public void AE_OnAttackFinished() => (StateMachine.CurrentState as EnemyAttackState)?.OnAnimationFinished();
}