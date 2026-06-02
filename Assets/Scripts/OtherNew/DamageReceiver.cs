using UnityEngine;
using System.Collections;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    private HealthSystem _health;
    private PoiseSystem _poise;
    private PlayerController _player;
    private EnemyController _enemy;
    private ArmorSystem _armor;

    // Propriété publique pour que l'IA connaisse son ralentissement actuel
    public float SpeedModifier { get; private set; } = 1f;

    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _poise = GetComponent<PoiseSystem>();
        _player = GetComponent<PlayerController>();
        _enemy = GetComponent<EnemyController>();
        _armor = GetComponent<ArmorSystem>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        float finalPhysicalDamage = (_armor != null)
            ? _armor.CalculateReducedDamage(damageInfo, out float _)
            : damageInfo.rawPhysicalDamage;

        if (_health != null) _health.TakeDamage(finalPhysicalDamage);

        // Logique de Poise / Hit classique
        if (_poise != null && _poise.ApplyPoiseDamage(damageInfo.poiseDamage))
        {
            TriggerHitReaction();
        }

        // Effets élémentaires
        if (damageInfo.elementalEffect != Effet.None)
        {
            ApplyElementalEffect(damageInfo);
        }
    }

    private void ApplyElementalEffect(DamageInfo damageInfo)
    {
        switch (damageInfo.elementalEffect)
        {
            case Effet.Feu:
                StartCoroutine(FeuDotCoroutine(damageInfo.rawPhysicalDamage));
                break;

            case Effet.Glace:
                // Pas besoin de toucher à l'agent direct, on lance juste le timer du débuff
                StartCoroutine(GlaceSlowCoroutine());
                break;

            case Effet.Foudre:
                TriggerStun(1.5f);
                break;
        }
    }

    private void TriggerStun(float duration)
    {
        if (_enemy != null)
        {
            // On récupère l'état, on lui donne la durée, et on bascule
            var stunnedState = _enemy.StateMachine.GetState(EnemyStateType.Stunned) as EnemyStunnedState;
            if (stunnedState != null && _enemy.AIManager.HasStunnedAnim)
            {
                stunnedState.SetDuration(duration);
                _enemy.StateMachine.ChangeState(EnemyStateType.Stunned);
            }
        }
        if (_player != null)
        {
            // Idem pour ton joueur si tu lui crées un PlayerStunnedState
            _player.StateMachine.ChangeState(PlayerStateType.Stunned);
        }
    }

    private IEnumerator GlaceSlowCoroutine()
    {
        SpeedModifier = 0.5f; // On réduit de moitié
        yield return new WaitForSeconds(3f);
        SpeedModifier = 1f;  // Retour à la normale
    }

    private IEnumerator FeuDotCoroutine(float baseDamage)
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(1f);
            if (_health != null) _health.TakeDamage(baseDamage * 0.2f);
        }
    }

    private void TriggerHitReaction()
    {
        // On n'interrompt pas si l'ennemi est déjà paralysé par la foudre
        if (_enemy != null && _enemy.StateMachine.CurrentState != _enemy.StunnedState && _enemy.AIManager.CanGetHit)
        {
            _enemy.StateMachine.ChangeState(EnemyStateType.Hit);
            // Exemple dans ton IA quand elle est touchée :
            _enemy.target = PlayerController.Instance.transform;
        }
        if (_player != null)
        {
            if (_poise != null && _poise.IsBroken)
                _player.StateMachine.ChangeState(PlayerStateType.Stunned);
            else 
                _player.StateMachine.ChangeState(PlayerStateType.Hit);
        }
    }
}