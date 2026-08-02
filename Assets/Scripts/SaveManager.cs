using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private int currentSlot = 1;

    private string GetSavePath(int slot)
    {
        return Application.persistentDataPath + $"/save_slot_{slot}.json";
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentSlot(int slot)
    {
        currentSlot = slot;
    }

    public void SaveGame()
    {
        string path = GetSavePath(currentSlot);

        SaveData data = new SaveData();
        data.playerController = PlayerController.Instance.GetSaveData();
        data.inventory = InventorySystem.instance.GetSaveData();
        data.palette = PaletteSystem.instance.GetSaveData();
        data.world = WorldStateManager.Instance.GetSaveData();
        data.sceneName = SceneManager.GetActiveScene().name;
        data.equipment = EquipmentSystem.instance.GetSaveData();
        data.map = MapManager.instance.GetSaveData();
        data.quests = NewQuestManager.instance.GetSaveData();
        data.questLog = NewQuestLog.instance.GetSaveData();
        data.progression = ProgressionManager.Instance.GetSaveData();

        if (RecipeDatabase.Instance != null)
            data.recipes = RecipeDatabase.Instance.GetSaveData();

        if (ChestInventory.Instance != null)
            data.chestInventory = ChestInventory.Instance.GetSaveData();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log($"Game Saved in slot {currentSlot}");
    }

    public void LoadGame(int slot)
    {
        string path = GetSavePath(slot);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save file in slot {slot}");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        currentSlot = slot;

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        StartCoroutine(LoadRoutine(data));
    }

    private IEnumerator LoadRoutine(SaveData data)
    {
        // 1. On charge d'abord le Bootstrap (qui contient tes managers globaux)
        yield return SceneManager.LoadSceneAsync("Bootstrap");

        //  NOUVEAU & CRITIQUE : On charge la progression TOUT DE SUITE.
        // Comme ça, le ProgressionManager est à jour AVANT que la scène de jeu ne se lance.
        if (data.progression != null)
            ProgressionManager.Instance.LoadSaveData(data.progression);

        // 2. On charge la scène de jeu
        yield return SceneManager.LoadSceneAsync(data.sceneName);
        yield return null; // On attend que la scène s'initialise (Start() s'exécute ici avec les bonnes données !)

        // 3. On applique les données du joueur et du monde
        PlayerController.Instance.LoadSaveData(data.playerController);

        if (data.inventory != null)
            InventorySystem.instance.LoadSaveData(data.inventory);

        if (data.palette != null)
            PaletteSystem.instance.LoadSaveData(data.palette);

        if (data.world != null)
            WorldStateManager.Instance.LoadSaveData(data.world);

        if (data.equipment != null)
            EquipmentSystem.instance.LoadSaveData(data.equipment);

        if (data.map != null)
            MapManager.instance.LoadSaveData(data.map);

        if (data.quests != null)
            NewQuestManager.instance.LoadSaveData(data.quests);

        if (data.recipes != null && RecipeDatabase.Instance != null)
            RecipeDatabase.Instance.LoadSaveData(data.recipes);

        if (data.questLog != null)
            NewQuestLog.instance.LoadSaveData(data.questLog);


        while (ChestInventory.Instance == null)
        {
            yield return null;
        }

        if (data.chestInventory != null)
            ChestInventory.Instance.LoadSaveData(data.chestInventory);

        Debug.Log("Game Loaded");
    }

    public void DeleteSave(int slot)
    {
        string path = GetSavePath(slot);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save file to delete in slot {slot}");
            return;
        }

        File.Delete(path);

        Debug.Log($"Save slot {slot} deleted");
    }

    public bool HasSave(int slot)
    {
        return File.Exists(GetSavePath(slot));
    }
}



[System.Serializable]
public class SaveData
{
    public PlayerControllerSaveData playerController;

    public InventorySaveData inventory;
    public PaletteSaveData palette;
    public EquipmentSaveData equipment;
    public MapSaveData map;
    public QuestSaveData quests;
    public WorldStateSaveData world;
    public QuestLogSaveData questLog;
    public ChestInventoryData chestInventory;
    public RecipeSaveData recipes;
    public ProgressionSaveData progression;

    public string sceneName;
    public int saveVersion = 1;
}


