using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 🟢 Ajouté pour gérer le changement de scène

public class NewQuestLog : MonoBehaviour
{
    public static NewQuestLog instance;

    [Header("UI Active Quest Texte (HUD)")]
    public TextMeshProUGUI QuestActiveText;
    public TextMeshProUGUI objectifQuestActiveText;
    public Toggle questActiveToggle;
    public GameObject panelQuestActive;

    [Header("UI Menu Panel Info (Journal)")]
    [SerializeField] private Image questLevelImage;
    [SerializeField] private Sprite questLevel1, questLevel2, questLevel3, questLevel4, questLevel5;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private Toggle questToggle;
    [SerializeField] private TextMeshProUGUI questObjectifText;
    [SerializeField] private Toggle questObjectifToggle;

    [Header("UI Menu Panel List Quests")]
    [SerializeField] private GameObject panelDescriptionQuest;
    [SerializeField] private Transform questsFirstList;
    [SerializeField] private Transform questsSecondList;
    [SerializeField] private Transform rewardsList;

    [Header("Prefabs")]
    [SerializeField] private GameObject buttonQuestPrefab;
    [SerializeField] private GameObject rewardQuestPrefab;

    [System.NonSerialized] public QuestInstance currentlyTrackedQuest;
    [System.NonSerialized] public QuestInstance currentlySelectedQuest;

    private readonly HashSet<string> bossScenes = new HashSet<string>
    {
        "Boss1",
        "Boss2",
        "Boss3",
        "BossFinal",
        "GrotteSecreteBoss"
    };

    private bool _wasHUDActiveBeforeBoss = false;
    private bool _isInBossScene = false;

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

    #region Gestion du Masquage du HUD en Scène de Boss
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isBossScene = bossScenes.Contains(scene.name);

