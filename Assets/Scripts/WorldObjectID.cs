using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldObjectID : MonoBehaviour
{
    [SerializeField] private string uniqueID;
    public string UniqueID => uniqueID;

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
            Debug.LogWarning($"[WorldObjectID] {name} n'a pas de UniqueID !", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ❌ Ne jamais toucher aux prefabs assets dans le dossier Project
        if (PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        // Cas 1 : L'instance n'a aucun ID
        if (string.IsNullOrEmpty(uniqueID))
        {
            GenerateID();
            return;
        }

        // Cas 2 : Détection des doublons dans la scène (ex: duplication via Ctrl+D)
        WorldObjectID[] allObjects = FindObjectsByType<WorldObjectID>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            // Si un AUTRE objet de la scène a exactement le même uniqueID
            if (obj != this && obj.uniqueID == uniqueID)
            {
                GenerateID();
                break;
            }
        }
    }

    [ContextMenu("Régénérer Unique ID")]
    public void GenerateID()
    {
        Undo.RecordObject(this, "Generate Unique ID");
        uniqueID = System.Guid.NewGuid().ToString();
        EditorUtility.SetDirty(this);
    }
#endif
}