using System.Collections.Generic;
using UnityEngine;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance;

    private HashSet<string> collectedObjects = new();

    private HashSet<string> activedBuildings = new();

    public bool IsWorldStateLoaded { get; private set; }

    public event System.Action OnWorldStateLoaded;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsWorldStateLoaded = true;
    }

    public WorldStateSaveData GetSaveData()
    {
        return new WorldStateSaveData
        {
            collectedObjectIDs = new List<string>(collectedObjects),
            activedBuildings = new List<string>(activedBuildings)
        };
    }

    public void LoadSaveData(WorldStateSaveData data)
    {
        collectedObjects = new HashSet<string>(data.collectedObjectIDs);
        activedBuildings = new HashSet<string>(data.activedBuildings);

        IsWorldStateLoaded = true;
        Debug.Log($"<color=green>[WorldStateManager] Loaded {collectedObjects.Count} collected objects.</color>");
        foreach (var id in collectedObjects)
        {
            Debug.Log($"<color=green> - Collected Object ID: {id}</color>");
        }
        OnWorldStateLoaded?.Invoke();
    }

    public void RegisterCollectedObject(string id)
    {
        collectedObjects.Add(id);
    }

    public void RegisterActivedBuilding(string id)
    {
        activedBuildings.Add(id);
    }

    public bool IsCollected(string id)
    {
        return collectedObjects.Contains(id);
    }

    public bool IsActived(string id)
    {
        return activedBuildings.Contains(id);
    }
    public void Subscribe(System.Action callback)
    {
        OnWorldStateLoaded += callback;

        //  Replay automatique si déjà chargé
        if (IsWorldStateLoaded)
        {
            callback.Invoke();
        }
    }

    public void Unsubscribe(System.Action callback)
    {
        OnWorldStateLoaded -= callback;
    }

}


[System.Serializable]
public class WorldStateSaveData
{
    public List<string> collectedObjectIDs = new List<string>(); 
    public List<string> activedBuildings = new List<string>();
}