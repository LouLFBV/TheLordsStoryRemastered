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
    public TextMeshProUGUI purchaseAmountText;
    public GameObject actionButtonsGroup;
    public Button buyButton;

    [Header("Fill Stock")]
    public TextMeshProUGUI priceFillStock;
    public Button fillStockButton;
    public Image levelIcon;

    private Marchand _marchandScript;
    private PNJAcheteur _pnjAcheteur;

    public void Setup(ItemData data, Marchand marchand)
    {
        itemData = data;
        _marchandScript = marchand;
        if (data.PurchaseAmount != 1) purchaseAmountText.text = $"x {itemData.PurchaseAmount}";
        else purchaseAmountText.text = "";
    }
    public void SetupPNJAcheteur(ItemData data, PNJAcheteur pnjAchetuer)
    {
        itemData = data;
        _pnjAcheteur = pnjAchetuer;
        if (levelIcon != null && data.equipmentType != EquipmentType.None)
        {
            switch(itemData.levelAmelioration)
            {
                case 0:
                    levelIcon.sprite = InventorySystem.instance.itemLevel1IconWhite;
                    break;
                case 1:
                    levelIcon.sprite = InventorySystem.instance.itemLevel2IconWhite;
                    break;
                case 2:
                    levelIcon.sprite = InventorySystem.instance.itemLevel3IconWhite;
                    break;
                default:
                    levelIcon.sprite = InventorySystem.instance.itemLevel1IconWhite;
                    break;
            }
        }
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