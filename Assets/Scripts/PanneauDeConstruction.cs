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

    private void Update()
    {
        if (craftPanel != null && craftPanel.activeInHierarchy)
        {
            if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
            {
                if (PlayerController.Instance.StateMachine.CurrentState == PlayerController.Instance.IdleState)
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
        if (craftPanel != null && !craftPanel.activeInHierarchy)
        {
            craftPanel.SetActive(true);

            if (PlayerController.Instance != null)
            {
                SetTargeted(false, PlayerController.Instance.transform);
            }

            RefreshRecipeRequirements();
        }
    }

    public void ClosePanel()
    {
        if (craftPanel != null) craftPanel.SetActive(false);

        // Sécurité UI : On désactive l'interactivité du bouton à la fermeture
        if (destroyButton != null) destroyButton.interactable = false;

        if (PlayerController.Instance != null && PlayerController.Instance.StateMachine != null)
        {
            PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    public void RefreshRecipeRequirements()
    {
        // Nettoyage des lignes UI précédentes
        foreach (Transform child in ingredientContainer)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
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

        foreach (var recipe in requiredIngredients)
        {
            if (recipe.itemNeeded == null) continue;

            for (int i = 0; i < recipe.amountNeededItem; i++)
            {
                InventorySystem.instance.RemoveItem(recipe.itemNeeded);
            }
        }

        Debug.Log("<color=green>[CONSTRUCTION] Passage validé et ressources consommées !</color>");

        InventorySystem.instance.RefreshContent();
        ClosePanel();
        if (isConstructible)
        {
            if (gameObjectToActive != null)
            {
                Debug.Log("<color=green>[CONSTRUCTION] Activation du bâtiment !</color>");
                gameObjectToActive.SetActive(true);
            }
        }
        Destroy(gameObjectToDestroy);
    }

}

[System.Serializable]
public class CraftingRecipe
{
    public ItemData itemNeeded;
    public int amountNeededItem;
}