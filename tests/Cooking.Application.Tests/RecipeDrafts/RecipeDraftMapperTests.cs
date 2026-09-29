using Cooking.Application.RecipeDrafts;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Application.Recipes;
using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Tests.RecipeDrafts;

public class RecipeDraftMapperTests
{
    private static readonly Guid ChickenId = Guid.NewGuid();
    private static readonly Guid DinnerTagId = Guid.NewGuid();

    private static readonly Dictionary<string, Guid> Catalog = new() { [RecipeDraftMapper.NameKey("Куриное филе")] = ChickenId };
    private static readonly Dictionary<string, Guid> Tags = new() { [RecipeDraftMapper.NameKey("Ужин")] = DinnerTagId };

    private static ParsedIngredient Ingredient(string name, decimal? amount = 100, string? unit = "g") =>
        new(name, amount, unit, "vegetables", "g", 40, 1.4m, 0.2m, 8.2m, null);

    private static ParsedRecipe Parsed(
        List<ParsedIngredient>? ingredients = null,
        List<ParsedStep>? steps = null,
        List<string>? tags = null,
        bool isRecipe = true,
        string title = "Курица с луком") =>
        new(
            isRecipe,
            title,
            Description: "  ",
            Complexity: "easy",
            Servings: null,
            ServingsEstimate: 2,
            CookingTimeMinutes: 30,
            CookingTimeMinutesEstimate: 35,
            ingredients ?? [Ingredient("Куриное филе", 500)],
            steps ?? [new ParsedStep("Обжарить курицу", 600)],
            tags ?? []);

    private static RecipeDraftContent Map(ParsedRecipe parsed) =>
        RecipeDraftMapper.ToDraftContent(parsed, Catalog, Tags) ?? throw new InvalidOperationException("Expected a recipe");

    [Theory]
    [InlineData("Куриное филе")]
    [InlineData("  куриное   ФИЛЕ ")]
    public void ToDraftContent_IngredientFromCatalog_ReferencesCatalogIgnoringCaseAndSpaces(string name)
    {
        var ingredient = Assert.Single(Map(Parsed([Ingredient(name, 500)])).Ingredients);

        Assert.Equal(ChickenId, ingredient.IngredientCatalogId);
        Assert.Null(ingredient.NewIngredient);
        Assert.Equal(500, ingredient.Amount);
        Assert.Equal(ReferenceIds.MeasurementUnits.Gram, ingredient.UnitId);
    }

    [Fact]
    public void ToDraftContent_NewIngredient_KeepsLlmCategoryUnitAndNutrition()
    {
        var parsed = Parsed([new ParsedIngredient("Молоко", 200, "ml", "dairy", "ml", 52, 2.8m, 2.5m, 4.7m, null)]);

        var ingredient = Assert.Single(Map(parsed).Ingredients);

        Assert.Null(ingredient.IngredientCatalogId);
        Assert.Equal(
            new NewIngredientDraft(ReferenceIds.IngredientCategories.Dairy, ReferenceIds.MeasurementUnits.Milliliter, 52, 2.8m, 2.5m, 4.7m),
            ingredient.NewIngredient);
        Assert.Equal(ReferenceIds.MeasurementUnits.Milliliter, ingredient.UnitId);
    }

    [Fact]
    public void ToDraftContent_YoAndYe_AreTheSameIngredient()
    {
        var catalog = new Dictionary<string, Guid> { [RecipeDraftMapper.NameKey("Свёкла")] = ChickenId };

        var content = RecipeDraftMapper.ToDraftContent(Parsed([Ingredient("свекла")]), catalog, Tags)!;

        Assert.Equal(ChickenId, Assert.Single(content.Ingredients).IngredientCatalogId);
    }

    [Theory]
    [InlineData(null, null)]  // «по вкусу»
    [InlineData(null, "g")]   // единица без количества
    [InlineData(5.0, null)]   // количество без единицы
    [InlineData(0.0, "g")]    // нулевое количество
    [InlineData(5.0, "ложка")] // единица не из списка
    public void ToDraftContent_AmountOrUnitMissing_BecomesToTaste(double? amount, string? unit)
    {
        var ingredient = Assert.Single(Map(Parsed([Ingredient("Соль", (decimal?)amount, unit)])).Ingredients);

        Assert.Null(ingredient.Amount);
        Assert.Null(ingredient.UnitId);
    }

