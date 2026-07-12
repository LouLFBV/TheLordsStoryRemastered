using UnityEngine;

public class ProgressionCondition : MonoBehaviour
{
    [Tooltip("L'ID de l'événement à vérifier (ex: Boss_1_Vaincu)")]
    public string eventID;

    [Tooltip("Cochez pour désactiver l'objet si l'événement EST terminé. Décochez pour l'activer.")]
    public bool disableIfCompleted = true;

    private void Start()
    {
        // On demande au manager si l'ID existe dans sa liste
        bool isCompleted = ProgressionManager.Instance.IsEventCompleted(eventID);

        if (isCompleted)
        {
            // On désactive (ou active) l'objet selon la coche
            gameObject.SetActive(!disableIfCompleted);
        }
    }
}