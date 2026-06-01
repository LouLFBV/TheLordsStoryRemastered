using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllRecipeData", menuName = "ScriptableObjects/AllRecipeData", order = 1)]
public class AllRecipeData : ScriptableObject
{
    public List<RecipeData> unlockedRecipes = new List<RecipeData>();
}