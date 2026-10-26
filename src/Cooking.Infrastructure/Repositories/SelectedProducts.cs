using Cooking.Application.Common.Interfaces;
using Cooking.Application.Nutrition;
using Cooking.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

/// <summary>
/// Выбранные продукты семьи: для каждого ингредиента — видимый продукт с самым поздним SelectedAt.
/// Продуктов у семьи десятки, поэтому грузим все разом и выбираем в памяти — один запрос на весь список рецептов.
/// </summary>
internal static class SelectedProducts
{
    public static async Task<Dictionary<Guid, SelectedProduct>> LoadAsync(
        IDataContext dataContext, User user, CancellationToken cancellationToken)
    {
        var rows = await dataContext.Products
            .Where(HouseholdAccess.Products(user))
            .Select(p => new
            {
                p.Id,
                p.IngredientCatalogId,
                p.Name,
                p.SelectedAt,
                p.PackagePrice,
                p.PackageAmount,
                p.PackageUnitId,
                p.CaloriesPer100g,
                p.ProteinPer100g,
                p.FatPer100g,
                p.CarbsPer100g,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(p => p.IngredientCatalogId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    // Одинаковое время — берём по Id, чтобы выбор не «прыгал» между запросами.
                    var p = g.OrderByDescending(x => x.SelectedAt).ThenBy(x => x.Id).First();
                    var per100 = NutritionFacts.AllOrNone(p.CaloriesPer100g, p.ProteinPer100g, p.FatPer100g, p.CarbsPer100g);
                    return new SelectedProduct(p.Id, p.Name, new ProductValues(p.PackagePrice, p.PackageAmount, p.PackageUnitId, per100));
                });
    }
}
