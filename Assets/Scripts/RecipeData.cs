using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Recipe", menuName = "Recipes/New Recipe")]
public class RecipeData : ScriptableObject
{
    public ItemData craftableItem; // L'objet final qu'on obtient (ex: Épée en fer)
    public int craftableAmount = 1; // La quantité d'objets finaux obtenus après la fabrication
    // La liste de tous les ingrédients requis
    public List<CraftingRecipe> ingredients = new List<CraftingRecipe>();
}