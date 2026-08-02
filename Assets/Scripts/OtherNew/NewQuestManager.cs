using System.Collections.Generic;
using UnityEngine;

public class NewQuestManager : MonoBehaviour
{
    public static NewQuestManager instance;


    [SerializeField] private AllRecipeData allRecipeData;

    [Header("Quests Lists")]
    public List<QuestInstance> activeQuests = new List<QuestInstance>();
    public List<QuestInstance> finishedQuests = new List<QuestInstance>();

    [Header("Global History (Retroactive tracking)")]
    public Dictionary<string, int> globalKillHistory = new Dictionary<string, int>();
    public HashSet<string> globalInteractionHistory = new HashSet<string>();

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rewardGived;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // Ajouter une quête
    public void AddQuest(QuestSO questData)
    {
        if (activeQuests.Exists(q => q.data == questData))
            return;

        QuestInstance newQuest = new QuestInstance
        {
            data = questData,
            status = QuestStatus.InProgress,
            currentCount = 0,
            interactionDone = false,
            escortFinished = false
        };

        if (questData.questType == QuestType.Hunt)
        {
            string enemyKey = questData.targetEnemyType.ToString();
            if (globalKillHistory.ContainsKey(enemyKey))
            {
                newQuest.currentCount = globalKillHistory[enemyKey];
            }
        }
        else if (questData.questType == QuestType.Interaction)
        {
            if (globalInteractionHistory.Contains(questData.questID))
            {
                newQuest.interactionDone = true;
            }
        }
        else if (questData.questType == QuestType.Collect || questData.questType == QuestType.Craft)
        {
            if (InventorySystem.instance != null && questData.requiredItem != null)
            {
                newQuest.currentCount = InventorySystem.instance.GetItemCount(questData.requiredItem);
            }
        }

        activeQuests.Add(newQuest);

        if (NewQuestLog.instance != null)
            NewQuestLog.instance.CreateQuestButton(newQuest);
    }

    // Marquer une interaction comme faite
    public void MarkInteractionDone(QuestSO quest)
    {
        if (!globalInteractionHistory.Contains(quest.questID))
            globalInteractionHistory.Add(quest.questID);

        QuestInstance q = activeQuests.Find(x => x.data == quest);
        if (q != null)
        {
            q.interactionDone = true;
        }

        if (NewQuestLog.instance != null)
        {
            NewQuestLog.instance.UpdateHUDToggleState();
        }
    }

    public QuestInstance GetQuestInstance(QuestSO questData)
    {
        return activeQuests.Find(q => q.data == questData);
    }

    public bool IsFinished(QuestSO questData)
    {
        return finishedQuests.Exists(q => q.data == questData);
    }

    // Vérifier progression (ex : ramasser un objet, tuer un ennemi…)
    public void UpdateQuestProgress(string target, int amount = 1, ItemData itemDataTarget = null)
    {
        if (itemDataTarget == null) // C'est un monstre (Hunt)
        {
            if (!globalKillHistory.ContainsKey(target))
                globalKillHistory[target] = 0;

            globalKillHistory[target] += amount;
        }

        // On met à jour les quêtes actives en cours
        foreach (var quest in activeQuests)
        {
            if (quest.status != QuestStatus.InProgress) continue;

            switch (quest.data.questType)
            {
                case QuestType.Craft:
                case QuestType.Collect:
                    if (quest.data.requiredItem != null && quest.data.requiredItem == itemDataTarget)
                        quest.currentCount += amount;
                    break;

                case QuestType.Hunt:
                    if (quest.data.targetEnemyType.ToString() == target)
                        quest.currentCount += amount;
                    break;
            }
        }

        if (NewQuestLog.instance != null)
        {
            NewQuestLog.instance.UpdateHUDToggleState();
        }
    }

    public bool CanCompleteQuest(QuestInstance quest)
    {
        if (quest == null || quest.status != QuestStatus.InProgress)
            return false;

        return quest.data.IsComplete(quest.currentCount, quest.interactionDone, quest.escortFinished);
    }

    public void CompleteQuest(QuestInstance quest)
    {
        quest.status = QuestStatus.Completed;
        QuestInstance toRemove = activeQuests.Find(q => q.data == quest.data);

        if (toRemove != null)
            activeQuests.Remove(toRemove);

        if (!finishedQuests.Exists(q => q.data == quest.data))
            finishedQuests.Add(quest);
    }

