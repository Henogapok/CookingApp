using System.Linq.Expressions;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Users;

namespace Cooking.Infrastructure.Repositories;

/// <summary>
/// Что видит пользователь: своё и участников его текущей семьи. Одно правило для рецептов и продуктов —
/// вступил в семью или вышел из неё, и видимость меняется сразу, ничего не переносится.
/// </summary>
internal static class HouseholdAccess
{
    public static Expression<Func<Recipe, bool>> Recipes(User user)
    {
        var userId = user.Id;
        var familyId = user.FamilyId;

        return r => r.CreatedByUserId == userId
                    || (familyId != null && r.CreatedByUser.FamilyId == familyId);
    }

    public static Expression<Func<Product, bool>> Products(User user)
    {
        var userId = user.Id;
        var familyId = user.FamilyId;

        return p => p.CreatedByUserId == userId
                    || (familyId != null && p.CreatedByUser.FamilyId == familyId);
    }
}
