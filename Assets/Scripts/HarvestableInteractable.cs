using UnityEngine;

public class HarvestableInteractable : InteractableBase
{
    private Harvestable harvestable;

    private void Awake()
    {
        harvestable = GetComponent<Harvestable>();
    }

    public override void OnInteract(PlayerInteractor player)
    {
        Debug.Log("Interacting with Harvestable");
        var interactBehaviour = player.GetComponent<InteractSystem>();

        if (interactBehaviour == null || harvestable == null)
        {
            Debug.Log("Cannot harvest: missing InteractBehaviour or Harvestable component.");
            return;
        }

        // Vérification outil
        bool canHarvest = false;
        switch (harvestable.tool)
        {
            case Tool.Axe:
                canHarvest = interactBehaviour.canAxe;
                Debug.Log($"Harvestable requires Axe. Player canAxe: {interactBehaviour.canAxe}");
                break;
            case Tool.Pickaxe:
                // Pickaxe peut être détruit avec canPickaxe OU canSuperPickaxe
                canHarvest = interactBehaviour.canPickaxe || interactBehaviour.canSuperPickaxe;
                Debug.Log($"Harvestable requires Pickaxe. Player canPickaxe: {interactBehaviour.canPickaxe}, canSuperPickaxe: {interactBehaviour.canSuperPickaxe}");
                break;
            case Tool.SuperPickaxe:
                canHarvest = interactBehaviour.canSuperPickaxe;
                Debug.Log($"Harvestable requires Super Pickaxe. Player canSuperPickaxe: {interactBehaviour.canSuperPickaxe}");
                break;
            default:
                canHarvest = false;
                Debug.Log("Harvestable has unknown tool requirement.");
                break;
        }

        if (!canHarvest)
        {
            Debug.Log("Cannot harvest: incorrect tool.");
            return;
        }

        Debug.Log("Harvesting...");
        interactBehaviour.DoHarvest(harvestable);
        if (TryGetComponent<WorldObjectID>(out var id))
        {
            WorldStateManager.Instance.RegisterCollectedObject(id.UniqueID);
            Debug.Log($"<color=yellow> Registering collected object with ID: {id.UniqueID} </color>");
        }
    }
}