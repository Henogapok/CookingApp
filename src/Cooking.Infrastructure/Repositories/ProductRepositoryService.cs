using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Nutrition;
using Cooking.Application.Products;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.ReferenceData;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class ProductRepositoryService(IDataContext dataContext) : IProductRepositoryService
{
    public async Task<Result<List<IngredientProductsDto>>> GetHouseholdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        // Рецептов у семьи сотни — пары «рецепт — ингредиент» считаем в памяти, а не группировкой в SQL.
        var recipeIngredients = await dataContext.Recipes
            .Where(HouseholdAccess.Recipes(user))
            .SelectMany(r => r.Ingredients.Select(i => new { RecipeId = r.Id, i.IngredientCatalogId }))
            .ToListAsync(cancellationToken);
        var recipeCounts = recipeIngredients
            .Distinct()
            .GroupBy(x => x.IngredientCatalogId)
            .ToDictionary(g => g.Key, g => g.Count());

        var products = await dataContext.Products
            .Where(HouseholdAccess.Products(user))
            .Select(p => new
            {
                p.Id,
                p.IngredientCatalogId,
                p.Name,
                p.PackagePrice,
                p.PackageAmount,
                p.PackageUnitId,
                PackageUnitAbbreviation = p.PackageUnit != null ? p.PackageUnit.Abbreviation : null,
                p.CaloriesPer100g,
                p.ProteinPer100g,
                p.FatPer100g,
                p.CarbsPer100g,
                p.SelectedAt,
            })
            .ToListAsync(cancellationToken);

        var ingredientIds = recipeCounts.Keys.Concat(products.Select(p => p.IngredientCatalogId)).Distinct().ToList();
        var ingredients = await dataContext.IngredientCatalog
            .Where(x => ingredientIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.BaseUnitId,
                BaseUnitAbbreviation = x.BaseUnit.Abbreviation,
                x.PieceWeight,
                Per100 = new NutritionFacts(x.CaloriesPer100g, x.ProteinPer100g, x.FatPer100g, x.CarbsPer100g),
                IsNutritionEstimatedByLlm = x.NutritionSourceId == ReferenceIds.DataSources.Llm,
            })
            .ToListAsync(cancellationToken);

        // Первый продукт ингредиента в этом порядке и есть выбранный — то же правило, что в расчёте рецептов.
        var productsByIngredient = products
            .InSelectionOrder(p => p.SelectedAt, p => p.Id)
            .ToLookup(p => p.IngredientCatalogId);

        var result = ingredients
            .Select(i =>
            {
                var source = new IngredientNutritionSource(i.BaseUnitId, i.PieceWeight, i.Per100, PricePer100: null);
                var selectedId = productsByIngredient[i.Id].FirstOrDefault()?.Id;

                return new IngredientProductsDto(
                    i.Id, i.Name, i.BaseUnitId, i.BaseUnitAbbreviation, i.PieceWeight, i.Per100, i.IsNutritionEstimatedByLlm,
                    recipeCounts.GetValueOrDefault(i.Id),
                    productsByIngredient[i.Id]
                        .Select(p =>
                        {
                            var per100 = NutritionFacts.AllOrNone(p.CaloriesPer100g, p.ProteinPer100g, p.FatPer100g, p.CarbsPer100g);
                            var values = new ProductValues(p.PackagePrice, p.PackageAmount, p.PackageUnitId, per100);
                            return new ProductDto(
                                p.Id, p.Name, p.PackagePrice, p.PackageAmount, p.PackageUnitId, p.PackageUnitAbbreviation,
                                NutritionCalculator.PricePer100(values, source), per100, p.Id == selectedId);
                        })
                        .ToList());
            })
            .ToList();

        return Result.Ok(result);
    }

    public async Task<Result<Guid>> CreateAsync(Guid userId, ProductFields fields, CancellationToken cancellationToken)
    {
        if (!await dataContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            return UserNotFound(userId);

        var check = await CheckIngredientAsync(fields, cancellationToken);
        if (check.IsFailed)
            return check;

        var product = new Product
        {
            CreatedByUserId = userId,
            IngredientCatalogId = fields.IngredientCatalogId,
            Name = fields.Name,
            SelectedAt = DateTime.UtcNow,
        };
        ApplyFields(product, fields);

        dataContext.Products.Add(product);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(product.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, Guid userId, ProductFields fields, CancellationToken cancellationToken)
    {
        var product = await FindVisibleAsync(id, userId, cancellationToken);
        if (product.IsFailed)
            return product.ToResult();

        if (product.Value.IngredientCatalogId != fields.IngredientCatalogId)
            return Result.Fail(new AppError("The ingredient of a product cannot be changed.", ErrorCode.Validation));

        var check = await CheckIngredientAsync(fields, cancellationToken);
        if (check.IsFailed)
            return check.ToResult();

        ApplyFields(product.Value, fields);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var product = await FindVisibleAsync(id, userId, cancellationToken);
        if (product.IsFailed)
            return product.ToResult();

        dataContext.Products.Remove(product.Value);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> SelectAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var product = await FindVisibleAsync(id, userId, cancellationToken);
        if (product.IsFailed)
            return product.ToResult();

        product.Value.SelectedAt = DateTime.UtcNow;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>Продукт автора или его семьи; чужой — NotFound, как и несуществующий.</summary>
    private async Task<Result<Product>> FindVisibleAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        var product = await dataContext.Products
            .Where(HouseholdAccess.Products(user))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null
            ? Result.Fail(new AppError($"Product with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(product);
    }

    /// <summary>Ингредиент существует, и продукт подходит к нему по правилам <see cref="ProductRules"/>.</summary>
    private async Task<Result<Guid>> CheckIngredientAsync(ProductFields fields, CancellationToken cancellationToken)
    {
        var ingredient = await dataContext.IngredientCatalog
            .Where(x => x.Id == fields.IngredientCatalogId)
            .Select(x => new { x.BaseUnitId, x.PieceWeight })
            .FirstOrDefaultAsync(cancellationToken);

        if (ingredient is null)
            return Result.Fail(new AppError(
                $"IngredientCatalog with id '{fields.IngredientCatalogId}' was not found.", ErrorCode.Validation));

        var source = new IngredientNutritionSource(ingredient.BaseUnitId, ingredient.PieceWeight, NutritionFacts.Zero, PricePer100: null);
        var product = new ProductValues(
            fields.PackagePrice, fields.PackageAmount, fields.PackageUnitId,
            NutritionFacts.AllOrNone(fields.CaloriesPer100G, fields.ProteinPer100G, fields.FatPer100G, fields.CarbsPer100G));
        if (ProductRules.Problem(product, source) is { } problem)
            return Result.Fail(new AppError(problem, ErrorCode.Validation));

        return Result.Ok(fields.IngredientCatalogId);
    }

    private static void ApplyFields(Product product, ProductFields fields)
    {
        product.Name = fields.Name;
        product.PackagePrice = fields.PackagePrice;
        product.PackageAmount = fields.PackageAmount;
        product.PackageUnitId = fields.PackageUnitId;
        product.CaloriesPer100g = fields.CaloriesPer100G;
        product.ProteinPer100g = fields.ProteinPer100G;
        product.FatPer100g = fields.FatPer100G;
        product.CarbsPer100g = fields.CarbsPer100G;
    }

    private static Result UserNotFound(Guid userId) =>
        Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));
}
