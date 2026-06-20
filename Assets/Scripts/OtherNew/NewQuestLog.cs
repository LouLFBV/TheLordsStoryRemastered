using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [HideInInspector] public QuestInstance currentlyTrackedQuest;  
    [HideInInspector] public QuestInstance currentlySelectedQuest; 

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void ShowQuest(QuestInstance quest)
    {
        if (quest == null) return;

        // On mémorise quelle quête est actuellement ouverte dans le menu
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

        // --- GESTION DU TOGGLE DE SUIVI (AFFICHER SUR L'ÉCRAN) ---
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

    // Affiche la quête sur l'écran du joueur (HUD)
    public void TrackQuestOnHUD(QuestInstance quest)
    {
        if (quest == null) return;

        currentlyTrackedQuest = quest;
        QuestActiveText.text = quest.data.questName;
        objectifQuestActiveText.text = quest.data.objectif;

        panelQuestActive.gameObject.SetActive(true);

        if (UIManagerSystem.Instance != null && !UIManagerSystem.Instance.hudElements.Contains(panelQuestActive))
            UIManagerSystem.Instance.hudElements.Add(panelQuestActive);

        UpdateHUDToggleState();
    }

    // Désactive le suivi de la quête sur l'écran
    public void UntrackQuest()
    {
        currentlyTrackedQuest = null;
        panelQuestActive.gameObject.SetActive(false);

        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Remove(panelQuestActive);

        // On rafraîchit les éléments pour s'assurer que les visuels soient clean
        UpdateHUDToggleState();
    }

    public void UpdateHUDToggleState()
    {
        // 1️⃣ GESTION DU HUD (Écran de jeu)
        if (currentlyTrackedQuest != null && questActiveToggle != null)
        {
            bool isHUDQuestComplete = NewQuestManager.instance.CanCompleteQuest(currentlyTrackedQuest);
            questActiveToggle.SetIsOnWithoutNotify(isHUDQuestComplete);
        }

        if (currentlySelectedQuest != null)
        {
            // Case de l'objectif de description
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
            case 3: slot.questIcon.sprite   = questLevel3; break;
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
        currentlySelectedQuest = null; // On oublie la sélection quand on ferme le panel
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
            hudActive = panelQuestActive.activeSelf;
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

        // 3. Retour des données sécurisées
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
            panelQuestActive.gameObject.SetActive(data.isHUDPanelActive);
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