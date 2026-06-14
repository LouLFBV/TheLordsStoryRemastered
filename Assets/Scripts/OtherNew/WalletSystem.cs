using UnityEngine;
using TMPro;

public class WalletSystem : MonoBehaviour
{
    [Header("Gold Settings")]
    [SerializeField] private int goldAmount;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip goldSound;

    private void Start()
    {
        UpdateGoldUI();
    }

    public int GetGoldAmount() => goldAmount;
    public void SetGoldAmount(int newGoldAmount) 
    {
        goldAmount = newGoldAmount; 
        UpdateGoldUI();
    }

    public void AddGold(int amount)
    {
        goldAmount += amount;
        if (audioSource && goldSound)
            audioSource.PlayOneShot(goldSound);

        UpdateGoldUI();
    }

    public bool CanSpendGold(int amount) => goldAmount >= amount;

    public bool SpendGold(int amount)
    {
        if (goldAmount >= amount)
        {
            goldAmount -= amount;
            UpdateGoldUI();
            return true;
        }
        return false; // Pas assez d'argent
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = goldAmount.ToString();
    }
}