using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class UIProduitMarchand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData itemData; // À assigner lors du Instantiate dans Marchand
    public TextMeshProUGUI nameItem;
    public Image iconeItem;
    public TextMeshProUGUI priceItem;
    public TextMeshProUGUI stockItemInInventory;
    public TextMeshProUGUI priceFillStock;
    public Button buyButton;
    public Button fillStockButton;
    public GameObject actionButtonsGroup;

    private Marchand _marchandScript;

    public void Setup(ItemData data, Marchand marchand)
    {
        itemData = data;
        _marchandScript = marchand;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        actionButtonsGroup.SetActive(true);
        // On dit au marchand que c'est nous l'item sélectionné
        _marchandScript.SetCurrentHoveredItem(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        actionButtonsGroup.SetActive(false);
        // On prévient le marchand qu'on ne le survole plus
        _marchandScript.SetCurrentHoveredItem(null);
    }
}