using UnityEngine;
using System.Collections;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    private HealthSystem _health;
    private PoiseSystem _poise;
    private PlayerController _player;
    private EnemyController _enemy;
    private ArmorSystem _armor;
    private EnemyParent _enemyParent; // Conservé pour tes méthodes d'IA spécifiques (vitesse, agent)

    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _poise = GetComponent<PoiseSystem>();
        _player = GetComponent<PlayerController>();
        _enemy = GetComponent<EnemyController>();
        _armor = GetComponent<ArmorSystem>();
        _enemyParent = GetComponent<EnemyParent>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        // 1. Calcul de l'armure (Réduction uniquement sur les dégâts physiques)
        float finalPhysicalDamage = (_armor != null)
            ? _armor.CalculateReducedDamage(damageInfo, out float _)
            : damageInfo.rawPhysicalDamage;

        // 2. Application des dégâts physiques à la vie
        if (_health != null)
        {
            _health.TakeDamage(finalPhysicalDamage);
        }

        // 3. Logique de Stun (Poise)
        if (_poise != null && _poise.ApplyPoiseDamage(damageInfo.poiseDamage))
        {
            TriggerHitReaction();
        }

        // 4. Application des Effets Élémentaires Bruts (Ils ignorent l'armure !)
        if (damageInfo.elementalEffect != Effet.None)
        {
            ApplyElementalEffect(damageInfo);
        }
    }

    private void ApplyElementalEffect(DamageInfo damageInfo)
    {
        // Si c'est une IA de type EnemyParent, on applique tes coroutines d'effets spécifiques
        if (_enemyParent != null)
        {
            switch (damageInfo.elementalEffect)
            {
                case Effet.Feu:
                    StartCoroutine(FeuDotCoroutine(damageInfo.rawPhysicalDamage));
                    break;

                case Effet.Glace:
                    StartCoroutine(GlaceSlowCoroutine());
                    break;

                case Effet.Foudre:
                    StartCoroutine(FoudreStunCoroutine());
                    break;
            }
        }
        else
        {
            // Logique alternative si le joueur subit l'effet (ex: visuel à l'écran, etc.)
            Debug.Log($"Le joueur subit l'effet élémentaire : {damageInfo.elementalEffect}");
        }
    }

    // --- COROUTINES DES EFFETS CENTRALISÉES ET SÉCURISÉES ---

    private IEnumerator FeuDotCoroutine(float baseDamage)
    {
        // Applique des tics de brûlure brute (5 fois, 20% des dégâts de base)
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(1f);
            if (_health != null) _health.TakeDamage(baseDamage * 0.2f);
        }
    }

    private IEnumerator GlaceSlowCoroutine()
    {
        if (_enemyParent == null) yield break;

        _enemyParent.UpdateSpeedWitchCoefficient(0.5f);
        yield return new WaitForSeconds(3f);
        _enemyParent.UpdateSpeedWitchCoefficient(2f);
    }

    private IEnumerator FoudreStunCoroutine()
    {
        if (_enemyParent == null || _enemyParent.agent == null) yield break;

        _enemyParent.agent.isStopped = true;
        yield return new WaitForSeconds(1.5f);

        // Sécurité au cas où l'ennemi meurt pendant le stun
        if (_enemyParent != null && _enemyParent.agent != null)
        {
            _enemyParent.agent.isStopped = false;
        }
    }

    private void TriggerHitReaction()
    {
        if (_player != null)
        {
            if (_poise != null && _poise.IsBroken)
                _player.StateMachine.ChangeState(PlayerStateType.Stunned);
            else
                _player.StateMachine.ChangeState(PlayerStateType.Hit);
        }

        if (_enemy != null)
            _enemy.StateMachine.ChangeState(EnemyStateType.Hit);
    }
}