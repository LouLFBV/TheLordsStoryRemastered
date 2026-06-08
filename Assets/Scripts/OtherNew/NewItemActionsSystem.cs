using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewItemActionsSystem : MonoBehaviour
{

    [Header("Other Scripts References")]

    [SerializeField] private EquipmentSystem equipment;
    [SerializeField] private PlayerController player;

    [SerializeField] private PaletteSystem palette;

    [Header("Action Panel References")]

    public GameObject actionPanel;

    [SerializeField] private Button useItemButton;

    [SerializeField] private Button equipmentItemButton;

    [SerializeField] private Button dropItemButton;

    [SerializeField] private Button destroyItemButton;

    [SerializeField] private Button desequipmentItemButton;

    [SerializeField] private Transform dropPoint;


    //[SerializeField] private Vector2 positionForEquipment, positionInitiale;

    [HideInInspector] public ItemData itemCurrentlySelected;

    [Header("item Description")]
    [SerializeField] private Image itemLevel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [SerializeField] private TextMeshProUGUI itemDegatsText;
    [SerializeField] private TextMeshProUGUI itemEffetText;
    [SerializeField] private TextMeshProUGUI itemTypeDeResistanceText;
    //private RectTransform actionPanelRect;

    void Awake()
    {
        //actionPanelRect = actionPanel.GetComponent<RectTransform>();
    }
    void Update()
    {
        // On ne vérifie les inputs que si le panneau est affiché et qu'un item est sélectionné
        if (!actionPanel.activeSelf || itemCurrentlySelected == null) return;

        HandleActionInputs();
    }

    private void HandleActionInputs()
    {
        // Utiliser / Consommer
        if (player.Input.UseActionPressed && useItemButton.gameObject.activeInHierarchy)
        {
            UseActionButton();
            player.Input.UseUseActionInput(); // On consomme l'input pour éviter les actions répétées
        }

        // Équiper
        else if (player.Input.EquipActionPressed && equipmentItemButton.gameObject.activeInHierarchy)
        {
            EquipActionButton();
            player.Input.UseEquipActionInput(); // On consomme l'input pour éviter les actions répétées
        }

        // Jeter
        else if (player.Input.DropActionPressed && dropItemButton.gameObject.activeInHierarchy)
        {
            DropActionButton();
            player.Input.UseDropActionInput(); // On consomme l'input pour éviter les actions répétées
        }

        // Détruire
        else if (player.Input.DestroyActionPressed && destroyItemButton.gameObject.activeInHierarchy)
        {
            //DestroyActionButton();
        }

        // Déséquiper
        else if (player.Input.UnequipActionPressed && desequipmentItemButton.gameObject.activeInHierarchy)
        {
            DesequipActionButton();
            player.Input.UseUnequipAction(); // On consomme l'input pour éviter les actions répétées
        }

        // Fermer le panel avec "Cancel" (souvent la touche Echap ou B sur manette)
        else if (player.Input.CancelPressed)
        {
            CloseActionPanel();
        }
    }
    public void OpenActionPanel(ItemData item, bool isEquipped)
    {
        itemCurrentlySelected = item;

        if (item == null)
        {
            actionPanel.SetActive(false);
            return;
        }

        itemDegatsText.gameObject.SetActive(false);
        itemEffetText.gameObject.SetActive(false);
        itemTypeDeResistanceText.gameObject.SetActive(false);

        switch (item.levelAmelioration)
        {
            case 0: itemLevel.sprite = InventorySystem.instance.itemLevel1Icon; break;
            case 1: itemLevel.sprite = InventorySystem.instance.itemLevel2Icon; break;
            case 2: itemLevel.sprite = InventorySystem.instance.itemLevel3Icon; break;
            default: itemLevel.sprite = InventorySystem.instance.itemLevel1Icon; break;
        }

        if (!isEquipped)
        {
            switch (item.itemType)
            {
                case ItemType.Consumable:
                    useItemButton.gameObject.SetActive(true);
                    equipmentItemButton.gameObject.SetActive(!palette.slotManager.ObjectsAreFull(item));
                    dropItemButton.gameObject.SetActive(true);
                    destroyItemButton.gameObject.SetActive(true);
                    //actionPanelRect.anchoredPosition = positionForEquipment;
                    break;
                case ItemType.Equipment:
                    useItemButton.gameObject.SetActive(false);
                    if (item.equipmentType != EquipmentType.Weapon)
                    {
                        equipmentItemButton.gameObject.SetActive(true);
                    }
                    else
                    {
                        equipmentItemButton.gameObject.SetActive(!palette.slotManager.WeaponsAreFull());
                    }
                    dropItemButton.gameObject.SetActive(true);
                    destroyItemButton.gameObject.SetActive(true);
                    //actionPanelRect.anchoredPosition = positionForEquipment;
                    break;
            }
            desequipmentItemButton.gameObject.SetActive(false);
        }
        else
        {
            useItemButton.gameObject.SetActive(false);
            equipmentItemButton.gameObject.SetActive(false);
            dropItemButton.gameObject.SetActive(false);
            destroyItemButton.gameObject.SetActive(false);
            desequipmentItemButton.gameObject.SetActive(!InventorySystem.instance.IsFullEquipment());
        }
        destroyItemButton.gameObject.SetActive(false);
        //actionPanel.transform.position = slotPosition;
        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.description;

        if (item.attackPoints > 0)
        {
            itemDegatsText.text = $"Dégâts : {item.attackPoints}";
            itemDegatsText.gameObject.SetActive(true);

            itemEffetText.text = $"Effet : {item.damageType}";
            itemEffetText.gameObject.SetActive(true);
        }
        if (item.armorPoints > 0)
        {

            itemDegatsText.text = $"Pourcentage de réduction : {item.attackPoints}";
            itemDegatsText.gameObject.SetActive(true);
            itemTypeDeResistanceText.text = $"Type de Résistance : {item.armorType}";
            itemTypeDeResistanceText.gameObject.SetActive(true);
            itemEffetText.gameObject.SetActive(false);
        }
        if (item.healthEffect > 0)
        {
            itemDegatsText.text = $"Capacité de soin : +{item.healthEffect} PV";
            itemDegatsText.gameObject.SetActive(true);
            itemEffetText.gameObject.SetActive(false);
            itemTypeDeResistanceText.gameObject.SetActive(false);
        }
        actionPanel.SetActive(true);
    }

    public void CloseActionPanel()
    {
        actionPanel.SetActive(false);
        itemCurrentlySelected = null;
    }

    public void UseActionButton()
    {
        Debug.Log("Using item: " + itemCurrentlySelected.itemName);
        player.Health.Heal(itemCurrentlySelected.healthEffect);
        InventorySystem.instance.RemoveItem(itemCurrentlySelected);
        if(InventorySystem.instance.GetItemCount(itemCurrentlySelected) <= 0)
            CloseActionPanel();
    }

    public void EquipActionButton()
    {
        Debug.Log("Equiping item: " + itemCurrentlySelected.itemName);
        equipment.EquipAction();
    }

    public void DropActionButton()
    {
        GameObject instantiatedItem = Instantiate(itemCurrentlySelected.prefab);
        instantiatedItem.transform.position = dropPoint.position;
        instantiatedItem.GetComponent<Item>().enableFloating = true;
        DestroyActionButton();
        player.Input.UseDropActionInput();

    }

    public void DestroyActionButton()
    {
        InventorySystem.instance.RemoveItem(itemCurrentlySelected);
        if (InventorySystem.instance.GetItemCount(itemCurrentlySelected) <= 0)
            CloseActionPanel();
        InventorySystem.instance.RefreshContent();
    }

    public void DesequipActionButton()
    {
        if (itemCurrentlySelected.equipmentType == EquipmentType.Weapon)
        {
            if (palette.slotManager.weaponSlots[0].slotItemData == itemCurrentlySelected)
            {
                Debug.Log("Desequipping weapon in slot 1");
                palette.equipmentManager.DesequipWeapon(1);

            }
            else
            {
                Debug.Log("Desequipping weapon in slot 2");
                palette.equipmentManager.DesequipWeapon(2);
            }
            
        }
        else if (itemCurrentlySelected.itemType == ItemType.Consumable)
        {
           if (palette.slotManager.objectSlots[0].slotItemData == itemCurrentlySelected)
                palette.equipmentManager.DesequipObject(1, player);
           else 
                palette.equipmentManager.DesequipObject(2, player);
        }
        else
            equipment.DesequipEquipment(itemCurrentlySelected.equipmentType);

        //CloseActionPanel();
    }
}