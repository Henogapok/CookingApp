using Cooking.Application.Recipes;

namespace Cooking.Application.Tests.Recipes;

public class RecipeFieldsValidatorTests
{
    private static readonly RecipeFieldsValidator Validator = new();

    private static RecipeFields ValidFields() => new(
        "Омлет",
        null,
        null,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Servings: 1,
        CookingTimeMinutes: 10,
        [new RecipeIngredientFields(Guid.NewGuid(), 2, Guid.NewGuid())],
        [new RecipeStepFields("Взбить яйца и пожарить", 300)],
        [Guid.NewGuid()]);

    [Fact]
    public void ValidFields_PassValidation()
    {
        Assert.True(Validator.Validate(ValidFields()).IsValid);
    }

    [Fact]
    public void EmptyLists_PassValidation()
    {
        var fields = ValidFields() with { Ingredients = [], Steps = [], TagIds = [] };

        Assert.True(Validator.Validate(fields).IsValid);
    }

    [Fact]
    public void DuplicateTags_FailValidation()
    {
        var tagId = Guid.NewGuid();

        Assert.False(Validator.Validate(ValidFields() with { TagIds = [tagId, tagId] }).IsValid);
    }

    [Fact]
    public void NonPositiveAmount_FailsValidation()
    {
        var fields = ValidFields() with { Ingredients = [new RecipeIngredientFields(Guid.NewGuid(), 0, Guid.NewGuid())] };

        Assert.False(Validator.Validate(fields).IsValid);
    }

    [Fact]
    public void ZeroServings_FailsValidation()
    {
        Assert.False(Validator.Validate(ValidFields() with { Servings = 0 }).IsValid);
    }

    [Fact]
    public void NonPositiveTimer_FailsValidation()
    {
        Assert.False(Validator.Validate(ValidFields() with { Steps = [new RecipeStepFields("Шаг", 0)] }).IsValid);
    }
}
