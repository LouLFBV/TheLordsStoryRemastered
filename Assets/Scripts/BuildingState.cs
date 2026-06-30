using UnityEngine;

public class BuildingState : MonoBehaviour
{
    private WorldObjectID worldID;

    [SerializeField] private GameObject buildingVisual;

    private void Awake()
    {
        worldID = GetComponent<WorldObjectID>();
    }

    private void OnEnable()
    {
        Debug.LogWarning($"<color=blue>[BuildingState] Awake called for building {worldID.UniqueID}.</color>");
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateLoaded += ApplyWorldState;
        ApplyWorldState();
    }

    private void OnDisable()
    {
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateLoaded -= ApplyWorldState;
    }

    private void ApplyWorldState()
    {
        if (WorldStateManager.Instance.IsActived(worldID.UniqueID))
        {
            buildingVisual.SetActive(true);
            Debug.Log($"<color=green>[BuildingState] Activated building {worldID.UniqueID}.</color>");
        } 
        else if (WorldStateManager.Instance.IsCollected(worldID.UniqueID))
        {
            buildingVisual.SetActive(false);
            Debug.Log($"<color=green>[BuildingState] Collected and destroyed building {worldID.UniqueID}.</color>");
        }
        else
        {
            Debug.Log($"<color=yellow>[BuildingState] Building {worldID.UniqueID} remains inactive.</color>");
        }
    }

    /// <summary>
    /// Appelle cette méthode lorsque le joueur donne les ressources pour construire cet élément.
    /// </summary>
    public void ConstructBuilding()
    {
        if (worldID == null) worldID = GetComponent<WorldObjectID>();

        // 1. On l'enregistre dans le Manager de l'état du monde
        WorldStateManager.Instance.RegisterActivedBuilding(worldID.UniqueID);

        // 2. On applique visuellement le changement immédiatement
        ApplyWorldState();

        // 3. SAUVEGARDE IMMÉDIATE SUR LE DISQUE
        // Ici, tu appelles ton script global de sauvegarde (ex: SaveManager)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log($"<color=cyan>[BuildingState] Sauvegarde automatique réussie pour : {gameObject.name}</color>");
        }
        else
        {
            Debug.LogWarning("SaveManager.Instance introuvable. Le bâtiment est actif en mémoire mais pas encore écrit sur le disque.");
        }
    }
}
