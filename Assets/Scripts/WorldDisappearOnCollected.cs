using UnityEngine;
using System.Collections;

public abstract class WorldDisappearOnCollected : MonoBehaviour
{
    protected WorldObjectID worldID;

    protected virtual void Awake()
    {
        worldID = GetComponent<WorldObjectID>();
    }

    protected virtual void OnEnable()
    {
        if (worldID == null)
            worldID = GetComponent<WorldObjectID>();

        if (worldID == null || WorldStateManager.Instance == null)
            return;

        // On s'abonne aux futurs rechargements (ex: charger une sauvegarde)
        WorldStateManager.Instance.Subscribe(ApplyWorldState);

        ApplyWorldState();
    }

    protected virtual void OnDisable()
    {
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.Unsubscribe(ApplyWorldState);
    }

    protected void ApplyWorldState()
    {
        if (worldID != null && WorldStateManager.Instance.IsCollected(worldID.UniqueID))
        {
            StartCoroutine(DestroyNextFrame());
            Debug.LogWarning($"<color=orange>[{name}] Déjà collecté/détruit -> Suppression avec l'ID : {worldID.UniqueID}</color>");
        }
        else if (worldID == null)
        {
            Debug.LogWarning($"<color=red>[{name}] Composant WorldObjectID manquant !</color>");
        }
    }

    private IEnumerator DestroyNextFrame()
    {
        yield return null; // attendre la fin du frame
        Destroy(gameObject);
    }
}