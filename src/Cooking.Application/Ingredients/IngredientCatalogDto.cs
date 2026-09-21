namespace Cooking.Application.Ingredients;

public record IngredientCatalogDto(
    Guid Id,
    string Name,
    Guid CategoryId,
    string CategoryName,
    Guid BaseUnitId,
    string BaseUnitName,
    string BaseUnitAbbreviation,
    decimal PricePer100G,
    decimal CaloriesPer100G,
    decimal ProteinPer100G,
    decimal FatPer100G,
    decimal CarbsPer100G,
    Guid CreatedBySourceId,
    string CreatedBySourceName,
    Guid NutritionSourceId,
    string NutritionSourceName);