        if (isBossScene)
        {
            // Si on vient d'une scène normale et qu'on rentre chez un boss
            if (!_isInBossScene)
            {
                // On enregistre si le panneau était affiché ou non
                _wasHUDActiveBeforeBoss = (panelQuestActive != null && panelQuestActive.activeSelf);
                _isInBossScene = true;
            }

            // On masque le panneau pendant le combat de boss
            if (panelQuestActive != null)
            {
                panelQuestActive.SetActive(false);
            }
        }
        else
        {
            // Si on vient de sortir d'une scène de boss vers une scène normale
            if (_isInBossScene)
            {
                _isInBossScene = false;

                // On réactive le panneau uniquement s'il était actif avant ET qu'on a toujours une quête suivie
                if (_wasHUDActiveBeforeBoss && currentlyTrackedQuest != null && panelQuestActive != null)
                {
                    panelQuestActive.SetActive(true);
                }

                _wasHUDActiveBeforeBoss = false;
            }
        }
    }
    #endregion

    private void ShowQuest(QuestInstance quest)
    {
        if (quest == null) return;

        currentlySelectedQuest = quest;

        ClearChildren(rewardsList);

        switch (quest.data.questLevel)
        {
            case 1: questLevelImage.sprite = questLevel1; break;
            case 2: questLevelImage.sprite = questLevel2; break;
            case 3: questLevelImage.sprite = questLevel3; break;
            case 4: questLevelImage.sprite = questLevel4; break;
            case 5: questLevelImage.sprite = questLevel5; break;
            default: questLevelImage.sprite = questLevel1; break;
        }

        questNameText.text = quest.data.questName;
        questDescriptionText.text = quest.data.description;
        questObjectifText.text = quest.data.objectif;

        foreach (string reward in quest.data.rewardsText)
        {
            GameObject obj = Instantiate(rewardQuestPrefab, rewardsList);
            obj.GetComponent<TextMeshProUGUI>().text = $"- {reward}";
        }

        if (questToggle != null)
        {
            questToggle.onValueChanged.RemoveAllListeners();

            bool isAlreadyTracked = (currentlyTrackedQuest != null && currentlyTrackedQuest.data == quest.data);
            questToggle.SetIsOnWithoutNotify(isAlreadyTracked);

            questToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    TrackQuestOnHUD(quest);
                else
                    UntrackQuest();
            });
        }

        if (questObjectifToggle != null)
        {
            bool isComplete = NewQuestManager.instance.CanCompleteQuest(quest);
            questObjectifToggle.SetIsOnWithoutNotify(isComplete);
        }
        panelDescriptionQuest.SetActive(true);
    }

    public void TrackQuestOnHUD(QuestInstance quest)
    {
        if (quest == null) return;

        currentlyTrackedQuest = quest;
        QuestActiveText.text = quest.data.questName;
        objectifQuestActiveText.text = quest.data.objectif;

        // Si on est dans une scène de boss, on enregistre l'intention d'afficher le HUD
        // mais on ne l'affiche pas tout de suite pour ne pas encombrer le combat de boss.
        if (_isInBossScene)
        {
            _wasHUDActiveBeforeBoss = true;
            panelQuestActive.gameObject.SetActive(false);
        }
        else
        {
            panelQuestActive.gameObject.SetActive(true);
        }

        if (UIManagerSystem.Instance != null && !UIManagerSystem.Instance.hudElements.Contains(panelQuestActive))
            UIManagerSystem.Instance.hudElements.Add(panelQuestActive);

        UpdateHUDToggleState();
    }

    public void UntrackQuest()
    {
        currentlyTrackedQuest = null;
        panelQuestActive.gameObject.SetActive(false);

        if (_isInBossScene)
        {
            _wasHUDActiveBeforeBoss = false;
        }

        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Remove(panelQuestActive);

        UpdateHUDToggleState();
    }

    public void UpdateHUDToggleState()
    {
        if (currentlyTrackedQuest != null && questActiveToggle != null)
        {
            bool isHUDQuestComplete = NewQuestManager.instance.CanCompleteQuest(currentlyTrackedQuest);
            questActiveToggle.SetIsOnWithoutNotify(isHUDQuestComplete);
        }

        if (currentlySelectedQuest != null)
        {
            if (questObjectifToggle != null)
            {
                bool isMenuQuestComplete = NewQuestManager.instance.CanCompleteQuest(currentlySelectedQuest);
                questObjectifToggle.SetIsOnWithoutNotify(isMenuQuestComplete);
            }

            if (questToggle != null)
            {
                bool isAlreadyTracked = (currentlyTrackedQuest != null && currentlyTrackedQuest.data == currentlySelectedQuest.data);
                questToggle.SetIsOnWithoutNotify(isAlreadyTracked);
            }
        }
    }

    public void CreateQuestButton(QuestInstance quest)
    {
        GameObject button = null;
        if (quest.data.isMainQuest)
            button = Instantiate(buttonQuestPrefab, questsFirstList);
        else
            button = Instantiate(buttonQuestPrefab, questsSecondList);

        SlotQuete slot = button.GetComponent<SlotQuete>();
        slot.questNameText.text = quest.data.questName;

        switch (quest.data.questLevel)
        {
            case 1: slot.questIcon.sprite = questLevel1; break;
            case 2: slot.questIcon.sprite = questLevel2; break;
            case 3: slot.questIcon.sprite = questLevel3; break;
            case 4: slot.questIcon.sprite = questLevel4; break;
            case 5: slot.questIcon.sprite = questLevel5; break;
            default: slot.questIcon.sprite = questLevel1; break;
        }
        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(() =>
        {
            ShowQuest(quest);
        });
    }

    public void DesactivePanel()
    {
        currentlySelectedQuest = null;
        panelDescriptionQuest.SetActive(false);
    }

    private void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }

    public void OnAffichageQuestPanel(List<QuestInstance> listQuest)
    {
        ClearChildren(questsFirstList);
        ClearChildren(questsSecondList);
        foreach (var quest in listQuest)
            CreateQuestButton(quest);

        currentlySelectedQuest = null;
        panelDescriptionQuest.SetActive(false);
    }

    #region Save/Load
    public QuestLogSaveData GetSaveData()
    {
        bool hudActive = false;
        if (panelQuestActive != null)
        {
            // En scène de boss, la valeur sauvegardée est l'état mémorisé d'avant le boss
            hudActive = _isInBossScene ? _wasHUDActiveBeforeBoss : panelQuestActive.activeSelf;
        }
        else
        {
            Debug.LogError("[Save System] 'panelQuestActive' n'est pas assigné dans l'Inspecteur de NewQuestLog !", this);
        }

        string questID = string.Empty;
        if (currentlyTrackedQuest != null)
        {
            if (currentlyTrackedQuest.data != null)
            {
                questID = currentlyTrackedQuest.data.questID;
            }
            else
            {
                Debug.LogError("[Save System] 'currentlyTrackedQuest' existe mais son champ 'data' est NULL !", this);
            }
        }

        return new QuestLogSaveData
        {
            trackedQuestID = questID,
            isHUDPanelActive = hudActive
        };
    }

    public void LoadSaveData(QuestLogSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.trackedQuestID))
        {
            UntrackQuest();
            return;
        }

        QuestInstance loadedQuest = NewQuestManager.instance.activeQuests.Find(q => q.data.questID == data.trackedQuestID);

        if (loadedQuest != null)
        {
            TrackQuestOnHUD(loadedQuest);

            // Si on charge le jeu pendant un boss, on garde le HUD masqué mais on enregistre qu'il doit réapparaître après
            string currentScene = SceneManager.GetActiveScene().name;
            if (bossScenes.Contains(currentScene))
            {
                _wasHUDActiveBeforeBoss = data.isHUDPanelActive;
                panelQuestActive.gameObject.SetActive(false);
            }
            else
            {
                panelQuestActive.gameObject.SetActive(data.isHUDPanelActive);
            }
        }
        else
        {
            UntrackQuest();
        }
    }
    #endregion
}

[System.Serializable]
public class QuestLogSaveData
{
    public string trackedQuestID;
    public bool isHUDPanelActive;
}
