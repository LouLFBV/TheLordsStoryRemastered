using UnityEngine;
using System.Collections;

public abstract class WorldDisappearOnCollected : MonoBehaviour
{
    protected WorldObjectID worldID;

    protected virtual void Awake()
    {
        worldID = GetComponent<WorldObjectID>();
        //Debug.Log($"[Awake] {name} activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy}, with ID : {worldID.UniqueID}");
    }

    protected virtual void OnEnable()
    {
       //Debug.Log($"[OnEnable] {name}, with ID : {worldID.UniqueID}");

        if (worldID == null || WorldStateManager.Instance == null)
            return;

        WorldStateManager.Instance.Subscribe(ApplyWorldState);
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
            Debug.LogWarning($"<color=orange>[{name}] checked world state: Collected = {WorldStateManager.Instance.IsCollected(worldID.UniqueID)}, with ID : {worldID.UniqueID}</color>");
        }
        else if (worldID != null)
        {
            Debug.LogWarning($"<color=cyan>[{name}] checked world state: Collected = {WorldStateManager.Instance.IsCollected(worldID.UniqueID)}, with ID : {worldID.UniqueID}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=red>[{name}] has no WorldObjectID component!, with ID : {worldID.UniqueID}</color>");
        }
        Debug.Log($"[ApplyWorldState] {name} activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy}, with ID : {worldID.UniqueID}");
    }
    private IEnumerator DestroyNextFrame()
    {
        yield return null; // attendre la fin de l'event
        Destroy(transform.gameObject);
    }
}
