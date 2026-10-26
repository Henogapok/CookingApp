using Cooking.Application.Products;

namespace Cooking.Api.Contracts;

/// <summary>Продукт: упаковка как на ценнике («520 ₸ · 1 л») и, по желанию, КБЖУ с упаковки на 100 г / 100 мл.</summary>
public record ProductRequest(
    Guid IngredientCatalogId,
    string Name,
    decimal? PackagePrice,
    decimal? PackageAmount,
    Guid? PackageUnitId,
    decimal? CaloriesPer100G,
    decimal? ProteinPer100G,
    decimal? FatPer100G,
    decimal? CarbsPer100G)
{
    public ProductFields ToFields() => new(
        IngredientCatalogId, Name, PackagePrice, PackageAmount, PackageUnitId,
        CaloriesPer100G, ProteinPer100G, FatPer100G, CarbsPer100G);
}
