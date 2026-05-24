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
    
    public AttackSO CurrentAttack => _currentAttack;

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
            // Au lieu de bloquer complètement la rotation, on ajuste sa vitesse !
            if (_isApplyingMovement)
            {
                // Pendant qu'il fonce/saute, il tourne plus lentement pour "ajuster" sa cible de manière réaliste
                FaceTargetWithSpeed(2f);
            }
            else
            {
                // En dehors du déplacement pur, il se tourne normalement et rapidement
                FaceTargetWithSpeed(5f);
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

    private void FaceTargetWithSpeed(float rotationSpeed)
    {
        if (enemy.target == null) return;

        // On calcule la direction vers le joueur
        Vector3 direction = (enemy.target.position - enemy.transform.position).normalized;
        direction.y = 0; // On reste sur un plan horizontal pour éviter que le boss ne penche en avant/arrière

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // On applique un Slerp avec la vitesse passée en paramètre
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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
        // 1. Logique de Combo
        if (_currentAttack != null && _currentAttack.nextAttack != null)
        {
            AttackSO nextPotentialAttack = enemy.PeekBestAttack();

            if (nextPotentialAttack == _currentAttack.nextAttack)
            {
                Debug.Log($"<color=darkred>[BOSS COMBO]</color> Enchaînement vers : {nextPotentialAttack.animationName}");
                this.Enter();
                return;
            }
        }

        // 2. --- SI PAS DE COMBO OFFICIEL ---
        // On repasse en Follow DANS TOUS LES CAS pour que l'IA reprenne sa marche/poursuite/sélection d'attaque
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

        (enemy as BossController)?.ForceDisableActiveVisual();

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
    }
}