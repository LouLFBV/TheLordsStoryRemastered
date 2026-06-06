using UnityEngine;

public class PoiseSystem : MonoBehaviour
{
    public bool IsBroken => CurrentPoise <= 0;
    [SerializeField] private float maxPoise = 30f;
    [SerializeField] private float poiseRecoveryRate = 10f;
    [SerializeField] private float poiseResetDelay = 20f;

    public float CurrentPoise { get; private set; }

    private float resetTimer;

    private void Awake()
    {
        CurrentPoise = maxPoise;
    }

    private void Update()
    {
        if (resetTimer > 0)
        {
            resetTimer -= Time.deltaTime;
            return;
        }

        if (CurrentPoise < maxPoise)
        {
            CurrentPoise += poiseRecoveryRate * Time.deltaTime;
            CurrentPoise = Mathf.Clamp(CurrentPoise, 0, maxPoise);
        }
    }

    public bool ApplyPoiseDamage(float amount)
    {
        Debug.Log($"Poise avant : {CurrentPoise}, dégâts de poise : {amount}");
        CurrentPoise -= amount;
        resetTimer = poiseResetDelay;

        if (CurrentPoise <= 0)
        {
            CurrentPoise = maxPoise;
            return true; // Stagger
        }

        return false;
    }

    public void ResetPoise()
    {
        CurrentPoise = maxPoise;
        resetTimer = 0f;
    }
}