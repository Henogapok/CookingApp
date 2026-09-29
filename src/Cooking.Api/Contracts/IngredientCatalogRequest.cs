namespace Cooking.Api.Contracts;

public record IngredientCatalogRequest(
    string Name,
    Guid CategoryId,
    Guid BaseUnitId,
    decimal PricePer100G,
    decimal CaloriesPer100G,
    decimal ProteinPer100G,
    decimal FatPer100G,
    decimal CarbsPer100G,
    Guid CreatedBySourceId,
    Guid NutritionSourceId,
    decimal? PieceWeight); // вес 1 шт в базовой единице; null — не считается штуками
