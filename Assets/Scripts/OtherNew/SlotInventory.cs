using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SlotInventory : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData item;
    public Image slotFilled; // L'image de fond/contour quand la case est pleine
    public Image itemVisual;
    public TextMeshProUGUI countTexte;
    [HideInInspector] public int count;
    [HideInInspector] public Button button;
    public bool isResource;
    public int arrayIndex;

    private bool _isHovered = false;
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
            _isHovered = true;
            Tooltip.Instance.Show(item, count, false);
            Tooltip.Instance.UpdateTooltipPosition(transform.position);
        }
        else
            Tooltip.Instance.Hide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        Tooltip.Instance.Hide();
    }

    private void Update()
    {
        // Si la souris est sur ce slot, qu'il y a un item, et qu'on appuie sur Jeter
        if (_isHovered && item != null && PlayerController.Instance.Input.DropActionPressed)
        {
            // Code pour instancier l'objet au sol...
            GameObject instantiatedItem = Instantiate(item.prefab);
            instantiatedItem.transform.position = PlayerController.Instance.dropPoint.position;
            instantiatedItem.GetComponent<Item>().enableFloating = true;
            InventorySystem.instance.RemoveItem(item);
            PlayerController.Instance.Input.UseDropActionInput();

            // Update le tooltip ou le cache si le slot devient vide
            if (item == null) Tooltip.Instance.Hide();
        }
    }
}