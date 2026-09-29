using Cooking.Application.Recipes;
using Cooking.Application.Tags;

namespace Cooking.Bot.Tests;

public class BotRecipeFormatterTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();

    private static RecipeDto CreateRecipe(
        string title = "Борщ",
        string? description = null,
        string? sourceUrl = null,
        int cookingTimeMinutes = 45,
        List<RecipeIngredientDto>? ingredients = null,
        List<RecipeStepDto>? steps = null,
        List<TagDto>? tags = null) =>
        new(
            Guid.NewGuid(), title, description, sourceUrl,
            Guid.NewGuid(), "Manual",
            Guid.NewGuid(), "Easy",
            Servings: 4, cookingTimeMinutes,
            AuthorId, "Аня",
            DateTime.UtcNow, DateTime.UtcNow,
            ingredients ?? [], steps ?? [], tags ?? []);

    private static RecipeIngredientDto Ingredient(string name, decimal amount, string unit = "г") =>
        new(Guid.NewGuid(), name, amount, Guid.NewGuid(), unit, unit);

    [Fact]
    public void FormatCard_EscapesHtmlInUserText()
    {
        var recipe = CreateRecipe(
            title: "Суп <острый>",
            description: "Tom & Jerry",
            ingredients: [Ingredient("Перец <чили>", 1, "шт")],
            steps: [new RecipeStepDto(1, "Варить <b>долго</b>", null)]);

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("<b>Суп &lt;острый&gt;</b>", card);
        Assert.Contains("Tom &amp; Jerry", card);
        Assert.Contains("Перец &lt;чили&gt;", card);
        Assert.Contains("Варить &lt;b&gt;долго&lt;/b&gt;", card);
        Assert.DoesNotContain("<острый>", card);
    }

    [Fact]
    public void FormatCard_OwnRecipe_DoesNotShowAuthor()
    {
        var card = BotRecipeFormatter.FormatCard(CreateRecipe(), AuthorId);

        Assert.DoesNotContain("Автор", card);
    }

    [Fact]
    public void FormatCard_FamilyMemberRecipe_ShowsAuthor()
    {
        var card = BotRecipeFormatter.FormatCard(CreateRecipe(), Guid.NewGuid());

        Assert.Contains("👤 Автор: Аня", card);
    }

    [Theory]
    [InlineData(45, "⏱ 45 мин")]
    [InlineData(60, "⏱ 1 ч ·")]
    [InlineData(90, "⏱ 1 ч 30 мин")]
    public void FormatCard_FormatsCookingTime(int minutes, string expected)
    {
        var card = BotRecipeFormatter.FormatCard(CreateRecipe(cookingTimeMinutes: minutes), AuthorId);

        Assert.Contains(expected, card);
    }

    [Theory]
    [InlineData(30, "⏲ 30 сек")]
    [InlineData(300, "⏲ 5 мин")]
    [InlineData(90, "⏲ 1 мин 30 сек")]
    public void FormatCard_FormatsStepTimer(int seconds, string expected)
    {
        var recipe = CreateRecipe(steps: [new RecipeStepDto(1, "Варить", seconds)]);

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("1. Варить " + expected, card);
    }

    [Fact]
    public void FormatCard_StepWithoutTimer_HasNoTimerIcon()
    {
        var recipe = CreateRecipe(steps: [new RecipeStepDto(1, "Посолить", null)]);

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("1. Посолить", card);
        Assert.DoesNotContain("⏲", card);
    }

    [Fact]
    public void FormatCard_FormatsAmountWithRussianDecimalSeparator()
    {
        var recipe = CreateRecipe(ingredients: [Ingredient("Мука", 1.5m, "ст.л."), Ingredient("Соль", 2.000m, "г")]);

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("• Мука — 1,5 ст.л.", card);
        Assert.Contains("• Соль — 2 г", card);
    }

    [Fact]
    public void FormatCard_WithSourceUrl_AddsEscapedLink()
    {
        var recipe = CreateRecipe(sourceUrl: "https://example.com/?a=1&b=2");

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("<a href=\"https://example.com/?a=1&amp;b=2\">Источник</a>", card);
    }

    [Fact]
    public void FormatCard_TooLong_TruncatesBetweenSectionsWithinTelegramLimit()
    {
        var steps = Enumerable.Range(1, 100)
            .Select(i => new RecipeStepDto(i, new string('а', 100), null))
            .ToList();
        var recipe = CreateRecipe(steps: steps, sourceUrl: "https://example.com");

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.True(card.Length <= 4096, $"Длина {card.Length}");
        Assert.EndsWith("…рецепт слишком длинный для одного сообщения", card);
        Assert.Contains("<b>Приготовление</b>", card);
        Assert.DoesNotContain("100. ", card);
        Assert.DoesNotContain("Источник", card);
    }

    [Fact]
    public void FormatListButton_OwnRecipe_ShowsTitleOnly()
    {
        var recipe = new RecipeSummaryDto(Guid.NewGuid(), "Борщ", "Easy", 4, 45, AuthorId, "Аня");

        Assert.Equal("Борщ", BotRecipeFormatter.FormatListButton(recipe, AuthorId));
    }

    [Fact]
    public void FormatListButton_FamilyMemberRecipe_AppendsAuthor()
    {
        var recipe = new RecipeSummaryDto(Guid.NewGuid(), "Борщ", "Easy", 4, 45, AuthorId, "Аня");

        Assert.Equal("Борщ · Аня", BotRecipeFormatter.FormatListButton(recipe, Guid.NewGuid()));
    }
}
