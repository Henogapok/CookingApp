using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Nutrition;

/// <summary>КБЖУ: ккал, белки, жиры, углеводы (г).</summary>
public record NutritionFacts(decimal Calories, decimal Protein, decimal Fat, decimal Carbs)
{
    public static readonly NutritionFacts Zero = new(0, 0, 0, 0);

    public static NutritionFacts operator +(NutritionFacts a, NutritionFacts b) =>
        new(a.Calories + b.Calories, a.Protein + b.Protein, a.Fat + b.Fat, a.Carbs + b.Carbs);

    public NutritionFacts Scale(decimal factor) =>
        new(Calories * factor, Protein * factor, Fat * factor, Carbs * factor);
}

/// <summary>Данные ингредиента из каталога, нужные для расчёта. Per100 и PricePer100 — на 100 базовых единиц.</summary>
public record IngredientNutritionSource(Guid BaseUnitId, decimal? PieceWeight, NutritionFacts Per100, decimal PricePer100);

/// <summary>Строка рецепта: сколько и в чём. Amount/UnitId = null — «по вкусу».</summary>
public record IngredientAmount(string Name, decimal? Amount, Guid? UnitId, IngredientNutritionSource Source);

/// <param name="BaseAmount">Количество в базовой единице ингредиента (г или мл); null — «по вкусу» или не пересчитать.</param>
public record IngredientNutrition(decimal? BaseAmount, NutritionFacts? Nutrition);

/// <param name="PerServing">null, если число порций неизвестно.</param>
/// <param name="Cost">Стоимость ингредиентов с известной ценой, ₸.</param>
/// <param name="IngredientsWithoutPrice">Сколько учтённых ингредиентов без цены (0 в каталоге).</param>
/// <param name="NotCounted">Ингредиенты с количеством, которое не удалось перевести в г/мл (например, «шт» без веса штуки).</param>
public record RecipeNutrition(
    NutritionFacts Total,
    NutritionFacts? PerServing,
    int CountedIngredients,
    decimal Cost,
    int IngredientsWithoutPrice,
    List<string> NotCounted);

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
        var withoutPrice = 0;
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

            if (ingredient.Source.PricePer100 > 0)
                cost += ingredient.Source.PricePer100 * baseAmount / 100;
            else
                withoutPrice++;
        }

        return new RecipeNutrition(
            total,
            servings is > 0 ? total.Scale(1m / servings.Value) : null,
            counted,
            cost,
            withoutPrice,
            notCounted);
    }
}
