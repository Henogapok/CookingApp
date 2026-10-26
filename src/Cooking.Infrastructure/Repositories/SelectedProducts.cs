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
    /// <summary>
    /// Порядок выбора: первый — выбранный (самый поздний SelectedAt; при равенстве — по Id, чтобы выбор не «прыгал»).
    /// Им пользуются и расчёт рецептов, и экран «Мои продукты» — правило одно.
    /// </summary>
    public static IOrderedEnumerable<T> InSelectionOrder<T>(this IEnumerable<T> products, Func<T, DateTime> selectedAt, Func<T, Guid> id) =>
        products.OrderByDescending(selectedAt).ThenBy(id);

    /// <param name="ingredientIds">Только эти ингредиенты (один рецепт); null — все (список рецептов).</param>
    public static async Task<Dictionary<Guid, SelectedProduct>> LoadAsync(
        IDataContext dataContext, User user, IReadOnlyCollection<Guid>? ingredientIds, CancellationToken cancellationToken)
    {
        var query = dataContext.Products.Where(HouseholdAccess.Products(user));
        if (ingredientIds is not null)
            query = query.Where(p => ingredientIds.Contains(p.IngredientCatalogId));

        var rows = await query
            .Select(ProductRow.Projection)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(p => p.IngredientCatalogId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var p = g.InSelectionOrder(x => x.SelectedAt, x => x.Id).First();
                    return new SelectedProduct(p.Id, p.Name, p.Values);
                });
    }
}
