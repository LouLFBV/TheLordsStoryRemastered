using UnityEngine;
using System;

public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 15f;
    [SerializeField] private float regenDelay = 1f;
    [SerializeField] private float regenDelayEmpty = 3f; // Pénalité si vide
    [SerializeField] private float minimumStaminaToRestart = 10f;

    public float CurrentStamina { get; private set; }
    public float consommationRate = 1f;
    private float _regenTimer;
    private bool _isExhausted;

    public event Action<float, float> OnStaminaChanged;
    public event Action OnStaminaEmpty;

    private void Awake() => CurrentStamina = maxStamina;

    private void Update()
    {
        if (_regenTimer > 0)
        {
            _regenTimer -= Time.deltaTime;
            return;
        }

        if (CurrentStamina < maxStamina)
        {
            CurrentStamina += regenRate * Time.deltaTime;
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0, maxStamina);

            // Si on récupère assez de stamina, on n'est plus épuisé
            if (_isExhausted && CurrentStamina >= minimumStaminaToRestart)
            {
                _isExhausted = false;
            }

            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }
    }

    public void Spend(float amount)
    {
        if (amount <= 0) return;

        // Si on essaie de dépenser alors qu'on est déjà à sec
        if (_isExhausted || CurrentStamina <= 0)
        {
            OnStaminaEmpty?.Invoke(); // On prévient que c'est vide
            return;
        }

        CurrentStamina -= amount;

        if (CurrentStamina <= 0)
        {
            CurrentStamina = 0;
            _isExhausted = true;
            _regenTimer = regenDelayEmpty;
            OnStaminaEmpty?.Invoke(); // Flash au moment où ça tombe à 0
        }
        else
        {
            _regenTimer = Mathf.Max(_regenTimer, regenDelay);
        }

        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    public bool CanSpend(float amount)
    {
        if (amount <= 0) return true; // On considère que dépenser 0 ou moins est toujours possible
        if (_isExhausted || CurrentStamina <= 0) return false; // Si on est à sec, on ne peut rien dépenser
        return CurrentStamina >= amount; // Sinon, on vérifie si on a assez de stamina
    }
    public bool HasStamina() => !_isExhausted && CurrentStamina > 0;

    public void RequestEmptyFeedback()
    {
        // On déclenche l'événement sans réduire la stamina
        if (CurrentStamina <= 0)
            OnStaminaEmpty?.Invoke();
    }

    public void SetStamania(float stamania) { CurrentStamina = stamania;}

    //public bool CanSpend(float amount)
    //{
    //    return CurrentStamina >= amount;
    //}
}