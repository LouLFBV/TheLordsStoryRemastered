using UnityEngine;

public class BossAttackState : EnemyState
{
    private bool isAnimationFinished;
    private float _exitTimer;
    private float _currentPostAttackDelay;
    private AttackSO _currentAttack;

    // Variables de mouvement avancées
    private float _movementTimer;
    private bool _isApplyingMovement;
    private float _startY; // Pour savoir où le boss doit atterrir

    public BossAttackState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        Debug.Log($"<color=red>[BOSS ATTACK]</color> Enter State.");
        isAnimationFinished = false;
        _exitTimer = 0f;
        _movementTimer = 0f;
        _isApplyingMovement = false;

        _currentAttack = enemy.GetBestAttack();

        if (_currentAttack != null)
        {
            Debug.Log($"<color=red>[BOSS ATTACK]</color> Lancement : {_currentAttack.animationName}");
            _currentPostAttackDelay = _currentAttack.postAttackDelay;

            // Désactivation du NavMeshAgent pour libérer complètement le Transform
            if (agent != null)
            {
                agent.velocity = Vector3.zero;
                agent.enabled = false;
            }

            // On stocke la position de départ au sol
            _startY = enemy.transform.position.y;

            if (_currentAttack.useAdvancedMovement)
            {
                _isApplyingMovement = true;

                // Optionnel : Désactiver la gravité du Rigidbody si tu en as un, 
                // pour éviter qu'il lutte contre notre courbe verticale
                if (enemy.Rigidbody != null) enemy.Rigidbody.isKinematic = true;
            }

            enemy.PrepareAttack(_currentAttack);
            enemy.Combat.ExecuteAttack(_currentAttack);
        }
        else
        {
            isAnimationFinished = true;
            _currentPostAttackDelay = 0f;
        }
    }

    public override void Update()
    {
        if (_isApplyingMovement && _currentAttack != null)
        {
            _movementTimer += Time.deltaTime;
            float normalizedTime = _movementTimer / _currentAttack.movementDuration;

            if (normalizedTime <= 1.0f)
            {
                // 1. CALCUL HORIZONTAL (Axe Z - Avant / Arrière)
                float forwardEval = _currentAttack.forwardMovementCurve.Evaluate(normalizedTime);
                Vector3 forwardMove = enemy.transform.forward * forwardEval * _currentAttack.forwardForce * Time.deltaTime;

                // 2. CALCUL VERTICAL (Axe Y - Saut / Écrasement)
                float verticalEval = _currentAttack.verticalMovementCurve.Evaluate(normalizedTime);
                float targetY = _startY + (verticalEval * _currentAttack.verticalForce);

                // On applique le déplacement horizontal
                enemy.transform.position += forwardMove;

                // On applique la hauteur absolue calculée par la courbe
                Vector3 currentPos = enemy.transform.position;
                currentPos.y = targetY;
                enemy.transform.position = currentPos;
            }
            else
            {
                // Fin de la fenêtre de déplacement
                _isApplyingMovement = false;
                SnapToFloor();
            }
        }

        // Logique de rotation et de transition
        if (!isAnimationFinished)
        {
            // On empêche le boss de pivoter sur lui-même s'il est en plein saut/fente rapide
            if (!_isApplyingMovement)
            {
                FaceTarget();
            }
        }
        else
        {
            _exitTimer += Time.deltaTime;
            if (_exitTimer >= _currentPostAttackDelay)
            {
                isAnimationFinished = false;
                DetermineNextState();
            }
        }
    }

    private void SnapToFloor()
    {
        // Sécurité : On s'assure que le boss est bien revenu à sa hauteur initiale au sol
        Vector3 finalPos = enemy.transform.position;
        finalPos.y = _startY;
        enemy.transform.position = finalPos;

        if (enemy.Rigidbody != null) enemy.Rigidbody.isKinematic = false;
    }

    private void DetermineNextState()
    {
        // FIX COMPORTEMENT : On autorise le réenchaînement (combo) UNIQUEMENT si l'attaque actuelle l'exige
        if (_currentAttack != null && _currentAttack.nextAttack != null)
        {
            // On vérifie si l'IA valide les conditions (distance, cooldown) pour cette suite précise
            AttackSO nextPotentialAttack = enemy.PeekBestAttack();

            if (nextPotentialAttack == _currentAttack.nextAttack)
            {
                Debug.Log($"<color=darkred>[BOSS COMBO]</color> Enchaînement vers : {nextPotentialAttack.animationName}");
                this.Enter(); // On relance proprement l'état avec la nouvelle attaque du combo
                return; // On stoppe l'exécution ici
            }
        }

        // --- SI PAS DE COMBO OFFICIEL ---
        // Le boss doit obligatoirement faire une pause et changer d'état, 
        // ce qui va déclencher Exit() et enregistrer son 'lastAttackExitTime'
        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);
        Debug.Log($"[BOSS ATTACK] Fin des enchaînements. Distance joueur : {distance:F2}m. Retrait.");

        if (distance <= 5f)
            enemy.StateMachine.ChangeState(EnemyStateType.Orbit);
        else
            enemy.StateMachine.ChangeState(EnemyStateType.Follow);
    }

    private void FaceTarget()
    {
        if (enemy.target == null) return;
        Vector3 direction = (enemy.target.position - enemy.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * 4f);
        }
    }

    public void OnAnimationFinished()
    {
        isAnimationFinished = true;
        if (_isApplyingMovement)
        {
            _isApplyingMovement = false;
            SnapToFloor();
        }
    }

    public override void Exit()
    {
        Debug.Log($"<color=red>[BOSS ATTACK]</color> Exit State. Restauration du NavMeshAgent.");

        SnapToFloor();

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isActiveAndEnabled)
            {
                agent.Warp(enemy.transform.position);
                agent.isStopped = false;
            }
        }

        enemy.lastAttackExitTime = Time.time;
        if (enemy.AIManager != null)
            enemy.AIManager.StartOrbitCooldown(1.5f);
    }
}