using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealthVar = 100f; 
    [SerializeField] private ParticleSystem healEffect;

    private bool _isInvulnerable;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealthVar; // Propriété en lecture seule pour le maxHealth
    public bool IsDead => CurrentHealth <= 0;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnHealthEnemyChanged;
    public event Action OnDeath;
    public event Action OnHit;

    private void Awake()
    {
        CurrentHealth = maxHealthVar;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"Dégâts reçus : {damage}");
        if (_isInvulnerable)
        {
            Debug.Log("Esquivé !");
            return;
        }
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealthEnemyChanged?.Invoke(CurrentHealth, MaxHealth);
        OnHit?.Invoke();

        if (CurrentHealth <= 0)
            OnDeath?.Invoke();
    }
    public void Heal(float amount)
    {
        // Si on est déjà full vie, on peut choisir de ne pas consommer l'objet
        // Mais si on soigne, on lance la logique :
        if (CurrentHealth < MaxHealth)
        {
            if (healEffect != null) healEffect.Play();

            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);

            // C'est CA qui remplace "UpdateHealthBar" ! 
            // Ton script d'UI doit être abonné à cet event.
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }


    public void SetInvulnerable(bool state)
    {
        _isInvulnerable = state;
        // Optionnel : Changer la couleur ou ajouter un effet de transparence
    }

    public void SetHealth(float newHealth) 
    {
        CurrentHealth = newHealth;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}