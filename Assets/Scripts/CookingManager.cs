using System.Collections.Generic;
using Inventory;
using UnityEngine;

public static class CookingManager
{
    public class Recipe
    {
        public string name;
        public int[] ingredientIDs;
        public int[] ingredientAmounts;
        public int resultItemID;
        public int resultAmount;
    }

    private static List<Recipe> recipes;

    public static List<Recipe> GetRecipes()
    {
        if (recipes == null)
        {
            recipes = new List<Recipe>();

            recipes.Add(CreateRecipe("Æ»¹û½´", new int[] { 35 }, new int[] { 2 }, 44, 1));
            recipes.Add(CreateRecipe("Êß²ËÌÀ", new int[] { 2, 4 }, new int[] { 2, 2 }, 45, 1));
            recipes.Add(CreateRecipe("¿¾Óã", new int[] { 40 }, new int[] { 1 }, 46, 1));
            recipes.Add(CreateRecipe("ÓãÌÀ", new int[] { 40, 57 }, new int[] { 1, 1 }, 58, 1));
            recipes.Add(CreateRecipe("ÉÕÓã¿é", new int[] { 56, 57 }, new int[] { 1, 2 }, 59, 1));
            recipes.Add(CreateRecipe("¹û½´Æ´ÅÌ", new int[] { 44, 50 }, new int[] { 1, 2 }, 60, 1));
        }

        return recipes;
    }

    private static Recipe CreateRecipe(string name, int[] ids, int[] amounts, int resultID, int resultAmount)
    {
        Recipe recipe = new Recipe();
        recipe.name = name;
        recipe.ingredientIDs = ids;
        recipe.ingredientAmounts = amounts;
        recipe.resultItemID = resultID;
        recipe.resultAmount = resultAmount;

        return recipe;
    }

    public static bool CanCook(Recipe recipe)
    {
        if (recipe == null || InventoryManager.Instance == null)
        {
            return false;
        }

        for (int i = 0; i < recipe.ingredientIDs.Length; i++)
        {
            if (InventoryManager.Instance.GetItemAmountInBag(recipe.ingredientIDs[i]) < recipe.ingredientAmounts[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool Cook(Recipe recipe)
    {
        if (!CanCook(recipe))
        {
            return false;
        }

        for (int i = 0; i < recipe.ingredientIDs.Length; i++)
        {
            InventoryManager.Instance.RemoveItem(recipe.ingredientIDs[i], recipe.ingredientAmounts[i]);
        }

        InventoryManager.Instance.AddItem(recipe.resultItemID, recipe.resultAmount);

        return true;
    }

    public static string GetIngredientText(Recipe recipe)
    {
        if (recipe == null || InventoryManager.Instance == null)
        {
            return string.Empty;
        }

        string text = string.Empty;

        for (int i = 0; i < recipe.ingredientIDs.Length; i++)
        {
            Item item = InventoryManager.Instance.GetItem(recipe.ingredientIDs[i]);
            string itemName = item != null ? item.itemName : recipe.ingredientIDs[i].ToString();

            if (i > 0)
            {
                text += " + ";
            }

            text += itemName + "¡Á" + recipe.ingredientAmounts[i];
        }

        return text;
    }
}