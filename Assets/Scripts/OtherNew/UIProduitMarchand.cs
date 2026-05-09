using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Obligatoire pour les interfaces de pointeur

public class UIProduitMarchand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI nameItem;
    public Image iconeItem;
    public TextMeshProUGUI priceItem;
    public TextMeshProUGUI stockItemInInventory;
    public TextMeshProUGUI priceFillStock;
    public Button buyButton;

    // Ajoute une référence au groupe qui contient "Acheter" et "Remplir"
    [Header("Hover Elements")]
    public GameObject actionButtonsGroup;

    private void Awake()
    {
        // On cache les boutons au départ
        if (actionButtonsGroup != null)
            actionButtonsGroup.SetActive(false);
    }

    // Appelé quand la souris (ou ton curseur manette) arrive sur le slot
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (actionButtonsGroup != null)
            actionButtonsGroup.SetActive(true);
    }

    // Appelé quand le curseur sort du slot
    public void OnPointerExit(PointerEventData eventData)
    {
        if (actionButtonsGroup != null)
            actionButtonsGroup.SetActive(false);
    }
}