    public void ApplyRewards(QuestInstance questInstance)
    {
        if (questInstance.data.rewards == null) return;

        bool hasGivenReward = false;

        // 1. Récompense en or
        if (questInstance.data.rewards.gold > 0)
        {
            PlayerController.Instance.Wallet.AddGold(questInstance.data.rewards.gold);
            hasGivenReward = true;
        }

        // 2. Récompense en objets (Sécurité null-check '?.')
        var items = questInstance.data.rewards.items;
        if (items != null && items.Length > 0)
        {
            foreach (var item in items)
            {
                if (item.itemType == ItemType.Recipe)
                {
                    if (!allRecipeData.unlockedRecipes.Contains(item.recipe))
                    {
                        allRecipeData.unlockedRecipes.Add(item.recipe);
                    }
                }
                else
                {
                    InventorySystem.instance.AddItem(item);
                }
            }
            hasGivenReward = true;
        }

        // 3. Son joué uniquement si AU MOINS une récompense a été donnée
        if (hasGivenReward && audioSource != null && rewardGived != null)
        {
            audioSource.PlayOneShot(rewardGived);
        }

        questInstance.rewardsGiven = true;
    }

    #region Save/Load
    public QuestSaveData GetSaveData()
    {
        QuestSaveData data = new QuestSaveData();

        foreach (var quest in activeQuests) data.activeQuests.Add(ToSaveData(quest));
        foreach (var quest in finishedQuests) data.completedQuests.Add(ToSaveData(quest));

        foreach (var kvp in globalKillHistory)
        {
            data.globalKillHistorySave.Add(new KillHistoryEntry { enemyType = kvp.Key, count = kvp.Value });
        }
        data.globalInteractionHistorySave = new List<string>(globalInteractionHistory);

        return data;
    }

    private QuestInstanceSaveData ToSaveData(QuestInstance quest)
    {
        return new QuestInstanceSaveData
        {
            questID = quest.data.questID,
            status = quest.status,
            currentCount = quest.currentCount,
            interactionDone = quest.interactionDone,
            escortFinished = quest.escortFinished,
            rewardsGiven = quest.rewardsGiven
        };
    }

    public void LoadSaveData(QuestSaveData data)
    {
        if (data == null) return;

        activeQuests.Clear();
        finishedQuests.Clear();
        globalKillHistory.Clear();

        foreach (var entry in data.globalKillHistorySave)
        {
            globalKillHistory[entry.enemyType] = entry.count;
        }

        // Sécurité anti-crash si la liste de sauvegarde est nulle
        globalInteractionHistory = data.globalInteractionHistorySave != null
            ? new HashSet<string>(data.globalInteractionHistorySave)
            : new HashSet<string>();

        // Chargement des quêtes actives et complétées
        foreach (var questData in data.activeQuests)
        {
            QuestInstance quest = FromSaveData(questData);
            if (quest != null) activeQuests.Add(quest);
        }

        foreach (var questData in data.completedQuests)
        {
            QuestInstance quest = FromSaveData(questData);
            if (quest != null) finishedQuests.Add(quest);
        }

        if (NewQuestLog.instance != null)
        {
            NewQuestLog.instance.OnAffichageQuestPanel(activeQuests);
        }
    }

    private QuestInstance FromSaveData(QuestInstanceSaveData data)
    {
        QuestSO questSO = QuestDatabase.Instance.GetQuestByID(data.questID);
        if (questSO == null) return null;

        return new QuestInstance
        {
            data = questSO,
            status = data.status,
            currentCount = data.currentCount,
            interactionDone = data.interactionDone,
            escortFinished = data.escortFinished,
            rewardsGiven = data.rewardsGiven
        };
    }
    #endregion
}

// ─── EXTENSIONS DE STRUCTURES DE SAUVEGARDE SÉRIALISABLES ───
[System.Serializable]
public class QuestSaveData
{
    public List<QuestInstanceSaveData> activeQuests = new();
    public List<QuestInstanceSaveData> completedQuests = new();

    // Ajoutés pour sérialiser proprement les Dictionnaires/HashSets de l'historique
    public List<KillHistoryEntry> globalKillHistorySave = new();
    public List<string> globalInteractionHistorySave = new();
}

[System.Serializable]
public class KillHistoryEntry
{
    public string enemyType;
    public int count;
}