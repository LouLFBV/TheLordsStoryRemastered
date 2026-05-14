using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NewQuestLog : MonoBehaviour
{
    public static NewQuestLog instance;

    [Header("UI Active Quest Texte")]
    public TextMeshProUGUI QuestActiveText;
    public TextMeshProUGUI objectifQuestActiveText;
    public Toggle questActiveToggle;
    public GameObject panelQuestActive;


    [Header("UI Menu Panel Info")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI questObjectifText;
    [SerializeField] private Toggle questToggle;

    [Header("UI Menu Panel List Quests")]
    [SerializeField] private GameObject panelDescriptionQuest;
    [SerializeField] private Transform questsFirstList;
    [SerializeField] private Transform questsSecondList;
    [SerializeField] private Transform rewardsList;

    [Header("Prefabs")]

    [SerializeField] private GameObject buttonQuestPrefab;
    [SerializeField] private GameObject objectifQuestPrefab;
    [SerializeField] private GameObject rewardQuestPrefab;
    [SerializeField] private GameObject objectifOnScreenPrefab;
    //[SerializeField] private GameObject gameObjectPourAfficher;
    //[SerializeField] private TextMeshProUGUI compteurEnemiesText;
    //public Toggle questToggle;

    [SerializeField] private UINavigationManager uiNavigationManager;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    private void ShowQuest(QuestInstance quest/*, bool isActive*/)
    {
        if (quest == null) return;

        ClearChildren(rewardsList);

        questNameText.text = quest.data.questName;
        questDescriptionText.text = quest.data.description;
        questObjectifText.text = quest.data.objectif;
        foreach (string reward in quest.data.rewardsText)
        {
            GameObject obj = Instantiate(rewardQuestPrefab, rewardsList);
            obj.GetComponent<TextMeshProUGUI>().text = $"- {reward}";
        }
        panelDescriptionQuest.SetActive(true);

        //if (quest.data.questType == QuestType.Hunt && isActive)
        //{
        //    compteurEnemiesText.gameObject.SetActive(true);
        //    QuestInstance questInstance = QuestManager.instance.GetQuestInstance(quest.data);
        //    compteurEnemiesText.text = $"Ennemis tués : {questInstance.currentCount} / {quest.data.requiredKillCount}";
        //}
        //else
        //{
        //    compteurEnemiesText.gameObject.SetActive(false);
        //}

        // Active = toggle disponible, Completed = pas de toggle
        //gameObjectPourAfficher.SetActive(isActive);

        //if (isActive)
        //{
        //    questToggle.onValueChanged.RemoveAllListeners();
        //    questToggle.onValueChanged.AddListener((isOn) =>
        //    {
        //        if (isOn)
        //            ActiveDesactiveQuestText(questNameText.text);
        //        else
        //            QuestActiveText.gameObject.SetActive(false);
        //    });
        //}
    }

    public void CreateQuestButton(QuestInstance quest/*, bool isActive*/)
    {
        GameObject button = null;
        if (quest.data.isMainQuest)
             button = Instantiate(buttonQuestPrefab, questsFirstList);
        else
             button = Instantiate(buttonQuestPrefab, questsSecondList);

        uiNavigationManager.elements.Add(button.GetComponent<UISelectable>());

        SlotQuete slot = button.GetComponent<SlotQuete>();
        // slot.icone.sprite = quest.data.icon;
        slot.questNameText.text = quest.data.questName;
        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(() =>
        {
            ShowQuest(quest/*, isActive*/);
        });
    }
    public void DesactivePanel() => panelDescriptionQuest.SetActive(false);

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

        panelDescriptionQuest.SetActive(false);
    }
    public void ActiveDesactiveQuestText(QuestSO questSO)
    {
        QuestActiveText.text = questSO.questName;
        objectifQuestActiveText.text = questSO.objectif;
        panelQuestActive.gameObject.SetActive(true);
        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Add(panelQuestActive);
    }

    //public void OnAffichage()
    //{
    //    panelDescriptionQuest.SetActive(false);
    //    foreach (Transform child in QuestsList)
    //        Destroy(child.gameObject);
    //}

    #region Save/Load
    public QuestLogSaveData GetSaveData()
    {
        return new QuestLogSaveData
        {
            activeQuestText = QuestActiveText.gameObject.activeSelf
                ? QuestActiveText.text
                : string.Empty,
            isQuestToggleOn = questActiveToggle != null && questActiveToggle.isOn
        };
    }

    public void LoadSaveData(QuestLogSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.activeQuestText))
        {
            QuestActiveText.gameObject.SetActive(false);
            return;
        }

        //  Texte
        QuestActiveText.text = data.activeQuestText;
        objectifQuestActiveText.text = data.activeObjectifQuestText;
        panelQuestActive.gameObject.SetActive(true);

        //  Toggle (sans déclencher l’event)
        //if (questToggle != null)
        //{
        //    questToggle.onValueChanged.RemoveAllListeners();
        //    questToggle.isOn = data.isQuestToggleOn;

        //    questToggle.onValueChanged.AddListener(isOn =>
        //    {
        //        if (isOn)
        //            ActiveDesactiveQuestText(QuestActiveText.text);
        //        else
        //            QuestActiveText.gameObject.SetActive(false);
        //    });
        //}
    }
    #endregion
}

[System.Serializable]
public class QuestLogSaveData
{
    public string activeQuestText;
    public string activeObjectifQuestText;
    public bool isQuestToggleOn;
}

