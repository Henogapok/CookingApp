using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Nutrition;

/// <summary>
/// КБЖУ и стоимость по сырым продуктам: amount × значение на 100 базовых единиц / 100.
/// Ничего не хранится — считается при каждом показе, поэтому смена формулы сразу отражается во всех рецептах.
/// </summary>
public static class NutritionCalculator
{
    /// <summary>
    /// Сколько базовых единиц в одной единице рецепта. Граммы и миллилитры считаем 1:1 (плотности пока нет):
    /// ложки и стакан — объём, для сухих продуктов это приближение.
    /// </summary>
    private static readonly Dictionary<Guid, decimal> UnitSizes = new()
    {
        [ReferenceIds.MeasurementUnits.Gram] = 1,
        [ReferenceIds.MeasurementUnits.Milliliter] = 1,
        [ReferenceIds.MeasurementUnits.Tablespoon] = 15,
        [ReferenceIds.MeasurementUnits.Teaspoon] = 5,
        [ReferenceIds.MeasurementUnits.Glass] = 250,
        [ReferenceIds.MeasurementUnits.Pinch] = 0.5m,
        [ReferenceIds.MeasurementUnits.Kilogram] = 1000,
        [ReferenceIds.MeasurementUnits.Liter] = 1000,
    };

    /// <summary>Количество в базовой единице ингредиента или null, если пересчитать нельзя.</summary>
    public static decimal? ToBaseAmount(decimal? amount, Guid? unitId, IngredientNutritionSource source)
    {
        if (amount is not { } value || unitId is not { } unit)
            return null;

        // КБЖУ в каталоге — на 100 г или 100 мл; другие базовые единицы не пересчитываем.
        if (source.BaseUnitId != ReferenceIds.MeasurementUnits.Gram && source.BaseUnitId != ReferenceIds.MeasurementUnits.Milliliter)
            return null;

        if (unit == ReferenceIds.MeasurementUnits.Piece)
            return source.PieceWeight is > 0 ? value * source.PieceWeight.Value : null;

        return UnitSizes.TryGetValue(unit, out var size) ? value * size : null;
    }

    /// <summary>
    /// Цена за 100 базовых единиц из упаковки: «520 ₸ · 1 л» → 52 ₸ за 100 мл. Упаковка пересчитывается так же,
    /// как количество в рецепте (кг, л, шт через вес штуки); не пересчитать или цены нет — null.
    /// </summary>
    public static decimal? PricePer100(ProductValues product, IngredientNutritionSource ingredient)
    {
        if (product.PackagePrice is not { } price)
            return null;

        return ToBaseAmount(product.PackageAmount, product.PackageUnitId, ingredient) is { } amount and > 0
            ? price / amount * 100
            : null;
    }

    /// <summary>КБЖУ ингредиента — оценка ИИ: в каталоге его оценил LLM, а у выбранного продукта своего КБЖУ нет.</summary>
    public static bool IsEstimatedByLlm(bool catalogEstimatedByLlm, ProductValues? product) =>
        catalogEstimatedByLlm && product?.Per100 is null;

    /// <summary>Источник для расчёта с выбранным продуктом: его КБЖУ (если указано) и цена; без продукта — каталог без цены.</summary>
    public static IngredientNutritionSource WithProduct(IngredientNutritionSource catalog, ProductValues? product) =>
        product is null
            ? catalog
            : catalog with { Per100 = product.Per100 ?? catalog.Per100, PricePer100 = PricePer100(product, catalog) };

    public static IngredientNutrition ForIngredient(IngredientAmount ingredient)
    {
        var baseAmount = ToBaseAmount(ingredient.Amount, ingredient.UnitId, ingredient.Source);

        return new IngredientNutrition(baseAmount, baseAmount is { } a ? ingredient.Source.Per100.Scale(a / 100) : null);
    }

    public static RecipeNutrition ForRecipe(IReadOnlyCollection<IngredientAmount> ingredients, int? servings)
    {
        var total = NutritionFacts.Zero;
        var counted = 0;
        var cost = 0m;
        var withoutPrice = new List<string>();
        var notCounted = new List<string>();

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Amount is null)
                continue; // «по вкусу» — осознанно не считаем

            if (ToBaseAmount(ingredient.Amount, ingredient.UnitId, ingredient.Source) is not { } baseAmount)
            {
                notCounted.Add(ingredient.Name);
                continue;
            }

            counted++;
            total += ingredient.Source.Per100.Scale(baseAmount / 100);

            if (ingredient.Source.PricePer100 is { } price)
                cost += price * baseAmount / 100;
            else
                withoutPrice.Add(ingredient.Name);
        }

        return new RecipeNutrition(
            total,
            servings is > 0 ? total.Scale(1m / servings.Value) : null,
            counted,
            cost,
            // Один ингредиент бывает в рецепте несколькими строками (сахар в тесто и в крем) — называем его один раз.
            withoutPrice.Distinct().ToList(),
            notCounted);
    }
}
