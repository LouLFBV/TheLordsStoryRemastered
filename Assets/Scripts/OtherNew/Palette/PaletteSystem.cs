using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletteSystem : MonoBehaviour
{
    public static PaletteSystem instance;

    [Header("Other References")]
    public PaletteInputHandler inputHandler;
    public PaletteSlotManager slotManager;
    public PaletteEquipmentManager equipmentManager;
    public PaletteSaveSystem saveSystem;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void HandlePaletteLogic(PlayerController player)
    {
        inputHandler.HandleInput(player);
    }

    public PaletteSaveData GetSaveData()
    {
        return saveSystem.GetSaveData();
    }

    public void LoadSaveData(PaletteSaveData data)
    {
        saveSystem.LoadSaveData(data);
    }

}

[System.Serializable]
public class PaletteSaveData
{
    public PaletteSlotSave weapon1;
    public PaletteSlotSave weapon2;

    public PaletteSlotSave object1;
    public PaletteSlotSave object2;
}

[System.Serializable]
public class PaletteSlotSave
{
    public string itemID;
    public int count;
    public bool isEquipped;
    public int levelAmelioration;
}

[System.Serializable]
public class PaletteSlot
{
    public ItemData slotItemData;
    public Image SlotImage;
    public Image iconeInput;
    public bool isEquipped = false;
    public TextMeshProUGUI countText;
    public GameObject imageSelected;
    public Slot slotInEquipment;
}