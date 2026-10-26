namespace Cooking.Application.Products;

/// <summary>
/// Поля продукта для создания и изменения. Упаковка — как на ценнике («520 ₸ · 1 л»): цена, количество и единица
/// задаются вместе или не задаются вовсе. КБЖУ с упаковки — на 100 базовых единиц ингредиента, все четыре или ни одного
/// (тогда в расчёт идёт КБЖУ из каталога).
/// </summary>
public record ProductFields(
    Guid IngredientCatalogId,
    string Name,
    decimal? PackagePrice,
    decimal? PackageAmount,
    Guid? PackageUnitId,
    decimal? CaloriesPer100G,
    decimal? ProteinPer100G,
    decimal? FatPer100G,
    decimal? CarbsPer100G);
