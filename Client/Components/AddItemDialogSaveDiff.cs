namespace MenuManager.Client.Components;

public record AddItemDialogSaveDiff(
    List<(int ItemId, decimal Qty)> ToAddItems,
    List<(int RecipeId, decimal Qty)> ToAddRecipes,
    List<(int MealItemId, decimal NewQty)> ToUpdate,
    List<int> ToDelete
);
