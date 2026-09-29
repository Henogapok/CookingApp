using Cooking.Application.Nutrition;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.Recipes;
using Cooking.Domain.ReferenceData;
using Cooking.Application.Tags;

namespace Cooking.Bot.Tests;

public class BotRecipeFormatterTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();

    private static RecipeDto CreateRecipe(
        string title = "Борщ",
        string? description = null,
        string? sourceUrl = null,
        int? cookingTimeMinutes = 45,
        int? servings = 4,
        List<RecipeIngredientDto>? ingredients = null,
        List<RecipeStepDto>? steps = null,
        List<TagDto>? tags = null,
        RecipeNutrition? nutrition = null) =>
        new(
            Guid.NewGuid(), title, description, sourceUrl,
            Guid.NewGuid(), "Manual",
            Guid.NewGuid(), "Easy",
            servings, cookingTimeMinutes,
            AuthorId, "Аня",
            DateTime.UtcNow, DateTime.UtcNow,
            ingredients ?? [], steps ?? [], tags ?? [],
            nutrition ?? EmptyNutrition);

    private static readonly RecipeNutrition EmptyNutrition = new RecipeNutrition(NutritionFacts.Zero, null, 0, 0, 0, []);

    private static RecipeIngredientDto Ingredient(
        string name, decimal? amount, string unit = "г", bool estimatedByLlm = false, NutritionFacts? nutrition = null) =>
        amount is null
            ? new(Guid.NewGuid(), name, null, null, null, null, estimatedByLlm, null, "г", null)
            : new(Guid.NewGuid(), name, amount, Guid.NewGuid(), unit, unit, estimatedByLlm, amount, "г", nutrition);

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
        Assert.Contains("<b>Приготовление (100 шаг.)</b>\n<blockquote expandable>1. ", card);
        Assert.Contains("</blockquote>\n…рецепт слишком длинный", card);
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

    [Fact]
    public void FormatCard_ToTasteIngredient_ShowsToTaste()
    {
        var card = BotRecipeFormatter.FormatCard(CreateRecipe(ingredients: [Ingredient("Соль", null)]), AuthorId);

        Assert.Contains("• Соль — по вкусу", card);
    }

    [Fact]
    public void FormatCard_IngredientWithLlmNutrition_IsMarkedWithLegend()
    {
        var recipe = CreateRecipe(ingredients: [Ingredient("Кунжут", 10, estimatedByLlm: true), Ingredient("Рис", 200)]);

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("• Кунжут — 10 г", card);
        Assert.Contains("• Рис — 200 г", card);
        Assert.Contains("🤖 КБЖУ оценил ИИ: 1 из 2 ингредиентов", card);
        Assert.DoesNotContain("🆕", card);
    }

    [Fact]
    public void FormatCard_UnknownServingsAndTime_AreOmitted()
    {
        var card = BotRecipeFormatter.FormatCard(CreateRecipe(cookingTimeMinutes: null, servings: null), AuthorId);

        Assert.DoesNotContain("⏱", card);
        Assert.DoesNotContain("порц.", card);
        Assert.Contains("📊 Easy", card);
    }

    private static RecipeDraftDto CreateDraft(
        int? servings = null,
        bool servingsIsEstimate = false,
        int? cookingTimeMinutes = 90,
        List<RecipeDraftIngredientDto>? ingredients = null) =>
        new(
            Guid.NewGuid(), "Плов <узбекский>", null, "Medium",
            servings, servingsIsEstimate, cookingTimeMinutes, CookingTimeIsEstimate: false,
            CanApplyEstimates: servings is null,
            IsBeingCorrected: false,
            ingredients ?? [DraftIngredient("Рис", 500, "г", isNew: false)],
            [new RecipeStepDto(1, "Обжарить мясо", null)],
            ["Ужин"],
            EmptyNutrition,
            DateTime.UtcNow.AddDays(1));

    private static RecipeDraftIngredientDto DraftIngredient(string name, decimal? amount, string? unit, bool isNew) =>
        new(name, amount, amount is null ? null : Guid.NewGuid(), unit, isNew, isNew, amount, "г", null);

    [Fact]
    public void FormatDraft_ShowsWhatIsMissingAndEstimated()
    {
        var missing = BotRecipeFormatter.FormatDraft(CreateDraft());
        var estimated = BotRecipeFormatter.FormatDraft(CreateDraft(servings: 4, servingsIsEstimate: true, cookingTimeMinutes: null));

        Assert.Contains("<b>Плов &lt;узбекский&gt;</b>", missing);
        Assert.Contains("⏱ 1 ч 30 мин", missing);
        Assert.Contains("👥 порции не указаны", missing);
        Assert.Contains("👥 4 порц. (🤖 оценка ИИ)", estimated);
        Assert.Contains("⏱ время не указано", estimated);
        Assert.EndsWith(BotRecipeFormatter.DraftCorrectionHint, missing);
    }

    [Fact]
    public void FormatDraft_NewIngredients_AreMarkedWithLegend()
    {
        var draft = CreateDraft(ingredients:
        [
            DraftIngredient("Рис", 500, "г", isNew: false),
            DraftIngredient("Зира", null, null, isNew: true),
        ]);

        var text = BotRecipeFormatter.FormatDraft(draft);

        Assert.Contains("• Рис — 500 г", text);
        Assert.Contains("• Рис — 500 г", text);
        Assert.Contains("• Зира 🆕 — по вкусу", text);
        Assert.Contains("🆕 — новый ингредиент", text);
        Assert.Contains("🤖 КБЖУ оценил ИИ: 1 из 2 ингредиентов", text);
    }

    [Fact]
    public void FormatCard_ShowsIngredientNutritionAndTotals()
    {
        var beet = new RecipeIngredientDto(
            Guid.NewGuid(), "Свёкла", 1, ReferenceIds.MeasurementUnits.Piece, "Штука", "шт", true,
            250, "г", new NutritionFacts(107.5m, 3.75m, 0.25m, 23.9m));
        var nutrition = new RecipeNutrition(
            new NutritionFacts(900, 60, 40, 80), new NutritionFacts(225, 15, 10, 20),
            CountedIngredients: 2, Cost: 1500, IngredientsWithoutPrice: 1, NotCounted: ["Зелень"]);

        var card = BotRecipeFormatter.FormatCard(CreateRecipe(ingredients: [beet], nutrition: nutrition), AuthorId);

        Assert.Contains("<b>Ингредиенты (1)</b> · <i>ккал/б/ж/у</i>\n<blockquote expandable>• Свёкла", card);
        Assert.Contains("• Свёкла — 1 шт (≈250 г) · 108/4/0/24", card);
        Assert.Contains("🔥 Всё блюдо: 900 ккал · Б 60 · Ж 40 · У 80", card);
        Assert.Contains("🍽 На порцию (из 4): 225 ккал · Б 15 · Ж 10 · У 20", card);
        Assert.Contains("⚠️ Не учтено в КБЖУ: Зелень", card);
        Assert.Contains("💰 ~1\u00a0500 ₸ (без цены: 1)", card);
    }

    [Fact]
    public void FormatCard_NoPrices_HidesCost()
    {
        var nutrition = new RecipeNutrition(new NutritionFacts(100, 1, 1, 1), null, 1, 0, 1, []);

        var card = BotRecipeFormatter.FormatCard(CreateRecipe(ingredients: [Ingredient("Рис", 100)], nutrition: nutrition), AuthorId);

        Assert.Contains("🔥 Всё блюдо: 100 ккал", card);
        Assert.DoesNotContain("На порцию", card);
        Assert.DoesNotContain("💰", card);
    }

    [Fact]
    public void FormatCard_IngredientsAndSteps_AreCollapsible_TotalsVisible()
    {
        var nutrition = new RecipeNutrition(new NutritionFacts(900, 60, 40, 80), null, 1, 0, 1, []);
        var recipe = CreateRecipe(
            ingredients: [Ingredient("Соль", 5, nutrition: NutritionFacts.Zero), Ingredient("Рис", 100, nutrition: new NutritionFacts(330, 7, 1, 72))],
            steps: [new RecipeStepDto(1, "Варить", 600)],
            nutrition: nutrition);

        var card = BotRecipeFormatter.FormatCard(recipe, AuthorId);

        Assert.Contains("<blockquote expandable>• Соль — 5 г\n• Рис — 100 г · 330/7/1/72</blockquote>", card);
        Assert.Contains("<blockquote expandable>1. Варить ⏲ 10 мин</blockquote>", card);
        Assert.DoesNotContain("🔥", card.Split("<blockquote")[0]); // КБЖУ — после ингредиентов,
        Assert.DoesNotContain("<blockquote", card.Split("🔥")[1].Split("<b>Приготовление")[0]); // но вне цитаты
    }
}
