using Cooking.Application.Nutrition;
using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Tests.Nutrition;

public class NutritionCalculatorTests
{
    private static readonly Guid Gram = ReferenceIds.MeasurementUnits.Gram;
    private static readonly Guid Ml = ReferenceIds.MeasurementUnits.Milliliter;
    private static readonly Guid Piece = ReferenceIds.MeasurementUnits.Piece;

    private static IngredientNutritionSource Source(
        Guid? baseUnit = null, decimal? pieceWeight = null, decimal calories = 100, decimal price = 0) =>
        new(baseUnit ?? Gram, pieceWeight, new NutritionFacts(calories, 10, 5, 20), price);

    [Theory]
    [InlineData("00000003-0000-0000-0000-000000000001", 200, 200)]  // г
    [InlineData("00000003-0000-0000-0000-000000000002", 200, 200)]  // мл → г 1:1
    [InlineData("00000003-0000-0000-0000-000000000004", 2, 30)]     // ст.л. = 15
    [InlineData("00000003-0000-0000-0000-000000000005", 2, 10)]     // ч.л. = 5
    [InlineData("00000003-0000-0000-0000-000000000006", 1, 250)]    // стакан = 250
    [InlineData("00000003-0000-0000-0000-000000000007", 2, 1)]      // щепотка = 0.5
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
        Assert.Equal(1, result.IngredientsWithoutPrice);  // картофель без цены
    }

    [Fact]
    public void ForRecipe_UnknownServings_HasNoPerServing()
    {
        var result = NutritionCalculator.ForRecipe([new IngredientAmount("Рис", 100, Gram, Source())], servings: null);

        Assert.Null(result.PerServing);
        Assert.Equal(100, result.Total.Calories);
    }
}
