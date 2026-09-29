using Cooking.Application.Common.Errors;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.RecipeDrafts.Commands;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Users;
using Cooking.Domain.ReferenceData;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.RecipeDrafts;

/// <summary>Весь путь черновика: разбор (LLM подменён) → превью → сохранение, на EF InMemory с seed'ом справочников.</summary>
public class RecipeDraftCommandsTests
{
    private const string RecipeText = "Курица с солью: 500 г филе, соль по вкусу. Обжарить 10 минут.";

    private sealed class FakeParser(Result<ParsedRecipe> result) : IRecipeTextParser
    {
        public RecipeParsingRequest? LastRequest { get; private set; }

        public Task<Result<ParsedRecipe>> ParseAsync(RecipeParsingRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeNotifier : IRecipeDraftNotifier
    {
        public List<Guid> Ready { get; } = [];
        public List<RecipeDraftFailureReason> Failed { get; } = [];

        public Task DraftReadyAsync(Guid draftId, Guid userId, CancellationToken cancellationToken)
        {
            Ready.Add(draftId);
            return Task.CompletedTask;
        }

        public Task DraftFailedAsync(Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken)
        {
            Failed.Add(reason);
            return Task.CompletedTask;
        }
    }

    private sealed class NoopQueue : IRecipeParsingQueue
    {
        public ValueTask EnqueueAsync(RecipeParsingJob job, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private static readonly ParsedRecipe ChickenRecipe = new(
        IsRecipe: true,
        Title: "Жареная курица",
        Description: null,
        Complexity: "easy",
        Servings: null,
        ServingsEstimate: 2,
        CookingTimeMinutes: null,
        CookingTimeMinutesEstimate: 15,
        Ingredients:
        [
            new ParsedIngredient("куриное филе", 500, "g", "poultry", "g", 110, 23, 1.2m, 0, null),
            new ParsedIngredient("Соль", null, null, "spices", "g", 0, 0, 0, 0, null),
        ],
        Steps: [new ParsedStep("Обжарить курицу", 600)],
        Tags: []);

    private static async Task<(CookingDbContext Context, Guid UserId, Guid ChickenId)> CreateAsync()
    {
        var context = new CookingDbContext(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        // InMemory применяет HasData — справочники с фиксированными ReferenceIds на месте.
        await context.Database.EnsureCreatedAsync();

        var user = new User { TelegramId = 1, FirstName = "Иван" };
        var chicken = new IngredientCatalog
        {
            Name = "Куриное филе",
            CategoryId = ReferenceIds.IngredientCategories.Poultry,
            BaseUnitId = ReferenceIds.MeasurementUnits.Gram,
            CreatedBySourceId = ReferenceIds.DataSources.Manual,
            NutritionSourceId = ReferenceIds.DataSources.Manual,
        };

        context.AddRange(user, chicken);
        await context.SaveChangesAsync();

        return (context, user.Id, chicken.Id);
    }

    private static async Task<Guid> CreateDraftAsync(CookingDbContext context, Guid userId)
    {
        var result = await new CreateRecipeDraftCommandHandler(new RecipeDraftRepositoryService(context), new NoopQueue())
            .Handle(new CreateRecipeDraftCommand(userId, RecipeText), CancellationToken.None);

        return result.Value;
    }

    private static Task<Result> ParseAsync(CookingDbContext context, Guid draftId, IRecipeTextParser parser, IRecipeDraftNotifier notifier) =>
        new ParseRecipeDraftCommandHandler(
                new RecipeDraftRepositoryService(context),
                new IngredientCatalogRepositoryService(context),
                new TagRepositoryService(context),
                parser,
                notifier)
            .Handle(new ParseRecipeDraftCommand(draftId), CancellationToken.None);

    private static Task<Result<Guid>> ConfirmAsync(CookingDbContext context, Guid draftId, Guid userId) =>
        new ConfirmRecipeDraftCommandHandler(
                new RecipeDraftRepositoryService(context),
                new IngredientCatalogRepositoryService(context),
                new RecipeRepositoryService(context))
            .Handle(new ConfirmRecipeDraftCommand(draftId, userId), CancellationToken.None);

    private static void AssertError(ResultBase result, ErrorCode expected)
    {
        Assert.True(result.IsFailed);
        Assert.Equal(expected, Assert.IsType<AppError>(result.Errors[0]).Code);
    }

    [Fact]
    public async Task Parse_Recipe_SavesPreviewMatchedWithCatalogAndNotifies()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        var parser = new FakeParser(Result.Ok(ChickenRecipe));
        var notifier = new FakeNotifier();

        Assert.True((await ParseAsync(context, draftId, parser, notifier)).IsSuccess);

        Assert.Equal([draftId], notifier.Ready);
        Assert.Equal(RecipeText, parser.LastRequest!.Text);
        Assert.Contains("Куриное филе", parser.LastRequest.CatalogIngredientNames);

        var preview = (await new RecipeDraftRepositoryService(context).GetByIdAsync(draftId, userId, CancellationToken.None)).Value;
        Assert.Equal(
            [("куриное филе", (decimal?)500, (string?)"г", false), ("Соль", null, null, true)],
            preview.Ingredients.Select(i => (i.Name, i.Amount, i.UnitAbbreviation, i.IsNew)));
        Assert.Null(preview.Servings);
        Assert.True(preview.CanApplyEstimates);
    }

    [Fact]
    public async Task Parse_NotARecipe_DiscardsDraftAndNotifies()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        var notifier = new FakeNotifier();

        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe with { IsRecipe = false })), notifier);

