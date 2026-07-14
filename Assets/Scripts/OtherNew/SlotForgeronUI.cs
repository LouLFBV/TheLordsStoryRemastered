using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotForgeronUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData itemData;
    public Image equipmentIcone;

    [SerializeField] private ForgeronUI forgeronUI;

    public void OnClick()
    {
        if (itemData == null)
        {
            Debug.Log("Aucun item dans ce slot du forgeron UI");
            return;
        }
        Debug.Log("Slot du forgeron UI cliqué pour l'item : " + itemData.name);
        forgeronUI.UpdateUpgradePanel(itemData);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemData == null)
        {
            Debug.Log("Aucun item dans ce slot du forgeron UI");
            return;
        }
        forgeronUI.UpdateUpgradePanel(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemData == null)
        {
            Debug.Log("Aucun item dans ce slot du forgeron UI");
            return;
        }
        forgeronUI.CloseUpgradePanel();
    }
}
