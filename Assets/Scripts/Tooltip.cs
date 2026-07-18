using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI itemName, itemDescription, itemStock;
    [SerializeField] private Vector3 offset = new Vector3(80, -80, 0); // Ajuste l'offset selon la taille de tes slots
    [SerializeField] private GameObject chestButton, inventoryButton;

    private RectTransform _rectTransform;
    public static Tooltip Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // On récupère le RectTransform pour manipuler la position UI proprement
        _rectTransform = tooltipPanel.GetComponent<RectTransform>();
    }

    public void Show(ItemData itemData, int stock, bool isInChest)
    {
        SetText(itemData, stock);
        tooltipPanel.SetActive(true);
        chestButton.SetActive(isInChest);
        inventoryButton.SetActive(!isInChest && itemData.isVendable);
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }

    public void SetText(ItemData itemData, int stock)
    {
        if (itemData != null)
        {
            itemName.text = itemData.itemName;
            itemDescription.text = itemData.description;
            itemStock.text = $"Stock : {stock}/{itemData.maxStack}";
        }
    }

    public void UpdateTooltipPosition(Vector3 slotPosition)
    {
        if (_rectTransform != null)
        {
            _rectTransform.position = slotPosition + offset;
        }
    }
}