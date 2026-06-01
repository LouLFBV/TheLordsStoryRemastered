using UnityEngine;
using System.Collections;

public class InteractSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AllRecipeData allRecipeData; 
    [SerializeField] private PlayerController player; // Changé de MoveBehaviour à PlayerController
    [SerializeField] private InventorySystem inventory; // Changé de Inventory à InventorySystem
    [SerializeField] private EquipmentSystem equipmentSystem;
    [SerializeField] private EquipmentLibrary equipmentLibrary;
    [SerializeField] private AudioSource audioSource;
    [HideInInspector] public bool isBusy = false;
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Tools Configuration")]
    [SerializeField] private GameObject pickaxeVisual;
    [SerializeField] private AudioClip pickaxeSound;
    [SerializeField] private GameObject axeVisual;
    public bool canAxe = false;
    [SerializeField] private AudioClip axeSound;
    public bool canPickaxe = false;
    public bool canSuperPickaxe = false;


    [Header("Other")]
    [SerializeField] private AudioClip pickUpSound;

    private Item currentItem;
    private Harvestable currentHarvestable;
    private Tool currentTool;


    private Vector3 spawnItemOffset = new Vector3(0, 0.5f, 0);
    public void DoPickUp(Item item)
    {
        if (isBusy) return;

        if (IsInventoryFull(item.itemData))
        {
            Debug.LogWarning("Inventaire plein !");
            return;
        }

        currentItem = item;

        // On exécute directement l'ajout sans passer par l'animator
        ProcessPickUpLogic();
    }
    private void ProcessPickUpLogic()
    {
        if (currentItem == null) return;

        if (currentItem.itemData.itemType == ItemType.Recipe)
        {
            currentItem.GetComponent<BookRecipe>().OpenCanvasRecipeBook();

            // On ajoute directement à l'unique liste, peu importe ce que c'est !
            if (!allRecipeData.unlockedRecipes.Contains(currentItem.itemData.recipe))
            {
                allRecipeData.unlockedRecipes.Add(currentItem.itemData.recipe);
            }
        }
        else if (currentItem.itemData.itemType == ItemType.Map)
        {
            MapManager.instance.AddIconeMap(currentItem.itemData);
        }
        else
        {
            // Ajout standard
            for (int i = 0; i < currentItem.amount; i++)
            {
                if (IsInventoryFull(currentItem.itemData))
                    break;

                inventory.AddItem(currentItem.itemData);
                NewQuestManager.instance.UpdateQuestProgress("", 1, currentItem.itemData);
            }
        }

        // Feedback Audio
        audioSource.PlayOneShot(pickUpSound);

        // Nettoyage de l'objet dans le monde
        var interact = currentItem.GetComponent<IInteractable>();
        interact?.SetTargeted(false, player.transform);

        if (currentItem.TryGetComponent<WorldObjectID>(out var id))
        {
            WorldStateManager.Instance.RegisterCollectedObject(id.UniqueID);
        }

        Transform itemTransform = currentItem.transform;

        Destroy(currentItem.gameObject);
        RespawnObject(itemTransform);

        currentItem = null;
    }
    public void DoHarvest(Harvestable harvestable)
    {
        if (isBusy) return;

        isBusy = true;
        currentTool = harvestable.tool;
        EnableToolSound(currentTool);
        currentHarvestable = harvestable;

        Debug.Log($"Starting harvest on {harvestable.name} with tool {currentTool}. canAxe: {canAxe}, canPickaxe: {canPickaxe}, canSuperPickaxe: {canSuperPickaxe}");
        player.Animator.SetTrigger("Harvest");
        // On bloque le mouvement via la StateMachine si possible, ou via canMove
        // player.StateMachine.ChangeState(PlayerStateType.Busy); 
    }

    private void EnableToolSound(Tool toolType)
    {
        switch (toolType)
        {
            case Tool.Pickaxe: audioSource.clip = pickaxeSound; break;
            case Tool.Axe: audioSource.clip = axeSound; break;
        }
    }


    private void RespawnObject(Transform objectToRespawn)
    {
        if (objectToRespawn.parent != null && objectToRespawn.parent.TryGetComponent<ObjectsSpawner>(out var spawner))
        {
            spawner.OnObjectCollected();
        }
    }

    //Coroutine appelé depuis l'animation "Haversitng"
    public IEnumerator BreakHarvestable()
    {
        Harvestable currentlyHarveting = currentHarvestable;
        currentlyHarveting.gameObject.layer = LayerMask.NameToLayer("Default");
        if (currentlyHarveting.disableKinematicOnHarvest)
        {
            Rigidbody rigidbody = currentlyHarveting.GetComponent<Rigidbody>();
            rigidbody.isKinematic = false;
            rigidbody.AddForce(transform.forward * 800, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(currentlyHarveting.destroyDelay);
        for (int i = 0; i < currentlyHarveting.harvestableItems.Length; i++)
        {
            Ressource ressource = currentlyHarveting.harvestableItems[i];
            if (Random.Range(0, 101) <= ressource.dropChance)
            {
                GameObject instantiatedRessource = Instantiate(ressource.itemData.prefab);

                // Rayon max du décalage (à ajuster)
                float spawnRadius = 0.2f;

                // Génère un décalage aléatoire dans un petit rayon (sur le sol uniquement, Y = 0)
                Vector3 randomOffset = new Vector3(
                    Random.Range(-spawnRadius, spawnRadius),
                    0f,
                    Random.Range(-spawnRadius, spawnRadius)
                );

                // Position finale = position du harvesting + offset initial + petit décalage aléatoire
                instantiatedRessource.transform.position = currentlyHarveting.transform.position + spawnItemOffset + randomOffset;
            }
        }
        Debug.Log("Harvested: " + currentlyHarveting.name);
        if (currentlyHarveting.TryGetComponent<WorldObjectID>(out var worldID))
        {
            Debug.Log($"<color=yellow> Registering collected object with ID: {worldID.UniqueID} </color>");
            WorldStateManager.Instance.RegisterCollectedObject(worldID.UniqueID);
        }
        else
        {
            Debug.Log("No WorldObjectID found on harvested object: " + currentlyHarveting.name);
        }

        Destroy(currentlyHarveting.gameObject);
        RespawnObject(currentlyHarveting.transform);
    }

    public void SetCurrentEquippedItem(EquipmentLibraryItem equippedItem)
    {
        //equipmentToDesactiveAndActive = equippedItem;
    }

    public void AddItemToInventory()
    {
        if (currentItem == null) return;
        if (currentItem.itemData.itemType == ItemType.Recipe)
        {
            currentItem.GetComponent<BookRecipe>().OpenCanvasRecipeBook();

            // On ajoute directement à l'unique liste, peu importe ce que c'est !
            if (!allRecipeData.unlockedRecipes.Contains(currentItem.itemData.recipe))
            {
                allRecipeData.unlockedRecipes.Add(currentItem.itemData.recipe);
            }
        }
        else if (currentItem.itemData.itemType == ItemType.Map)
        {
            MapManager.instance.AddIconeMap(currentItem.itemData);
        }
        else
        {
            for (int i = 0; i < currentItem.amount; i++)
            {
                if (!IsInventoryFull(currentItem.itemData))
                {
                    inventory.AddItem(currentItem.itemData);
                    NewQuestManager.instance.UpdateQuestProgress("", 1, currentItem.itemData);
                }
            }
        }

        audioSource.PlayOneShot(pickUpSound);

        var interact = currentItem.GetComponent<IInteractable>();
        interact?.SetTargeted(false, player.transform);
        if (currentItem.TryGetComponent<WorldObjectID>(out var id))
        {
            WorldStateManager.Instance.RegisterCollectedObject(id.UniqueID);
            Debug.Log($"<color=yellow> Registering collected object with ID: {id.UniqueID} </color>");
        }

        Destroy(currentItem.gameObject);

        RespawnObject(currentItem.transform);
    }

    //private void EnableToolGameObjectFromTool(Tool toolType)
    //{
    //    switch (toolType)
    //    {
    //        case Tool.Pickaxe:
    //            audioSource.clip = pickaxeSound;
    //            break;
    //        case Tool.Axe:
    //            audioSource.clip = axeSound;
    //            break;
    //    }
    //}

    public void PlayHarvestingSoundEffect()
    {
        audioSource.Play();
    }

    bool IsInventoryFull(ItemData itemData)
    {
        switch (itemData.itemType)
        {
            case ItemType.Ressource:
                return inventory.IsFullRessources();

            case ItemType.Craft:
                return inventory.IsFullCraft();

            case ItemType.Equipment:
            case ItemType.Consumable:
                return inventory.IsFullEquipment();

            default:
                return false;
        }
    }
}