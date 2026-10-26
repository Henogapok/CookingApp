using Cooking.Application.Nutrition;

namespace Cooking.Application.Products;

/// <param name="PricePer100">Цена за 100 базовых единиц ингредиента, посчитанная из упаковки; null — цена не указана.</param>
/// <param name="Per100">КБЖУ с упаковки; null — в расчёт идёт КБЖУ из каталога.</param>
/// <param name="IsSelected">По этому продукту семья сейчас считает ингредиент.</param>
public record ProductDto(
    Guid Id,
    string Name,
    decimal? PackagePrice,
    decimal? PackageAmount,
    Guid? PackageUnitId,
    string? PackageUnitAbbreviation,
    decimal? PricePer100,
    NutritionFacts? Per100,
    bool IsSelected);
