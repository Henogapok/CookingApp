using System.Linq.Expressions;
using Cooking.Application.Nutrition;
using Cooking.Domain.Entities.Ingredients;

namespace Cooking.Infrastructure.Repositories;

/// <summary>Продукт, прочитанный из базы для расчётов и списков: одна проекция вместо копий в каждом репозитории.</summary>
internal sealed record ProductRow(
    Guid Id,
    Guid IngredientCatalogId,
    string Name,
    decimal? PackagePrice,
    decimal? PackageAmount,
    Guid? PackageUnitId,
    string? PackageUnitAbbreviation,
    decimal? CaloriesPer100g,
    decimal? ProteinPer100g,
    decimal? FatPer100g,
    decimal? CarbsPer100g,
    DateTime SelectedAt)
{
    public static readonly Expression<Func<Product, ProductRow>> Projection = p => new ProductRow(
        p.Id,
        p.IngredientCatalogId,
        p.Name,
        p.PackagePrice,
        p.PackageAmount,
        p.PackageUnitId,
        p.PackageUnit != null ? p.PackageUnit.Abbreviation : null,
        p.CaloriesPer100g,
        p.ProteinPer100g,
        p.FatPer100g,
        p.CarbsPer100g,
        p.SelectedAt);

    /// <summary>КБЖУ с упаковки; null — считается по каталогу.</summary>
    public NutritionFacts? Per100 => NutritionFacts.AllOrNone(CaloriesPer100g, ProteinPer100g, FatPer100g, CarbsPer100g);

    public ProductValues Values => new(PackagePrice, PackageAmount, PackageUnitId, Per100);
}
