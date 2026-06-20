using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ForgeronUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EquipmentSystem equipment;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private ItemData metalItemData;

    [Header("UI Elements")]
    [SerializeField] private GameObject forgeronUIPanel;
    [SerializeField] private List<SlotForgeronUI> slotForgeronUIs;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Icones level")]
    [SerializeField] private Sprite iconeLevel1;
    [SerializeField] private Sprite iconeLevel2;
    [SerializeField] private Sprite iconeLevel3;

    [Header("Upgrade Panel")]
    [SerializeField] private GameObject upgradePanel;

    [SerializeField] private Image iconeLevelItem;
    [SerializeField] private TextMeshProUGUI nameItem;

    [SerializeField] private TextMeshProUGUI levelItem;
    [SerializeField] private TextMeshProUGUI levelItemUpgrade;
    [SerializeField] private GameObject levelItemUpgradeGameObject;

    [SerializeField] private TextMeshProUGUI resistanceItem;
    [SerializeField] private TextMeshProUGUI resistanceItemUpgrade;
    [SerializeField] private GameObject resistanceItemUpgradeGameObject;

    [SerializeField] private TextMeshProUGUI prixUpgradeItem;
    [SerializeField] private GameObject prixUpgradeItemGameObject;


    [SerializeField] private TextMeshProUGUI amountMetal;
    [SerializeField] private Image iconeItem;

    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button destroyButton;

    public bool isOpen = false;
    public PlayerController player;
    private ItemData _currentItem;
    private Forgeron _forgeron;

    private void Start()
    {
        if (equipment == null)
            equipment = EquipmentSystem.instance;
        if (inventory == null)
            inventory = InventorySystem.instance;
        if(player == null)
            player = PlayerController.Instance;
        if (_forgeron == null)
            _forgeron = GetComponent<Forgeron>();
    }

    private void Update()
    {
        if (!isOpen) return;

        if (player.Input.EquipActionPressed)
        {
            UpgradeItem(_currentItem);
            player.Input. UseEquipActionInput();
        }
        else if (player.Input.DestroyActionPressed)
        {
            DestroyItem(_currentItem);
        }
        else if (player.Input.CancelPressed || player.Input.CloseMenuPressed)
        {
            Debug.Log("Fermeture du forgeron UI via input Cancel ou Menu");
            CloseForgeronUI();
            player.Input.UseCancelInput();
            player.Input.UseCloseMenuInput();
        }
    }
    public void OpenForgeonUI()
    {
        isOpen = true;
        forgeronUIPanel.SetActive(true);
        upgradePanel.SetActive(false);
        UpdateGoldText();
        UpdateForgeronUI(EquipmentType.Weapon);
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);

    }

    public void CloseForgeronUI()
    {
        isOpen = false;
        _currentItem = null;
        forgeronUIPanel.SetActive(false);
        upgradePanel.SetActive(false);
        _forgeron.EndCommerce();

    }

    public void UpdateForgeronUI(EquipmentType equipmentType)
    {
        Debug.Log($"Mise à jour de l'UI du forgeron : {equipmentType}");

        // 1. On prépare une liste globale qui va contenir TOUT ce qui est éligible à la forge
        List<ItemData> allEligibleItems = new List<ItemData>();

        // AJOUTER : Les items de l'inventaire filtrés par type
        var inventoryItems = inventory.GetContentEquipment();
        foreach (var item in GetContentForEquipment(inventoryItems, equipmentType))
        {
            if (item.itemData != null) allEligibleItems.Add(item.itemData);
        }

        foreach (var slotPalette in PaletteSystem.instance.slotManager.weapons)
        {
            if (slotPalette != null && slotPalette.itemData != null && slotPalette.itemData.equipmentType == equipmentType)
            {
                allEligibleItems.Add(slotPalette.itemData);
            }
        }

        foreach (var slotPalette in EquipmentSystem.instance.equipmentSlots)
        {
            if (slotPalette != null && slotPalette.item != null && slotPalette.item.equipmentType == equipmentType)
            {
                allEligibleItems.Add(slotPalette.item);
            }
        }


        // AJOUTER : La flèche équipée (si on cherche des armes à distance ou munitions)
        var arrowItem = EquipmentSystem.instance.arrowItemInInventory;
        if (arrowItem != null && arrowItem.itemData != null && arrowItem.itemData.equipmentType == equipmentType)
        {
            allEligibleItems.Add(arrowItem.itemData);
        }


        // 2. On nettoie l'UI
        CleanForgeronUI();

        // 3. On remplit les slots UI à partir de notre liste consolidée
        for (int i = 0; i < slotForgeronUIs.Count; i++)
        {
            if (i < allEligibleItems.Count)
            {
                FillForgeronSlot(slotForgeronUIs[i], allEligibleItems[i]);
            }
            else
            {
                ClearForgeronSlot(slotForgeronUIs[i]);
            }
        }
    }

    // Méthode helper pour remplir un slot (évite la répétition)
    private void FillForgeronSlot(SlotForgeronUI slot, ItemData data)
    {
        slot.itemData = data;
        slot.equipmentIcone.sprite = data.visual;
        slot.equipmentIcone.enabled = true;
    }

    // Méthode helper pour vider un slot
    private void ClearForgeronSlot(SlotForgeronUI slot)
    {
        slot.itemData = null;
        slot.equipmentIcone.sprite = null;
        slot.equipmentIcone.enabled = false;
    }
    private void CleanForgeronUI()
    {
        foreach (var slot in slotForgeronUIs)
        {
            slot.itemData = null;
            slot.equipmentIcone.sprite = null;
        }
    }
    private List<ItemInInventory> GetContentForEquipment(ItemInInventory[] items, EquipmentType equipmentType)
    {
        List<ItemInInventory> filteredItems = new List<ItemInInventory>();

        foreach (ItemInInventory item in items)
        {
            if (item == null || item.itemData == null)
                continue;

            if (item.itemData.equipmentType == equipmentType)
            {
                filteredItems.Add(item);
            }
        }
        return filteredItems;
    }

    public void UpdateUpgradePanel(ItemData itemData)
    {
        if (itemData == null)
        {
            upgradePanel.SetActive(false);
            return;
        }
        upgradePanel.SetActive(true);
        _currentItem = itemData;
        levelItemUpgradeGameObject.SetActive(true);
        resistanceItemUpgradeGameObject.SetActive(true);
        prixUpgradeItemGameObject.SetActive(true);

        switch (itemData.levelAmelioration)
        {
            case 0:
                iconeLevelItem.sprite = iconeLevel1;
                break;
            case 1:
                iconeLevelItem.sprite = iconeLevel2;
                break;
            case 2:
                iconeLevelItem.sprite = iconeLevel3;
                levelItemUpgradeGameObject.SetActive(false);
                resistanceItemUpgradeGameObject.SetActive(false);
                prixUpgradeItemGameObject.SetActive(false);
                break;
            case 3:
                iconeLevelItem.sprite = iconeLevel3;
                levelItemUpgradeGameObject.SetActive(false);
                resistanceItemUpgradeGameObject.SetActive(false);
                prixUpgradeItemGameObject.SetActive(false);
                break;
            default:
                Debug.LogWarning("Niveau d'amélioration inattendu : " + itemData.levelAmelioration);
                break;
        }

        nameItem.text = itemData.itemName;
        iconeItem.sprite = itemData.visual;
        levelItem.text = (itemData.levelAmelioration+1).ToString()+"/3";
        levelItemUpgrade.text = (itemData.levelAmelioration+2).ToString() + "/3";

        if (itemData.equipmentType == EquipmentType.Weapon || itemData.equipmentType == EquipmentType.Arrow)
        {
            resistanceItem.text = "Degats : " + itemData.attackPoints.ToString();
            resistanceItemUpgrade.text = (itemData.attackPoints + 10).ToString();
        }
        else if (itemData.handWeaponType == HandWeapon.Bow)
        {
            resistanceItem.text = "Portee : " + itemData.rangeMax.ToString();
            resistanceItemUpgrade.text = (itemData.rangeMax + 5).ToString();
        }
        else
        {
            resistanceItem.text = "Resistance : " + itemData.armorPoints.ToString();
            resistanceItemUpgrade.text = (itemData.armorPoints + 10).ToString();
        }

        prixUpgradeItem.text = (itemData.prix * (itemData.levelAmelioration + 1)).ToString();
        amountMetal.text = itemData.metalCost.ToString();

        UpdateButtons();
    }

    public void UpgradeItem(ItemData itemData)
    {
        Debug.Log("Tentative d'amélioration de l'item : " + (itemData != null ? itemData.itemName : "null"));
        if (itemData == null)
        {
            Debug.LogWarning("Aucun item sélectionné pour l'amélioration");
            return;
        }
        if (itemData.levelAmelioration >= 2)
        {
            Debug.LogWarning("L'item est déjà au niveau maximum d'amélioration");
            return;
        }
        if (player.Wallet.CanSpendGold(itemData.prix * (itemData.levelAmelioration + 1)))
        {
            player.Wallet.SpendGold(itemData.prix * (itemData.levelAmelioration + 1));
        }
        else
        {
            Debug.LogWarning("Pas assez de gold pour améliorer cet item");
            return;
        }
        itemData.levelAmelioration++;
        if (itemData.equipmentType != EquipmentType.Weapon)
            itemData.armorPoints += 10; 
        else
            itemData.attackPoints += 10; 
        UpdateUpgradePanel(itemData);
        UpdateGoldText();
    }

    public void DestroyItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Aucun item sélectionné pour la destruction");
            return;
        }

        // 1. Donner les composants de recyclage (métal)
        for (int i = 0; i < itemData.metalCost; i++)
            inventory.AddItem(metalItemData);

        // 2. Retirer de l'inventaire principal
        inventory.RemoveItem(itemData);

        // 3. Retirer de la Palette de raccourcis si présent dedans
        if (PaletteSystem.instance != null && PaletteSystem.instance.slotManager != null)
        {
            bool aEteRetireDeLaPalette = false;

            foreach (var slotPalette in PaletteSystem.instance.slotManager.weapons)
            {
                if (slotPalette != null && slotPalette.itemData == itemData)
                {
                    slotPalette.itemData = null;
                    aEteRetireDeLaPalette = true;
                }
            }

            if (aEteRetireDeLaPalette)
            {
                PaletteSystem.instance.slotManager.RefreshAffichage();
            }
        }

        // 4. Retirer des slots d'équipement (Armures/Armes portées)
        if (EquipmentSystem.instance != null)
        {
            EquipmentSystem.instance.DesequipEquipment(itemData.equipmentType);

            // Sécurité : Au cas où DesequipEquipment ne vide pas le slot de données lui-même
            foreach (var slotEquip in EquipmentSystem.instance.equipmentSlots)
            {
                if (slotEquip != null && slotEquip.item == itemData)
                {
                    slotEquip.item = null;
                }
            }

            // Vérification pour les flèches équipées
            if (EquipmentSystem.instance.arrowItemInInventory != null && EquipmentSystem.instance.arrowItemInInventory.itemData == itemData)
            {
                EquipmentSystem.instance.arrowItemInInventory.itemData = null;
            }
        }

        // 6. Rafraîchir l'UI du forgeron
        UpdateForgeronUI(itemData.equipmentType);
        upgradePanel.SetActive(false);
        _currentItem = null;
    }

    private void UpdateGoldText()
    {
        if (player == null)
            player = PlayerController.Instance;
        goldText.text = player.Wallet.GetGoldAmount().ToString();
    }

    private void UpdateButtons()
    {
        if (_currentItem == null)
        {
            upgradeButton.interactable = false;
            destroyButton.interactable = false;
            return;
        }
        upgradeButton.interactable = _currentItem.levelAmelioration < 3 && player.Wallet.CanSpendGold(_currentItem.prix * (_currentItem.levelAmelioration + 1));
        destroyButton.interactable = true;

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(() => UpgradeItem(_currentItem));
        destroyButton.onClick.RemoveAllListeners();
        destroyButton.onClick.AddListener(() => DestroyItem(_currentItem));
    }
}
