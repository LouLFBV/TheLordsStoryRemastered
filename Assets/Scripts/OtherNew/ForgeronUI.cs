using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem.Processors;

public class ForgeronUI : MonoBehaviour
{
    [SerializeField] private float upgradePercent = 0.15f;

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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip upgradeSound, destroySound, updatePanel;

    public bool isOpen = false;
    public PlayerController player;
    private ItemData _currentItem;
    private Forgeron _forgeron;

    private void Start()
    {
        if (equipment == null) equipment = EquipmentSystem.instance;
        if (inventory == null) inventory = InventorySystem.instance;
        if (player == null) player = PlayerController.Instance;
        if (_forgeron == null) _forgeron = GetComponent<Forgeron>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!isOpen) return;

        if (player.Input.EquipActionPressed)
        {
            player.Input.UseEquipActionInput();
            if (_currentItem != null) UpgradeItem(_currentItem);
        }
        else if (player.Input.DestroyActionPressed)
        {
            player.Input.UseDestroyActionInput();
            if (_currentItem != null) DestroyItem(_currentItem);
        }
        else if (player.Input.CancelPressed || player.Input.CloseMenuPressed)
        {
            player.Input.UseCancelInput();
            player.Input.UseCloseMenuInput();
            CloseForgeronUI();
        }
    }

    public void OpenForgeonUI()
    {
        isOpen = true;
        upgradePanel.SetActive(false);
        UpdateGoldText();
        UpdateForgeronUI(EquipmentType.Weapon);
        player.StateMachine.ChangeState(PlayerStateType.UI);

        _forgeron.OpenProduitsPanel();
    }

    public void CloseForgeronUI()
    {
        isOpen = false;
        _currentItem = null;

        if (_forgeron.animatorPanelProduits != null)
            _forgeron.animatorPanelProduits.SetBool("PanelIsOpen", false);
        upgradePanel.SetActive(false);
        _forgeron.EndCommerce();
    }

    public void UpdateForgeronUI(EquipmentType equipmentType, bool isFromButton = false)
    {
        if(isFromButton) audioSource.PlayOneShot(updatePanel);
        List<ItemData> allEligibleItems = new List<ItemData>();

        // 1. Items de l'inventaire filtrés
        var inventoryItems = inventory.GetContentEquipment();
        foreach (var item in GetContentForEquipment(inventoryItems, equipmentType))
        {
            if (item.itemData != null && item.itemData.isVendable) allEligibleItems.Add(item.itemData);
        }

        // 2. Items de la Palette
        if (PaletteSystem.instance != null && PaletteSystem.instance.slotManager != null)
        {
            foreach (var slotPalette in PaletteSystem.instance.slotManager.weapons)
            {
                if (slotPalette != null && slotPalette.itemData != null && slotPalette.itemData.equipmentType == equipmentType && slotPalette.itemData.isVendable)
                {
                    allEligibleItems.Add(slotPalette.itemData);
                }
            }
        }

        // 3. Items équipés
        foreach (var slotEquip in equipment.equipmentSlots)
        {
            if (slotEquip != null && slotEquip.item != null && slotEquip.item.equipmentType == equipmentType && slotEquip.item.isVendable)
            {
                allEligibleItems.Add(slotEquip.item);
            }
        }

        // 4. Remplissage des slots
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

    private void FillForgeronSlot(SlotForgeronUI slot, ItemData data)
    {
        slot.itemData = data;
        slot.equipmentIcone.sprite = data.visual;
        slot.equipmentIcone.enabled = true;
    }

    private void ClearForgeronSlot(SlotForgeronUI slot)
    {
        slot.itemData = null;
        slot.equipmentIcone.sprite = null;
        slot.equipmentIcone.enabled = false;
    }

    private List<ItemInInventory> GetContentForEquipment(ItemInInventory[] items, EquipmentType equipmentType)
    {
        List<ItemInInventory> filteredItems = new List<ItemInInventory>();
        foreach (ItemInInventory item in items)
        {
            if (item != null && item.itemData != null && item.itemData.equipmentType == equipmentType)
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
            case 0: iconeLevelItem.sprite = iconeLevel1; break;
            case 1: iconeLevelItem.sprite = iconeLevel2; break;
            case 2:
            case 3:
                iconeLevelItem.sprite = iconeLevel3;
                levelItemUpgradeGameObject.SetActive(false);
                resistanceItemUpgradeGameObject.SetActive(false);
                prixUpgradeItemGameObject.SetActive(false);
                break;
        }

        nameItem.text = itemData.itemName;
        iconeItem.sprite = itemData.visual;
        levelItem.text = (itemData.levelAmelioration + 1).ToString() + "/3";
        levelItemUpgrade.text = (itemData.levelAmelioration + 2).ToString() + "/3";

        // Exemple avec +15% par niveau d'amélioration (multiplier = 0.15f)
        float upgradePercent = 0.15f;

        if (itemData.equipmentType == EquipmentType.Weapon || itemData.equipmentType == EquipmentType.Arrow)
        {
            int bonusDmg = Mathf.Max(1, Mathf.RoundToInt(itemData.attackPoints * upgradePercent));
            resistanceItem.text = "Dégâts : " + itemData.attackPoints;
            resistanceItemUpgrade.text = (itemData.attackPoints + bonusDmg).ToString();
        }
        else if (itemData.handWeaponType == HandWeapon.Bow)
        {
            int bonusRange = Mathf.Max(1, Mathf.RoundToInt(itemData.rangeMax * upgradePercent));
            resistanceItem.text = "Portée : " + itemData.rangeMax;
            resistanceItemUpgrade.text = (itemData.rangeMax + bonusRange).ToString();
        }
        else // Armures
        {
            int bonusArmor = Mathf.Max(1, Mathf.RoundToInt(itemData.armorPoints * upgradePercent));
            resistanceItem.text = "Résistance : " + itemData.armorPoints;
            resistanceItemUpgrade.text = (itemData.armorPoints + bonusArmor).ToString();
        }

        prixUpgradeItem.text = (itemData.prix * (itemData.levelAmelioration + 1)).ToString();
        amountMetal.text = itemData.metalCost.ToString();

        UpdateButtons();
    }

    public void CloseUpgradePanel()
    {
        upgradePanel.SetActive(false);
        _currentItem = null;
    }

    public void UpgradeItem(ItemData itemData)
    {
        if (itemData == null || itemData.levelAmelioration >= 2) return;

        int cost = itemData.prix * (itemData.levelAmelioration + 1);

        if (!player.Wallet.CanSpendGold(cost)) return;
        player.Wallet.SpendGold(cost);

        itemData.levelAmelioration++;

        // Calcul du bonus (au moins +1 de stat garanti, même sur les très petites armes)
        if (itemData.equipmentType == EquipmentType.Weapon || itemData.equipmentType == EquipmentType.Arrow)
        {
            int bonus = Mathf.Max(1, Mathf.RoundToInt(itemData.attackPoints * upgradePercent));
            itemData.attackPoints += bonus;
        }
        else if (itemData.handWeaponType == HandWeapon.Bow)
        {
            int bonus = Mathf.Max(1, Mathf.RoundToInt(itemData.rangeMax * upgradePercent));
            itemData.rangeMax += bonus;
        }
        else // Armures
        {
            int bonus = Mathf.Max(1, Mathf.RoundToInt(itemData.armorPoints * upgradePercent));
            itemData.armorPoints += bonus;
        }

        UpdateUpgradePanel(itemData);
        UpdateGoldText();
        UpdateForgeronUI(itemData.equipmentType);
    }

    public void DestroyItem(ItemData itemData)
    {
        if (itemData == null) return;

        // 1. Don du métal
        inventory.AddItem(metalItemData, itemData.metalCost);

        // 2. Retirer de la Palette si présent
        if (PaletteSystem.instance != null && PaletteSystem.instance.slotManager != null)
        {
            var weaponsList = PaletteSystem.instance.slotManager.weapons;
            for (int i = 0; i < weaponsList.Length; i++)
            {
                if (weaponsList[i] != null && weaponsList[i].itemData == itemData)
                {
                    PaletteSystem.instance.equipmentManager.DesequipWeapon(i + 1);
                    PaletteSystem.instance.slotManager.RefreshAffichage();
                    break;
                }
            }
        }

        // 3. Retirer des slots d'équipement si actuellement porté
        if (equipment != null && itemData.equipmentType != EquipmentType.Weapon)
        {
            bool isCurrentlyWorn = false;
            foreach (var slotEquip in equipment.equipmentSlots)
            {
                if (slotEquip != null && slotEquip.item == itemData)
                {
                    isCurrentlyWorn = true;
                    // On supprime la ligne : slotEquip.item = null; 
                    // Car sinon EquipmentSystem ne trouve plus l'item à éteindre !
                }
            }

            if (equipment.arrowItemInInventory != null && equipment.arrowItemInInventory.itemData == itemData)
            {
                isCurrentlyWorn = true;
                // On supprime la ligne : equipment.arrowItemInInventory.itemData = null;
            }

            if (isCurrentlyWorn)
            {
                // On laisse EquipmentSystem nettoyer les variables ET le visuel.
                // On passe 'false' pour empêcher qu'il soit remis dans l'inventaire (vu qu'on le détruit)
                equipment.DesequipEquipment(itemData.equipmentType, false);
            }
        }

        // 4. Retirer de l'inventaire principal
        inventory.RemoveItem(itemData);

        // 5. Rafraîchir l'UI
        UpdateForgeronUI(itemData.equipmentType);
        if (audioSource != null && destroySound != null) audioSource.PlayOneShot(destroySound);
        upgradePanel.SetActive(false);
        _currentItem = null;
    }

    private void UpdateGoldText()
    {
        if (player == null) player = PlayerController.Instance;
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

        upgradeButton.interactable = _currentItem.levelAmelioration < 2 && player.Wallet.CanSpendGold(_currentItem.prix * (_currentItem.levelAmelioration + 1));
        destroyButton.interactable = true;

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(() => UpgradeItem(_currentItem));
        destroyButton.onClick.RemoveAllListeners();
        destroyButton.onClick.AddListener(() => DestroyItem(_currentItem));
    }
}