using Cooking.Application.Nutrition;
using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Tests.Nutrition;

public class NutritionCalculatorTests
{
    private static readonly Guid Gram = ReferenceIds.MeasurementUnits.Gram;
    private static readonly Guid Ml = ReferenceIds.MeasurementUnits.Milliliter;
    private static readonly Guid Piece = ReferenceIds.MeasurementUnits.Piece;

    private static IngredientNutritionSource Source(
        Guid? baseUnit = null, decimal? pieceWeight = null, decimal calories = 100, decimal? price = null) =>
        new(baseUnit ?? Gram, pieceWeight, new NutritionFacts(calories, 10, 5, 20), price);

    [Theory]
    [InlineData("00000003-0000-0000-0000-000000000001", 200, 200)]  // г
    [InlineData("00000003-0000-0000-0000-000000000002", 200, 200)]  // мл → г 1:1
    [InlineData("00000003-0000-0000-0000-000000000004", 2, 30)]     // ст.л. = 15
    [InlineData("00000003-0000-0000-0000-000000000005", 2, 10)]     // ч.л. = 5
    [InlineData("00000003-0000-0000-0000-000000000006", 1, 250)]    // стакан = 250
    [InlineData("00000003-0000-0000-0000-000000000007", 2, 1)]      // щепотка = 0.5
    [InlineData("00000003-0000-0000-0000-000000000008", 1.5, 1500)] // кг
    [InlineData("00000003-0000-0000-0000-000000000009", 1, 1000)]   // л
    public void ToBaseAmount_ConvertsUnits(string unitId, double amount, double expected)
    {
        Assert.Equal((decimal)expected, NutritionCalculator.ToBaseAmount((decimal)amount, Guid.Parse(unitId), Source()));
    }

    [Fact]
    public void ToBaseAmount_Pieces_UseCatalogPieceWeight()
    {
        Assert.Equal(300, NutritionCalculator.ToBaseAmount(2, Piece, Source(pieceWeight: 150)));
        Assert.Null(NutritionCalculator.ToBaseAmount(2, Piece, Source(pieceWeight: null)));
    }

    [Fact]
    public void ToBaseAmount_ToTasteOrUnknownUnit_IsNull()
    {
        Assert.Null(NutritionCalculator.ToBaseAmount(null, null, Source()));
        Assert.Null(NutritionCalculator.ToBaseAmount(5, Guid.NewGuid(), Source()));
        Assert.Null(NutritionCalculator.ToBaseAmount(5, Gram, Source(baseUnit: Piece))); // КБЖУ не на 100 г/мл
    }

    [Fact]
    public void ForIngredient_ScalesPer100ToAmount()
    {
        var result = NutritionCalculator.ForIngredient(new IngredientAmount("Молоко", 2, ReferenceIds.MeasurementUnits.Glass, Source(baseUnit: Ml)));

        Assert.Equal(500, result.BaseAmount);
        Assert.Equal(new NutritionFacts(500, 50, 25, 100), result.Nutrition);
    }

    [Fact]
    public void ForRecipe_SumsCountedIngredients_ListsNotCounted_SkipsToTaste()
    {
        var ingredients = new[]
        {
            new IngredientAmount("Говядина", 500, Gram, Source(calories: 200, price: 400)),
            new IngredientAmount("Картофель", 2, Piece, Source(pieceWeight: 150, calories: 80)),
            new IngredientAmount("Свёкла", 1, Piece, Source(pieceWeight: null)),
            new IngredientAmount("Соль", null, null, Source()),
        };

        var result = NutritionCalculator.ForRecipe(ingredients, servings: 4);

        // 500 г × 200/100 + 300 г × 80/100
        Assert.Equal(1000 + 240, result.Total.Calories);
        Assert.Equal(result.Total.Calories / 4, result.PerServing!.Calories);
        Assert.Equal(2, result.CountedIngredients);
        Assert.Equal(["Свёкла"], result.NotCounted);
        Assert.Equal(2000, result.Cost);                 // 500 г × 400 ₸ / 100
        Assert.Equal(["Картофель"], result.IngredientsWithoutPrice);
    }

    [Fact]
    public void ForRecipe_UnknownServings_HasNoPerServing()
    {
        var result = NutritionCalculator.ForRecipe([new IngredientAmount("Рис", 100, Gram, Source())], servings: null);

        Assert.Null(result.PerServing);
        Assert.Equal(100, result.Total.Calories);
    }

    [Fact]
    public void PricePer100_FromPackage()
    {
        // 520 ₸ · 1 л → 52 ₸ за 100 мл
        Assert.Equal(52, NutritionCalculator.PricePer100(
            new ProductValues(520, 1, ReferenceIds.MeasurementUnits.Liter, null), Source(baseUnit: Ml)));
        // 1 290 ₸ · 900 г → 143,33 ₸ за 100 г
        Assert.Equal(143.33m, Math.Round(NutritionCalculator.PricePer100(
            new ProductValues(1290, 900, Gram, null), Source())!.Value, 2));
        // 950 ₸ · 10 шт по 55 г → 172,73 ₸ за 100 г
        Assert.Equal(172.73m, Math.Round(NutritionCalculator.PricePer100(
            new ProductValues(950, 10, Piece, null), Source(pieceWeight: 55))!.Value, 2));
    }

    [Fact]
    public void PricePer100_NoPriceOrPiecesWithoutWeight_IsNull()
    {
        Assert.Null(NutritionCalculator.PricePer100(new ProductValues(null, null, null, null), Source()));
        Assert.Null(NutritionCalculator.PricePer100(new ProductValues(950, 10, Piece, null), Source(pieceWeight: null)));
    }

    [Fact]
    public void WithProduct_UsesProductPriceAndOwnNutrition()
    {
        var own = new NutritionFacts(58, 2.9m, 3.2m, 4.7m);

        var source = NutritionCalculator.WithProduct(
            Source(baseUnit: Ml), new ProductValues(520, 1, ReferenceIds.MeasurementUnits.Liter, own));

        Assert.Equal(own, source.Per100);
        Assert.Equal(52, source.PricePer100);
    }

    [Fact]
    public void WithProduct_WithoutOwnNutrition_KeepsCatalogNutrition()
    {
        var catalog = Source(calories: 345);

        var source = NutritionCalculator.WithProduct(catalog, new ProductValues(1500, 1, ReferenceIds.MeasurementUnits.Kilogram, null));

        Assert.Equal(catalog.Per100, source.Per100);
        Assert.Equal(150, source.PricePer100);
    }

    [Fact]
    public void WithProduct_NoProduct_NoPrice()
    {
        var result = NutritionCalculator.ForRecipe(
            [new IngredientAmount("Морковь", 500, Gram, NutritionCalculator.WithProduct(Source(), null))], servings: 2);

        Assert.Equal(0, result.Cost);
        Assert.Equal(["Морковь"], result.IngredientsWithoutPrice);
    }
}
