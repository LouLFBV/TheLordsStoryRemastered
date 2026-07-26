using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanneauDeConstruction : InteractableBase
{
    [SerializeField] private bool isConstructible = true; // Permet de différencier les panneaux de construction des panneaux de destruction
    [SerializeField] GameObject gameObjectToDestroy;
    [SerializeField] GameObject gameObjectToActive;
    [SerializeField] private GameObject craftPanel;

    [Header("UI Hierarchy")]
    [SerializeField] private GameObject ingredientPrefab;
    [SerializeField] private Transform ingredientContainer;
    [SerializeField] private Button destroyButton;

    [Header("Recipe Configuration")]
    [SerializeField] private List<CraftingRecipe> requiredIngredients;


    [Header("Animation Panel")]
    [SerializeField] private Animator animatorPanelProduits;


    [Header("Audio Settings")]
    [SerializeField] private AudioClip craftSound;

    private void Update()
    {
        if (craftPanel != null && animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
            {
                if (PlayerController.Instance.Input.CloseMenuPressed || PlayerController.Instance.Input.MenuPressed)
                {
                    // On ne log que lorsque l'action de fermeture est validée !
                    Debug.Log("[CONSTRUCTION] Touche de fermeture détectée. Fermeture du panneau.");
                    ClosePanel();
                    PlayerController.Instance.Input.UseCloseMenuInput();
                }
            }
        }
    }

    public override void OnInteract(PlayerInteractor player)
    {
        OpenPanel();
        if (PlayerController.Instance != null && PlayerController.Instance.StateMachine != null)
        {
            PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);
        }
    }

    private void OpenPanel()
    {
        if (craftPanel != null && !animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            RefreshRecipeRequirements();
            craftPanel.SetActive(true);

            if (animatorPanelProduits != null)
                animatorPanelProduits.SetBool("PanelIsOpen", true);

            if (PlayerController.Instance != null)
            {
                SetTargeted(false, PlayerController.Instance.transform);
            }

        }
    }

    public void ClosePanel()
    {
        //if (craftPanel != null) craftPanel.SetActive(false);

        if (animatorPanelProduits != null)
            animatorPanelProduits.SetBool("PanelIsOpen", false);


        // Sécurité UI : On désactive l'interactivité du bouton à la fermeture
        if (destroyButton != null) destroyButton.interactable = false;

        if (PlayerController.Instance != null && PlayerController.Instance.StateMachine != null)
        {
            PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    public void RefreshRecipeRequirements()
    {
        // On parcourt de la fin vers le début pour ne pas casser les index
        for (int i = ingredientContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(ingredientContainer.GetChild(i).gameObject);
        }

        if (requiredIngredients == null || requiredIngredients.Count == 0)
        {
            if (destroyButton != null) destroyButton.interactable = true;
            return;
        }

        bool allRequirementsMet = true;

        foreach (CraftingRecipe recipe in requiredIngredients)
        {
            if (recipe.itemNeeded == null) continue;

            GameObject ingredientUI = Instantiate(ingredientPrefab, ingredientContainer);

            if (ingredientUI.TryGetComponent<SlotRecette>(out var slotScript))
            {
                int playerStock = InventorySystem.instance.GetItemCount(recipe.itemNeeded);
                bool stepValidated = slotScript.SetupIngredient(recipe.itemNeeded, recipe.amountNeededItem, playerStock);

                if (!stepValidated)
                {
                    allRequirementsMet = false;
                }
            }
        }

        if (destroyButton != null)
        {
            destroyButton.interactable = allRequirementsMet;
        }
    }

    public void OnDestroyButtonPressed()
    {
        if (destroyButton == null || !destroyButton.interactable) return;

        // 1. CONSOMMATION DES RESSOURCES
        foreach (var recipe in requiredIngredients)
        {
            if (recipe.itemNeeded == null) continue;

            for (int i = 0; i < recipe.amountNeededItem; i++)
            {
                InventorySystem.instance.RemoveItem(recipe.itemNeeded);
            }
        }

        Debug.Log("<color=green>[CONSTRUCTION] Passage validé et ressources consommées !</color>");
        if (craftSound != null && AudioPanneauDeConstruction.Instance != null)
        {
            AudioPanneauDeConstruction.Instance.PlayCraftSound(craftSound);
        }
        InventorySystem.instance.RefreshContent();
        ClosePanel();

        // 2. LOGIQUE DE CONSTRUCTION OU DE DESTRUCTION
        if (isConstructible)
        {
            if (gameObjectToActive != null)
            {
                Debug.Log("<color=green>[CONSTRUCTION] Activation du bâtiment !</color>");
                gameObjectToActive.SetActive(true);

                // SÉCURITÉ : On cherche le BuildingState sur le bâtiment activé OU sur le panneau
                if (gameObjectToActive.TryGetComponent<BuildingState>(out var building))
                {
                    building.ConstructBuilding();
                }
                else if (TryGetComponent<BuildingState>(out var panelBuilding))
                {
                    panelBuilding.ConstructBuilding();
                }
            }
        }
        else
        {
            // --- CAS DESTRUCTION (Ex: Mur à casser) ---
            if (gameObjectToDestroy != null && TryGetComponent<WorldObjectID>(out var obstacleID))
            {
                // On enregistre l'obstacle comme "Collecté/Détruit" pour qu'il ne réapparaisse jamais
                WorldStateManager.Instance.RegisterCollectedObject(obstacleID.UniqueID);

            }
        }

        // 3. NETTOYAGE DE LA SCÈNE ACTIVE
        // On détruit l'objet ciblé (l'obstacle ou le panneau lui-même)
        if (gameObjectToDestroy != null)
        {
            Destroy(gameObjectToDestroy);
        }

        // Si le panneau n'était pas l'objet à détruire direct, on le détruit quand même 
        // car il ne sert plus à rien une fois l'action faite.
        if (gameObjectToDestroy != gameObject)
        {
            Destroy(gameObject);
        }
    }

}

[System.Serializable]
public class CraftingRecipe
{
    public ItemData itemNeeded;
    public int amountNeededItem;
}