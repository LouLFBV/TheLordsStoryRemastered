using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotRecette : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI ingredientName;
    public TextMeshProUGUI ingredientCurrentAmount;
    public TextMeshProUGUI ingredientAmountNeeded;
    public Image ingredientIcon; // Changé en Image pour l'UI canvas

    [Header("Colors")]
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;

    /// <summary>
    /// configure le visuel de l'ingrédient et retourne TRUE si le joueur en a assez.
    /// </summary>
    public bool SetupIngredient(ItemData item, int requiredAmount, int currentAmount)
    {
        if (item == null) return false;

        ingredientName.text = item.itemName;
        ingredientAmountNeeded.text = $"/{requiredAmount}";
        ingredientCurrentAmount.text = currentAmount.ToString();

        if (ingredientIcon != null)
        {
            ingredientIcon.sprite = item.visual;
        }

        // Vérification de la quantité
        bool hasEnough = currentAmount >= requiredAmount;

        // Application de la couleur selon le stock
        Color targetColor = hasEnough ? validColor : invalidColor;
        ingredientCurrentAmount.color = targetColor;

        return hasEnough;
    }
}