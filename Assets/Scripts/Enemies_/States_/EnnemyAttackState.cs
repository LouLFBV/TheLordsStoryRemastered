using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private bool isAnimationFinished;
    private float _exitTimer;
    private float _currentPostAttackDelay; // Délai dynamique récupéré de l'AttackSO
    private AttackSO _currentAttack;

    public EnemyAttackState(EnemyControllerBase enemy) : base(enemy) { }


    public override void Enter()
    {
        isAnimationFinished = false;
        _exitTimer = 0f;
        _currentAttack = enemy.GetBestAttack();

        if (_currentAttack != null)
        {
            //Debug.Log($"<color=red>[ATTACK]</color> Lancement de : {_currentAttack.animationName}");
            _currentPostAttackDelay = _currentAttack.postAttackDelay;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            enemy.PrepareAttack(_currentAttack);
            enemy.Combat.ExecuteAttack(_currentAttack);
        }
        else
        {
            //Debug.LogWarning("[ATTACK] Enter sans attaque valide, retour immédiat.");
            isAnimationFinished = true;
            _currentPostAttackDelay = 0f;
        }
    }

    public override void Update()
    {
        // On continue de pivoter tant que l'anim n'est pas finie 
        // (ou tu peux stopper la rotation via un Event si besoin)
        if (!isAnimationFinished)
        {
            //Debug.Log("[ATTACK] En cours d'animation, pivotement vers la cible.");
            //Debug.Log($"[ATTACK] Animation en cours, délai post-attaque: {_currentPostAttackDelay:F2}s");
            FaceTarget();
        }
        else
        {
            //Debug.Log($"[ATTACK] Animation finie, attente du délai post-attaque: {_currentPostAttackDelay:F2}s");
            // 4. Une fois l'animation finie, on attend le délai de l'AttackSO
            _exitTimer += Time.deltaTime;
            if (_exitTimer >= _currentPostAttackDelay)
            {
                isAnimationFinished = false; 
                DetermineNextState();
            }
        }
    }


    private void DetermineNextState()
    {
        // FIX COMPORTEMENT : On vérifie si l'attaque qui vient de se terminer possède une suite officielle (Combo)
        if (_currentAttack != null && _currentAttack.nextAttack != null)
        {
            // On vérifie si cette suite spécifique est prête (cooldown/distance requis)
            // Pour être sûr, on demande à Peek de valider si un combo est possible
            AttackSO nextPotentialAttack = enemy.PeekBestAttack();

            // Si l'IA valide qu'elle peut attaquer ET que le choix se porte sur la suite logique
            if (nextPotentialAttack == _currentAttack.nextAttack)
            {
                Debug.Log($"<color=orange>[COMBO TRUQUÉ]</color> Enchaînement fluide vers le combo officiel : {nextPotentialAttack.animationName}");
                this.Enter(); // On enchaîne sans repasser par l'état Follow
                return; // On s'arrête là !
            }
        }

        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);
        Debug.Log($"[ATTACK] Fin de l'attaque. Pas de combo. Distance: {distance:F2}. Application du repos.");

        if (distance <= 4f && enemy.AIManager.HasPermission(EnemyStateType.Orbit))
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
            // Vitesse de rotation pendant l'attaque (peut être ajustée par SO aussi !)
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public void OnAnimationFinished()
    {
        Debug.Log("[ATTACK] Animation event reçu : animation terminée.");
        isAnimationFinished = true;
    }

    public override void Exit()
    {
        // Reset de la vitesse pour le prochain état
        agent.isStopped = false;
        enemy.lastAttackExitTime = Time.time;
        // Cooldown global pour éviter l'orbite spam
        enemy.AIManager.StartOrbitCooldown(2f);
    }
}