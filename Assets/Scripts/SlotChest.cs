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
    private bool _toolipVisible;

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
        _toolipVisible = true;
        if (item != null)
        {
            Tooltip.Instance.Show(item, count);
            Tooltip.Instance.UpdateTooltipPosition(transform.position);
        }

        //itemActionsSystem.OpenActionPanel(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _toolipVisible = false;
        Tooltip.Instance.Hide();
        //itemActionsSystem.CloseActionPanel();
    }

    //public void ClickOnSlot()
    //{
    //    itemActionsSystem.OpenActionPanel(item,isEquipmentSlot/*, transform.position - new Vector3(0, 15, 0)*/);
    //}
}