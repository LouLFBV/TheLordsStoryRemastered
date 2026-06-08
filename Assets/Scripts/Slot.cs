using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData item;
    public Image itemVisual;
    public Image itemTypeVisual;
    public TextMeshProUGUI countTexte;
    [SerializeField] private bool isEquipmentSlot;

    [SerializeField]
    //private ItemActionsSystem itemActionsSystem;
    private NewItemActionsSystem itemActionsSystem;
    public void OnPointerEnter(PointerEventData eventData)
    {
        //if (item != null)
        //    TooltipSystem.instance.Show(item.description, item.itemName);

        itemActionsSystem.OpenActionPanel(item, isEquipmentSlot);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //TooltipSystem.instance.Hide();
        itemActionsSystem.CloseActionPanel();
    }

    //public void ClickOnSlot()
    //{
    //    itemActionsSystem.OpenActionPanel(item,isEquipmentSlot/*, transform.position - new Vector3(0, 15, 0)*/);
    //}
}