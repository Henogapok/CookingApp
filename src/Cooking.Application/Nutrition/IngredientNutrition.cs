namespace Cooking.Application.Nutrition;

/// <param name="BaseAmount">Количество в базовой единице ингредиента (г или мл); null — «по вкусу» или не пересчитать.</param>
public record IngredientNutrition(decimal? BaseAmount, NutritionFacts? Nutrition);
