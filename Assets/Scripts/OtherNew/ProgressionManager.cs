using System.Collections.Generic;
using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    private List<string> completedEvents = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Ajoute un événement à la liste quand un boss meurt
    public void MarkEventAsCompleted(string eventID)
    {
        if (!completedEvents.Contains(eventID))
        {
            completedEvents.Add(eventID);
            Debug.Log($"Événement accompli : {eventID}");
            // C'est souvent ici qu'on appelle la sauvegarde automatique !
        }
    }

    // Vérifie si un événement a déjà été fait
    public bool IsEventCompleted(string eventID)
    {
        return completedEvents.Contains(eventID);
    }

    #region Save & Load
    public ProgressionSaveData GetSaveData()
    {
        return new ProgressionSaveData
        {
            completedEvents = new List<string>(this.completedEvents)
        };
    }

    public void LoadSaveData(ProgressionSaveData data)
    {
        if (data == null || data.completedEvents == null)
        {
            completedEvents = new List<string>();
            return;
        }

        completedEvents = data.completedEvents;
    }
    #endregion
}


[System.Serializable]
public class ProgressionSaveData
{
    // On stocke simplement les "noms" des événements terminés
    public List<string> completedEvents = new List<string>();
}