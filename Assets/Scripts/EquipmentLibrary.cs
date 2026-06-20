using UnityEngine;
using System.Collections.Generic;

public class EquipmentLibrary : MonoBehaviour
{
    public List<EquipmentLibraryItem> content = new List<EquipmentLibraryItem>();
    private Dictionary<ItemData, EquipmentLibraryItem> lookup;

    void Awake()
    {
        lookup = new Dictionary<ItemData, EquipmentLibraryItem>();

        foreach (var item in content)
        {
            if (item.itemData == null)
            {
                Debug.LogWarning("EquipmentLibraryItem has null ItemData");
                continue;
            }

            lookup[item.itemData] = item;
        }
    }

    public EquipmentLibraryItem Get(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("EquipmentLibrary.Get called with NULL ItemData");
            return null;
        }

        if (lookup.TryGetValue(item, out var result))
        {
            return result;
        }

        Debug.LogWarning($"Item {item.itemName} not found in EquipmentLibrary.");
        return null;
    }
}

[System.Serializable]
public class EquipmentLibraryItem
{
    public ItemData itemData;

    [Header("Player In Game")]
    public GameObject itemPrefab;
    public GameObject[] elementsToDisable;

    [Header("Player In Equipment Panel")]
    public GameObject itemPrefabEquipment;
    public GameObject[] elementsToDisableEquipment;
}