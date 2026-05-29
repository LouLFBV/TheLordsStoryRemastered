using System.Collections.Generic;
using UnityEngine;

public class PanneauDeConstruction : InteractableBase
{
    public CraftingSystem craftingSystem;

    [Header("Others")]
    [SerializeField] private GameObject craftPanel;

    [SerializeField] private RecipeData recetteDeLObject;

    public override void OnInteract(PlayerInteractor player)
    {
        OpenPanel();
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);
        Time.timeScale = 1f; 
    }

    private void OpenPanel()
    {
        if (craftPanel != null && !craftPanel.activeInHierarchy)
        {
            craftingSystem.availableRecipes = new List<RecipeData> { recetteDeLObject };
            craftingSystem.UpdateDisplayRecipes();
            if (craftingSystem.textIsRecipeListEmpty != null)
                craftingSystem.textIsRecipeListEmpty.SetActive(false);
            craftPanel.SetActive(true);
            SetTargeted(false,PlayerController.Instance.transform);
            //if (craftingSystem.uiNavigationManager != null)
            //{
            //    craftingSystem.uiNavigationManager.onCancel = craftingSystem.ClosePanel;
            //}
        }
    }
    public void ClosePanel()
    {
        craftPanel.SetActive(false);
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.Idle);
    }
}
