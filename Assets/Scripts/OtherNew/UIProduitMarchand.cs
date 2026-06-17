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
    private PNJAcheteur _pnjAcheteur;

    public void Setup(ItemData data, Marchand marchand)
    {
        itemData = data;
        _marchandScript = marchand;
    }
    public void SetupPNJAcheteur(ItemData data, PNJAcheteur pnjAchetuer)
    {
        itemData = data;
        _pnjAcheteur = pnjAchetuer;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        actionButtonsGroup.SetActive(true);
        // On dit au marchand que c'est nous l'item sélectionné
        if (_marchandScript != null) _marchandScript.SetCurrentHoveredItem(this);
        if (_pnjAcheteur != null) _pnjAcheteur.SetCurrentHoveredItem(this);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        actionButtonsGroup.SetActive(false);
        // On prévient le marchand qu'on ne le survole plus
        if (_marchandScript != null) _marchandScript.SetCurrentHoveredItem(null);
        if (_pnjAcheteur != null) _pnjAcheteur.SetCurrentHoveredItem(null);
    }
}