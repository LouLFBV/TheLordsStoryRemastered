using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BookRecipe : MonoBehaviour
{
    [Header("References UI")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private CanvasGroup descriptionCanvasGroup;

    [Header("UI Animation Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 5f;

    [Header("Item Reference")]
    public Item item;

    private void Start()
    {
        if (item == null) item = GetComponent<Item>();

        if (canvas != null)
        {
            // 1. On détache IMMÉDIATEMENT le canvas du livre
            // On le place sous PopupParent pour qu'il ne soit PAS détruit quand le livre est ramassé !
            if (PopupParent.Instance != null)
            {
                canvas.transform.SetParent(PopupParent.Instance.parentItem, false);
            }
            else
            {
                canvas.transform.SetParent(null, false);
            }

            canvas.SetActive(false);
        }
    }

    public void OpenCanvasRecipeBook()
    {
        if (item == null || item.itemData == null || item.itemData.recipe == null || item.itemData.recipe.craftableItem == null)
            return;

        var craftable = item.itemData.recipe.craftableItem;

        // 2. Remplissage des textes et visuels
        if (itemNameText != null) itemNameText.text = craftable.itemName;
        if (itemIcon != null) itemIcon.sprite = craftable.visual;

        // 3. On passe le relais au UIManagerSystem (Persistent) pour gérer le Canvas et la Coroutine
        if (UIManagerSystem.Instance != null)
        {
            UIManagerSystem.Instance.TriggerRecipeFade(
                canvas,
                descriptionCanvasGroup,
                fadeDuration,
                displayDuration
            );
        }
    }
}