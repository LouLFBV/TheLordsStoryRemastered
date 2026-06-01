using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SlotRecipe : MonoBehaviour
{
    public ItemData itemData { get; private set; }

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton; // Bouton sur tout le slot pour le sélectionner

    public void Initialize(ItemData data, Action<ItemData> onSelectedCallback)
    {
        itemData = data;

        if (nameText != null) nameText.text = data.itemName;
        if (iconImage != null) iconImage.sprite = data.visual;

        // Quand on clique sur ce slot, on prévient la table de craft qu'on a été sélectionné
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelectedCallback?.Invoke(itemData));
        }
    }
}