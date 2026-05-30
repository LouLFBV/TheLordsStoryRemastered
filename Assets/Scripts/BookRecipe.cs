using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BookRecipe : MonoBehaviour
{
    [Header("References")]
    public GameObject canvas;
    [SerializeField] public TextMeshProUGUI itemNameText;
    [SerializeField] public Image itemIcon;

    [Header("UI Animation")]
    [SerializeField] private CanvasGroup descriptionCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 5f;

    public Item item;

    void Start()
    {
        item = GetComponent<Item>();
        // On détache le canvas pour qu'il ne meure pas avec le livre
        canvas.transform.SetParent(null, false);
        canvas.SetActive(false);
    }

    public void OpenCanvasRecipeBook()
    {
        if (item == null || item.itemData == null || item.itemData.recipe == null || item.itemData.recipe.craftableItem == null)
            return;

        var craftable = item.itemData.recipe.craftableItem;
        itemNameText.text = craftable.itemName;
        itemIcon.sprite = craftable.visual;

        if (UIManagerSystem.Instance != null)
        {
            // On ajoute le canvas au manager pour la gestion du HUD
            if (!UIManagerSystem.Instance.hudElements.Contains(canvas))
                UIManagerSystem.Instance.hudElements.Add(canvas);

            // C'EST ICI QUE LA MAGIE OPÈRE : Le manager prend le relais du fondu !
            UIManagerSystem.Instance.TriggerRecipeFade(
                canvas,
                descriptionCanvasGroup,
                craftable.itemName,
                craftable.visual,
                fadeDuration,
                displayDuration
            );
        }
    }
}