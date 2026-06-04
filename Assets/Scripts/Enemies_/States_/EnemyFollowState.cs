using Unity.VisualScripting;
using UnityEngine;

public class EnemyFollowState : EnemyState
{
    public EnemyFollowState(EnemyControllerBase enemy) : base(enemy) { }

    public override void Enter()
    {
        agent.speed = enemy.enemyData.chaseSpeed;
        agent.isStopped = false;

        if (enemy.enemyData != null && enemy.enemyData.runSound != null)
        {
            enemy.ChangeLoopingSound(enemy.enemyData.runSound);
        }
    }

    public override void Update()
    {
        // 1. SÉCURITÉ ABSOLUE : Si le boss crie, on fige TOUT et on stoppe immédiatement l'état de poursuite
        if (enemy.isScreaming)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            enemy.Animator.SetFloat("Speed", 0f);
            return;
        }

        // 2. Vérification de la cible
        if (enemy.target == null)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);

        // 3. Gestion de la vitesse et des effets de gel/slow
        float currentSlow = enemy.DmgReceiver.SpeedModifier;
        agent.speed = enemy.enemyData.chaseSpeed * currentSlow;
        enemy.Animator.speed = currentSlow;

        // 4. Gestion de la première Aggro
        if (!enemy.HasAggroedOnce && distance <= enemy.enemyData.visionRange)
        {
            enemy.HasAggroedOnce = true;
            Debug.Log($"<color=orange>[AGGRO]</color> {enemy.gameObject.name} a atteint le joueur. Mode normal activé.");
        }

        // 5. PRIORITÉ 1 EN COMBAT : Est-ce qu'on peut attaquer ?
        AttackSO ready = enemy.PeekBestAttack();
        if (ready != null)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Attack);
            return;
        }

        // 6. PRIORITÉ 2 EN COMBAT : Est-ce qu'on doit bloquer ? (Seulement si pas d'attaque prête !)
        if (enemy.AIManager.CanBlock && Time.time >= enemy.nextBlockTime)
        {
            if (distance <= enemy.enemyData.distToStartBlock && Random.value < 0.008f)
            {
                enemy.StateMachine.ChangeState(EnemyStateType.Block);
                return;
            }
        }

        // 7. Distances de confort et d'outils de combat (Orbit)
        if (distance <= agent.stoppingDistance + 0.5f && ready == null)
        {
            // Au contact mais aucune attaque possible (en cooldown...)
        }

        if (enemy.AIManager.HasPermission(EnemyStateType.Orbit) && distance <= enemy.AIManager.OrbitDistance + 2f)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Orbit);
            return;
        }

        // 8. Perte de l'aggro si le joueur s'enfuit trop loin
        if (enemy.HasAggroedOnce && distance > enemy.enemyData.visionRange * 1.5f)
        {
            enemy.StateMachine.ChangeState(EnemyStateType.Idle);
            return;
        }

        // 9. DÉPLACEMENT : Si aucune action de combat n'est requise, on avance !
        agent.SetDestination(enemy.target.position);

        // Animation de course dynamique
        float speedParameter = agent.velocity.magnitude / enemy.enemyData.chaseSpeed;
        enemy.Animator.SetFloat("Speed", speedParameter, 0.1f, Time.deltaTime);
    }

    public override void Exit()
    {
        if (agent.isActiveAndEnabled)
            agent.isStopped = true;

        if (enemy.Animator != null)
            enemy.Animator.speed = 1f;
    }
}