        Assert.Equal([RecipeDraftFailureReason.NotARecipe], notifier.Failed);
        Assert.Empty(context.RecipeDrafts);
    }

    [Fact]
    public async Task Parse_ParserNotConfigured_ReportsUnavailable()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        var notifier = new FakeNotifier();
        var parser = new FakeParser(Result.Fail(new AppError("no key", ErrorCode.Unavailable)));

        await ParseAsync(context, draftId, parser, notifier);

        Assert.Equal([RecipeDraftFailureReason.ParserUnavailable], notifier.Failed);
        Assert.Empty(context.RecipeDrafts);
    }

    [Fact]
    public async Task Confirm_CreatesMissingIngredientsMarkedAsLlmAndSavesRecipe()
    {
        var (context, userId, chickenId) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), new FakeNotifier());

        var result = await ConfirmAsync(context, draftId, userId);

        Assert.True(result.IsSuccess);

        var salt = await context.IngredientCatalog.SingleAsync(i => i.Name == "Соль");
        Assert.Equal(ReferenceIds.DataSources.Llm, salt.CreatedBySourceId);
        Assert.Equal(ReferenceIds.DataSources.Llm, salt.NutritionSourceId);
        Assert.Equal(0, salt.PricePer100g);

        var recipe = (await new RecipeRepositoryService(context).GetByIdAsync(result.Value, userId, CancellationToken.None)).Value;
        Assert.Equal("Жареная курица", recipe.Title);
        Assert.Null(recipe.Servings);
        Assert.Collection(recipe.Ingredients,
            chicken =>
            {
                Assert.Equal(chickenId, chicken.IngredientCatalogId);
                Assert.Equal(500, chicken.Amount);
                Assert.False(chicken.IsNutritionEstimatedByLlm);
            },
            s =>
            {
                Assert.Equal(salt.Id, s.IngredientCatalogId);
                Assert.Null(s.Amount);
                Assert.True(s.IsNutritionEstimatedByLlm);
            });

        Assert.Empty(context.RecipeDrafts);
        AssertError(await ConfirmAsync(context, draftId, userId), ErrorCode.NotFound);
    }

    [Fact]
    public async Task Confirm_IngredientAddedToCatalogMeanwhile_ReusesIt()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), new FakeNotifier());

        var salt = new IngredientCatalog
        {
            Name = "соль",
            CategoryId = ReferenceIds.IngredientCategories.Spices,
            BaseUnitId = ReferenceIds.MeasurementUnits.Gram,
            CreatedBySourceId = ReferenceIds.DataSources.Manual,
            NutritionSourceId = ReferenceIds.DataSources.Manual,
        };
        context.IngredientCatalog.Add(salt);
        await context.SaveChangesAsync();

        Assert.True((await ConfirmAsync(context, draftId, userId)).IsSuccess);
        Assert.Equal(1, await context.IngredientCatalog.CountAsync(i => i.Name.ToLower() == "соль"));
    }

    [Fact]
    public async Task ApplyEstimates_FillsOnlyMissingValues()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe with { CookingTimeMinutes = 20 })), new FakeNotifier());
        var drafts = new RecipeDraftRepositoryService(context);

        await new ApplyRecipeDraftEstimatesCommandHandler(drafts)
            .Handle(new ApplyRecipeDraftEstimatesCommand(draftId, userId), CancellationToken.None);

        var preview = (await drafts.GetByIdAsync(draftId, userId, CancellationToken.None)).Value;
        Assert.Equal(2, preview.Servings);
        Assert.True(preview.ServingsIsEstimate);
        Assert.Equal(20, preview.CookingTimeMinutes);
        Assert.False(preview.CookingTimeIsEstimate);
        Assert.False(preview.CanApplyEstimates);
    }

    [Fact]
    public async Task Draft_IsInvisibleToOtherUsersAndConflictsWhileParsing()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var stranger = new User { TelegramId = 2, FirstName = "Пётр" };
        context.Users.Add(stranger);
        await context.SaveChangesAsync();
        var draftId = await CreateDraftAsync(context, userId);
        var drafts = new RecipeDraftRepositoryService(context);

        AssertError(await drafts.GetByIdAsync(draftId, userId, CancellationToken.None), ErrorCode.LogicConflict);

        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), new FakeNotifier());

        AssertError(await drafts.GetByIdAsync(draftId, stranger.Id, CancellationToken.None), ErrorCode.NotFound);
        AssertError(await ConfirmAsync(context, draftId, stranger.Id), ErrorCode.NotFound);
        AssertError(await drafts.DeleteAsync(draftId, stranger.Id, CancellationToken.None), ErrorCode.NotFound);
    }

    [Fact]
    public async Task ExpiredDraft_IsNotFoundAndRemovedByNextDraft()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), new FakeNotifier());

        var draft = await context.RecipeDrafts.SingleAsync();
        draft.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        AssertError(await ConfirmAsync(context, draftId, userId), ErrorCode.NotFound);

        await CreateDraftAsync(context, userId);
        Assert.DoesNotContain(context.RecipeDrafts, d => d.Id == draftId);
    }

    private static readonly ParsedRecipe EggRecipe = ChickenRecipe with
    {
        Ingredients =
        [
            // Филе есть в каталоге без веса штуки, яиц нет — оба в штуках.
            new ParsedIngredient("Куриное филе", 1, "pcs", "poultry", "g", 110, 23, 1.2m, 0, 200),
            new ParsedIngredient("Яйцо", 2, "pcs", "eggs", "g", 157, 12.7m, 11.5m, 0.7m, 55),
        ],
    };

    [Fact]
    public async Task Preview_CountsNutritionForPiecesUsingLlmPieceWeight()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(EggRecipe)), new FakeNotifier());

        var preview = (await new RecipeDraftRepositoryService(context).GetByIdAsync(draftId, userId, CancellationToken.None)).Value;

        var egg = preview.Ingredients[1];
        Assert.Equal(110, egg.BaseAmount);                  // 2 шт × 55 г
        Assert.Equal(157 * 1.1m, egg.Nutrition!.Calories);  // КБЖУ из оценки ИИ
        Assert.Equal(200, preview.Ingredients[0].BaseAmount); // вес штуки из черновика, пока в каталоге его нет
        Assert.Empty(preview.Nutrition.NotCounted);
    }

    [Fact]
    public async Task Confirm_SavesPieceWeight_ForNewAndForCatalogIngredientsWithoutIt()
    {
        var (context, userId, chickenId) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(EggRecipe)), new FakeNotifier());

        var recipeId = (await ConfirmAsync(context, draftId, userId)).Value;

        Assert.Equal(55, (await context.IngredientCatalog.SingleAsync(i => i.Name == "Яйцо")).PieceWeight);
        Assert.Equal(200, (await context.IngredientCatalog.FindAsync(chickenId))!.PieceWeight);

        var recipe = (await new RecipeRepositoryService(context).GetByIdAsync(recipeId, userId, CancellationToken.None)).Value;
        Assert.Equal(2, recipe.Nutrition.CountedIngredients);
        Assert.Equal(157 * 1.1m, recipe.Nutrition.Total.Calories); // у филе в тестовом каталоге КБЖУ = 0
    }

    [Fact]
    public async Task Confirm_DoesNotOverwriteExistingPieceWeight()
    {
        var (context, userId, chickenId) = await CreateAsync();
        await using var _ = context;
        (await context.IngredientCatalog.FindAsync(chickenId))!.PieceWeight = 250;
        await context.SaveChangesAsync();
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(EggRecipe)), new FakeNotifier());

        await ConfirmAsync(context, draftId, userId);

        Assert.Equal(250, (await context.IngredientCatalog.FindAsync(chickenId))!.PieceWeight);
    }
}
