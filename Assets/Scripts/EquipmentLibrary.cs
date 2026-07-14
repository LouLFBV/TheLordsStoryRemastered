using UnityEngine;
using System.Collections.Generic;

public class EquipmentLibrary : MonoBehaviour
{
    public List<EquipmentLibraryItem> content = new List<EquipmentLibraryItem>();

    //  On change la clé en 'string' (qui correspondra à l'itemID)
    private Dictionary<string, EquipmentLibraryItem> lookup;

    void Awake()
    {
        lookup = new Dictionary<string, EquipmentLibraryItem>();

        foreach (var item in content)
        {
            if (item.itemData == null)
            {
                Debug.LogWarning("EquipmentLibraryItem has null ItemData");
                continue;
            }

            if (string.IsNullOrEmpty(item.itemData.itemID))
            {
                Debug.LogWarning($"L'objet {item.itemData.name} n'a pas d'itemID !");
                continue;
            }

            // On enregistre la librairie en utilisant l'ID comme clé
            lookup[item.itemData.itemID] = item;
        }
    }

    public EquipmentLibraryItem Get(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("EquipmentLibrary.Get called with NULL ItemData");
            return null;
        }

        if (string.IsNullOrEmpty(item.itemID))
        {
            Debug.LogWarning($"EquipmentLibrary.Get : L'item {item.name} n'a pas d'itemID !");
            return null;
        }

        //  On cherche dans le dictionnaire via l'ID de l'item (clone ou original, l'ID sera le même !)
        if (lookup.TryGetValue(item.itemID, out var result))
        {
            return result;
        }

        Debug.LogWarning($"Item {item.itemName} (ID: {item.itemID}) not found in EquipmentLibrary.");
        return null;
    }
}

[System.Serializable]
public class EquipmentLibraryItem
{
    public ItemData itemData;

    [Header("Player In Game")]
    public GameObject itemPrefab;
    public GameObject[] bobyPartToEnable;
    public GameObject[] elementsToDisable;

    [Header("Player In Equipment Panel")]
    public GameObject itemPrefabEquipment;
    public GameObject[] bobyPartToEnableEquipment;
    public GameObject[] elementsToDisableEquipment;
}