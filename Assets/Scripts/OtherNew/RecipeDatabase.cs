using System.Collections.Generic;
using UnityEngine;

public class RecipeDatabase : MonoBehaviour
{
    public static RecipeDatabase Instance;

    [Header("References")]
    [SerializeField] private AllRecipeData playerRecipesAsset; // Ton ScriptableObject AllRecipeData (la liste du joueur)
    [SerializeField] private RecipeData[] allGameRecipes;      // Glisse ICI toutes les recettes existantes de ton jeu

    // On indexe les recettes par l'ID de l'objet qu'elles fabriquent
    private Dictionary<string, RecipeData> recipeByCraftableItemID = new Dictionary<string, RecipeData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // Optionnel : si tu veux qu'il survive entre les scènes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialisation du dictionnaire de recherche rapide
        foreach (var recipe in allGameRecipes)
        {
            if (recipe != null && recipe.craftableItem != null && !string.IsNullOrEmpty(recipe.craftableItem.itemID))
            {
                if (!recipeByCraftableItemID.ContainsKey(recipe.craftableItem.itemID))
                {
                    recipeByCraftableItemID.Add(recipe.craftableItem.itemID, recipe);
                }
            }
        }
    }

    public RecipeData GetRecipeByResultItemID(string itemID)
    {
        recipeByCraftableItemID.TryGetValue(itemID, out var recipe);
        return recipe;
    }

    #region Save / Load Logic
    public RecipeSaveData GetSaveData()
    {
        RecipeSaveData data = new RecipeSaveData();

        foreach (var recipe in playerRecipesAsset.unlockedRecipes)
        {
            if (recipe != null && recipe.craftableItem != null && !string.IsNullOrEmpty(recipe.craftableItem.itemID))
            {
                // On sauvegarde l'ID de l'objet fabriqué par cette recette
                data.unlockedCraftableItemIDs.Add(recipe.craftableItem.itemID);
            }
        }

        return data;
    }

    public void LoadSaveData(RecipeSaveData data)
    {
        if (data == null || data.unlockedCraftableItemIDs == null) return;

        // On vide la liste de la session précédente pour repartir propre
        playerRecipesAsset.unlockedRecipes.Clear();

        // On recherche la recette correspondante à chaque ID d'item sauvegardé
        foreach (string itemID in data.unlockedCraftableItemIDs)
        {
            RecipeData recipe = GetRecipeByResultItemID(itemID);
            if (recipe != null)
            {
                playerRecipesAsset.unlockedRecipes.Add(recipe);
            }
        }
    }
    #endregion
}

[System.Serializable]
public class RecipeSaveData
{
    // Liste des IDs d'objets craftables dont le joueur possède la recette
    public List<string> unlockedCraftableItemIDs = new List<string>();
}