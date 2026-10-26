namespace Cooking.Application.Nutrition;

/// <summary>Строка рецепта: сколько и в чём. Amount/UnitId = null — «по вкусу».</summary>
public record IngredientAmount(string Name, decimal? Amount, Guid? UnitId, IngredientNutritionSource Source);
