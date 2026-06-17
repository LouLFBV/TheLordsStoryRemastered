using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    public static MapManager instance;

    [SerializeField] private Transform iconeMapTransform;
    [SerializeField] private ListeMorceauxDeMap[] allListMap = new ListeMorceauxDeMap[5];
    [SerializeField] private GameObject iconeIfNoMap;

    [SerializeField] private GameObject legendeGameObject;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // SECURITE SI DOUBLON
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Nouvelle scène chargée : " + scene.name);
        SetMapActive(scene.name);
        ActiveLegendeGameObject();
    }

    private void ActiveLegendeGameObject()
    {
        if (legendeGameObject != null)
        {
            legendeGameObject.SetActive(SceneManager.GetActiveScene().name == "Jeu");
            Debug.Log("Legende GameObject actif : " + legendeGameObject.activeSelf);
        }
        else
        {
            Debug.LogWarning("Legende GameObject n'est pas assigné dans l'inspecteur.");
        }
    }
    private void SetMapActive(string sceneName)
    {
        bool found = false;
        bool hasActiveMorceaux = false;

        foreach (var map in allListMap)
        {
            bool isCurrent = (map.nomDeScene == sceneName);

            if (isCurrent)
            {
                found = true;
                if (map.morceauxDeMap != null && map.morceauxDeMap.Count > 0)
                {
                    hasActiveMorceaux = true;
                }
            }
            foreach (var morceau in map.morceauxDeMap)
            {
                if (morceau != null)
                    morceau.SetActive(isCurrent);
            }
        }
        if (iconeIfNoMap != null)
        {
            bool shouldShow = !found || !hasActiveMorceaux;
            iconeIfNoMap.SetActive(shouldShow);
        }
    }


    public void AddIconeMap(ItemData itemData)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        foreach (var map in allListMap)
        {
            if (map.nomDeScene == sceneName)
            {
                if (map.morceauxDeMap.Exists(m =>
                    m != null &&
                    m.TryGetComponent<MapIcon>(out var existingIcon) &&
                    existingIcon.sourceItem == itemData))
                {
                    return; // déjà possédée
                }

                GameObject newIcon = Instantiate(
                    itemData.iconeMap,
                    iconeMapTransform.position,
                    Quaternion.identity,
                    iconeMapTransform
                );

                if (!newIcon.TryGetComponent<MapIcon>(out var icon))
                {
                    Debug.LogError("Map icon prefab missing MapIcon component");
                    Destroy(newIcon);
                    return;
                }

                icon.sourceItem = itemData;
                map.morceauxDeMap.Add(newIcon);
                iconeIfNoMap.SetActive(false);
                return;
            }
        }
    }


    public MapSaveData GetSaveData()
    {
        MapSaveData data = new MapSaveData();

        foreach (var map in allListMap)
        {
            MapSceneSaveData sceneData = new MapSceneSaveData
            {
                sceneName = map.nomDeScene
            };

            foreach (var morceau in map.morceauxDeMap)
            {
                if (morceau == null) continue;

                if (morceau.TryGetComponent<MapIcon>(out var icon))
                {
                    sceneData.mapItemIDs.Add(icon.sourceItem.itemID);
                }
            }

            data.scenes.Add(sceneData);
        }

        return data;
    }

    public void LoadSaveData(MapSaveData data)
    {
        if (data == null) return;

        // Nettoyage
        foreach (var map in allListMap)
        {
            foreach (var morceau in map.morceauxDeMap)
            {
                if (morceau != null)
                    Destroy(morceau);
            }
            map.morceauxDeMap.Clear();
        }

        foreach (var sceneData in data.scenes)
        {
            foreach (string itemID in sceneData.mapItemIDs)
            {
                ItemData item = ItemDataDatabase.Instance.GetItemByID(itemID);
                if (item == null || item.iconeMap == null) continue;

                GameObject newIcon = Instantiate(
                    item.iconeMap,
                    iconeMapTransform.position,
                    Quaternion.identity,
                    iconeMapTransform
                );

                var icon = newIcon.GetComponent<MapIcon>();
                if (icon != null)
                {
                    icon.sourceItem = item;
                }



                var map = System.Array.Find(allListMap, m => m.nomDeScene == sceneData.sceneName);
                if (map == null)
                {
                    Debug.LogWarning($"Map scene not found: {sceneData.sceneName}");
                    Destroy(newIcon);
                    continue;
                }
                map.morceauxDeMap.Add(newIcon);
            }
        }

        SetMapActive(SceneManager.GetActiveScene().name);
    }

}

[System.Serializable]
public class ListeMorceauxDeMap
{
    public string nomDeScene;
    public List<GameObject> morceauxDeMap;
}

[System.Serializable]
public class MapSaveData
{
    public List<MapSceneSaveData> scenes = new();
}

[System.Serializable]
public class MapSceneSaveData
{
    public string sceneName;
    public List<string> mapItemIDs = new();
}
