namespace Cooking.Application.Nutrition;

/// <summary>
/// Данные ингредиента, нужные для расчёта: из каталога, а с выбранным продуктом — его КБЖУ и цена
/// (<see cref="NutritionCalculator.WithProduct"/>). Per100 и PricePer100 — на 100 базовых единиц; PricePer100 = null — цены нет.
/// </summary>
public record IngredientNutritionSource(Guid BaseUnitId, decimal? PieceWeight, NutritionFacts Per100, decimal? PricePer100);
