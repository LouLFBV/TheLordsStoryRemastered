using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SlotChest : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData item;
    public Image slotFilled; // L'image de fond/contour quand la case est pleine
    public Image itemVisual;
    public TextMeshProUGUI countTexte;
    [HideInInspector] public int count;
    [HideInInspector] public Button button;
    public bool isInChest;  
    public bool isResource; 
    public int arrayIndex;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    // Méthode utilitaire pour changer l'état visuel du slot en une seule ligne
    public void SetSlotState(bool isFilled)
    {
        if (slotFilled != null)
        {
            slotFilled.enabled = isFilled;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null)
        {
            Tooltip.Instance.Show(item, count, true);
            Tooltip.Instance.UpdateTooltipPosition(transform.position);
            ChestInventory.Instance.SetCurrentSlot(this);
        }
        else
            Tooltip.Instance.Hide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tooltip.Instance.Hide();
        ChestInventory.Instance.SetCurrentSlot(null);
    }

}