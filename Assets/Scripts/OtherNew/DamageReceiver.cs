using UnityEngine;
using System.Collections;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    private HealthSystem _health;
    private PoiseSystem _poise;
    private PlayerController _player;
    private EnemyControllerBase _enemy; 
    private ArmorSystem _armor;

    public float SpeedModifier { get; private set; } = 1f;

    [SerializeField] private GameObject burnGameObject;

    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _poise = GetComponent<PoiseSystem>();
        _player = GetComponent<PlayerController>();
        _enemy = GetComponent<EnemyControllerBase>(); 
        _armor = GetComponent<ArmorSystem>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        float finalPhysicalDamage = (_armor != null)
            ? _armor.CalculateReducedDamage(damageInfo, out float _)
            : damageInfo.rawPhysicalDamage;

        if (_health != null) _health.TakeDamage(finalPhysicalDamage);

        // Logique de Poise / Hit classique
        if (_poise != null && _poise.ApplyPoiseDamage(damageInfo.poiseDamage) && !_health.IsInvulnerable)
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
                StartCoroutine(GlaceSlowCoroutine());
                break;

            case Effet.Foudre:
                if (_enemy != null && (_enemy.isScreaming || _enemy.StateMachine.CurrentState == _enemy.BlockState))
                    break;
                TriggerStun(damageInfo.stunDuration);
                break;
        }
    }

    private void TriggerStun(float duration)
    {
        if (_enemy != null)
        {
            var stunnedState = _enemy.StateMachine.GetState(EnemyStateType.Stunned) as EnemyStunnedState;
            if (stunnedState != null && _enemy.AIManager.HasStunnedAnim)
            {
                stunnedState.SetDuration(duration);
                _enemy.StateMachine.ChangeState(EnemyStateType.Stunned);
            }
        }
        if (_player != null)
        {
            _player.StateMachine.ChangeState(PlayerStateType.Stunned);
        }
    }

    private IEnumerator GlaceSlowCoroutine()
    {
        SpeedModifier = 0.5f;
        yield return new WaitForSeconds(3f);
        SpeedModifier = 1f;
    }

    private IEnumerator FeuDotCoroutine(float baseDamage)
    {
        if (burnGameObject != null) burnGameObject.SetActive(true);
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(1f);
            if (_health != null) _health.TakeDamage(baseDamage * 0.3f);
        }
        if (burnGameObject != null) burnGameObject.SetActive(false);
    }

    private void TriggerHitReaction()
    {
        if (_enemy != null)
        {
            if (_enemy.StateMachine.CurrentState != _enemy.StunnedState && _enemy.AIManager.CanGetHit)
            {
                _enemy.GoToHitState();

                _enemy.target = PlayerController.Instance.transform;
            }
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