namespace Cooking.Application.Nutrition;

/// <param name="PerServing">null, если число порций неизвестно.</param>
/// <param name="Cost">Стоимость ингредиентов с известной ценой, ₸.</param>
/// <param name="IngredientsWithoutPrice">Учтённые ингредиенты без цены: нет выбранного продукта или у продукта не указана цена.</param>
/// <param name="NotCounted">Ингредиенты с количеством, которое не удалось перевести в г/мл (например, «шт» без веса штуки).</param>
public record RecipeNutrition(
    NutritionFacts Total,
    NutritionFacts? PerServing,
    int CountedIngredients,
    decimal Cost,
    List<string> IngredientsWithoutPrice,
    List<string> NotCounted);
