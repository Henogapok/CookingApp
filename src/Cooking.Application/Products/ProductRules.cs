using Cooking.Application.Nutrition;
using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Products;

/// <summary>
/// Правила продукта, которым нужен его ингредиент (единица, вес штуки). Проверяются и при сохранении продукта,
/// и при правке каталога: сменили единицу или вес штуки — продукты семей не должны стать неверными.
/// </summary>
public static class ProductRules
{
    /// <summary>Что не так с продуктом для этого ингредиента; null — всё в порядке.</summary>
    public static string? Problem(ProductValues product, IngredientNutritionSource ingredient) =>
        PackageProblem(product, ingredient) ?? NutritionProblem(product, ingredient);

    /// <summary>Упаковку нельзя перевести в базовую единицу ингредиента — цену за 100 не посчитать.</summary>
    private static string? PackageProblem(ProductValues product, IngredientNutritionSource ingredient)
    {
        if (product.PackageAmount is null)
            return null;

        if (ingredient.BaseUnitId != ReferenceIds.MeasurementUnits.Gram && ingredient.BaseUnitId != ReferenceIds.MeasurementUnits.Milliliter)
            return "Prices are supported only for ingredients measured in grams or milliliters.";

        return NutritionCalculator.ToBaseAmount(product.PackageAmount, product.PackageUnitId, ingredient) is null
            ? "A package in pieces needs the ingredient's piece weight in the catalog."
            : null;
    }

    /// <summary>Б+Ж+У на 100 г не больше 100 г; на 100 мл может быть больше (мёд, сиропы) — их не ограничиваем.</summary>
    private static string? NutritionProblem(ProductValues product, IngredientNutritionSource ingredient) =>
        ingredient.BaseUnitId == ReferenceIds.MeasurementUnits.Gram
        && product.Per100 is { } n && n.Protein + n.Fat + n.Carbs > 100
            ? "Protein, fat and carbs per 100 g cannot exceed 100 g together."
            : null;
}