    [Fact]
    public void ToDraftContent_Tags_KeepsOnlyKnownOnesWithoutDuplicates()
    {
        var content = Map(Parsed(tags: ["ужин", "Ужин", "Праздничное"]));

        Assert.Equal([DinnerTagId], content.TagIds);
    }

    [Fact]
    public void ToDraftContent_KeepsTextValuesSeparateFromEstimates()
    {
        var content = Map(Parsed());

        Assert.Null(content.Servings);
        Assert.Equal(2, content.ServingsEstimate);
        Assert.Equal(30, content.CookingTimeMinutes);
        Assert.False(content.UseEstimates);
        Assert.Null(content.EffectiveServings);
        Assert.True(content.CanApplyEstimates);
    }

    [Fact]
    public void ToDraftContent_CleansUpStepsAndDescription()
    {
        var content = Map(Parsed(steps: [new ParsedStep("  ", 60), new ParsedStep("Посолить", 0), new ParsedStep("Варить", 600)]));

        Assert.Null(content.Description);
        Assert.Equal([new RecipeStepFields("Посолить", null), new RecipeStepFields("Варить", 600)], content.Steps);
        Assert.Equal(ReferenceIds.Complexities.Easy, content.ComplexityId);
    }

    [Fact]
    public void ToDraftContent_NotARecipe_ReturnsNull()
    {
        Assert.Null(RecipeDraftMapper.ToDraftContent(Parsed(isRecipe: false), Catalog, Tags));
        Assert.Null(RecipeDraftMapper.ToDraftContent(Parsed(title: " "), Catalog, Tags));
        Assert.Null(RecipeDraftMapper.ToDraftContent(Parsed(ingredients: [], steps: []), Catalog, Tags));
    }

    [Fact]
    public void ToRecipeFields_ResolvesNewIngredientsByNameAndKeepsToTaste()
    {
        var content = Map(Parsed([Ingredient("Куриное филе", 500), Ingredient("Соль", null, null)]));
        var saltId = Guid.NewGuid();
        var catalog = new Dictionary<string, Guid>(Catalog) { [RecipeDraftMapper.NameKey("соль")] = saltId };

        var fields = RecipeDraftMapper.ToRecipeFields(content, catalog);

        Assert.Equal(
            [
                new RecipeIngredientFields(ChickenId, 500, ReferenceIds.MeasurementUnits.Gram),
                new RecipeIngredientFields(saltId, null, null),
            ],
            fields.Ingredients);
        Assert.Equal(ReferenceIds.SourceTypes.Manual, fields.SourceTypeId);
    }

    [Fact]
    public void ToRecipeFields_UsesEstimatesOnlyWhenRequested()
    {
        var content = Map(Parsed());

        Assert.Null(RecipeDraftMapper.ToRecipeFields(content, Catalog).Servings);

        var withEstimates = RecipeDraftMapper.ToRecipeFields(content with { UseEstimates = true }, Catalog);

        Assert.Equal(2, withEstimates.Servings);
        Assert.Equal(30, withEstimates.CookingTimeMinutes); // значение из текста оценка не перетирает
    }

    [Fact]
    public void ToCorrectionJson_UsesLlmCodesAndTagNames()
    {
        var content = Map(Parsed([Ingredient("Куриное филе", 500), Ingredient("Соль", null, null)], tags: ["Ужин"]));

        var json = RecipeDraftMapper.ToCorrectionJson(content, new Dictionary<Guid, string> { [DinnerTagId] = "Ужин" });

        Assert.Contains("\"name\":\"Куриное филе\",\"amount\":500,\"unit\":\"g\"", json);
        Assert.Contains("\"name\":\"Соль\",\"amount\":null,\"unit\":null", json);
        Assert.Contains("\"complexity\":\"easy\"", json);
        Assert.Contains("\"tags\":[\"Ужин\"]", json);
    }
}
