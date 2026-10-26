namespace Cooking.Application.Nutrition;

/// <summary>Выбранный продукт ингредиента: упаковка как на ценнике и КБЖУ с упаковки (null — из каталога).</summary>
public record ProductValues(decimal? PackagePrice, decimal? PackageAmount, Guid? PackageUnitId, NutritionFacts? Per100);
