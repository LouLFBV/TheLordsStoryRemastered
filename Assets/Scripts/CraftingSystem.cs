using UnityEngine;
using System.Collections.Generic;

public class CraftingSystem : MonoBehaviour
{
    //public List<RecipeData> availableRecipes;

    //[SerializeField]
    //private GameObject recipeUiPrefab;

    //[SerializeField]
    //private Transform recipesParent;

    //public GameObject craftPanel;
    //public GameObject textIsRecipeListEmpty;

    ////public UINavigationManager uiNavigationManager;
    //void Start()
    //{
    //    UpdateDisplayRecipes();
    //}
    //public void UpdateDisplayRecipes()
    //{
    //    foreach (Transform child in recipesParent)
    //    {
    //        Destroy(child.gameObject);
    //    }
    //    for (int i = 0; i < availableRecipes.Count; i++)
    //    {
    //        GameObject currentRecipe = Instantiate(recipeUiPrefab, recipesParent);
    //        Recipe recipe = currentRecipe.GetComponent<Recipe>();
    //        recipe.Configure(availableRecipes[i]);
    //        recipe.craftingSystem = this;
    //        //uiNavigationManager.elements.Clear();
    //        //uiNavigationManager.elements.Add(recipe.craftableItemImageGO);
    //        //uiNavigationManager.elements.Add(recipe.craftButtonGO);
    //    }
    //}

    //public void ClosePanel()
    //{
    //    //TooltipSystem.instance.Hide();
    //    craftPanel.SetActive(false);
    //    //if (uiNavigationManager != null)
    //    //{
    //    //    uiNavigationManager.onCancel = null;
    //    //}
    //}
}
