using Cooking.Application.Common.Errors;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.RecipeDrafts.Commands;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Application.RecipeDrafts.Sources;
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

        public List<RecipeDraftFailureReason> CorrectionFailed { get; } = [];

        public Task DraftCorrectionFailedAsync(Guid draftId, Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken)
        {
            CorrectionFailed.Add(reason);
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

    private static Task<Result> ParseAsync(
        CookingDbContext context, Guid draftId, IRecipeTextParser parser, IRecipeDraftNotifier notifier,
        IVideoSourceLoader? videoLoader = null, ISpeechToText? speechToText = null) =>
        new ParseRecipeDraftCommandHandler(
                new RecipeDraftRepositoryService(context),
                new IngredientCatalogRepositoryService(context),
                new TagRepositoryService(context),
                parser,
                notifier,
                videoLoader ?? new FakeVideoLoader(Result.Fail(new AppError("not used", ErrorCode.ExternalService))),
                speechToText ?? new FakeSpeechToText(Result.Ok("")))
            .Handle(new ParseRecipeDraftCommand(draftId), CancellationToken.None);

    private sealed class FakeVideoLoader(Result<VideoSource> result) : IVideoSourceLoader
    {
        public string? LastUrl { get; private set; }

        public Task<Result<VideoSource>> LoadAsync(string url, CancellationToken cancellationToken)
        {
            LastUrl = url;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeSpeechToText(Result<string> result) : ISpeechToText
    {
        public string? LastFile { get; private set; }

        public Task<Result<string>> TranscribeAsync(string mediaFilePath, CancellationToken cancellationToken)
        {
            LastFile = mediaFilePath;
            return Task.FromResult(result);
        }
    }

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

    private static Task<Result> CorrectAsync(CookingDbContext context, Guid draftId, Guid userId, string text) =>
        new CorrectRecipeDraftCommandHandler(new RecipeDraftRepositoryService(context), new NoopQueue())
            .Handle(new CorrectRecipeDraftCommand(draftId, userId, text), CancellationToken.None);

    private static async Task<(CookingDbContext Context, Guid UserId, Guid DraftId)> CreateParsedDraftAsync()
    {
        var (context, userId, _) = await CreateAsync();
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), new FakeNotifier());
        return (context, userId, draftId);
    }

    [Fact]
    public async Task Correction_SendsCurrentVersionAndCorrectionToLlm_AndReplacesContent()
    {
        var (context, userId, draftId) = await CreateParsedDraftAsync();
        await using var _ = context;
        await new ApplyRecipeDraftEstimatesCommandHandler(new RecipeDraftRepositoryService(context))
            .Handle(new ApplyRecipeDraftEstimatesCommand(draftId, userId), CancellationToken.None);

        Assert.True((await CorrectAsync(context, draftId, userId, "соли не надо")).IsSuccess);

        var corrected = ChickenRecipe with { Ingredients = [ChickenRecipe.Ingredients[0]] };
        var parser = new FakeParser(Result.Ok(corrected));
        var notifier = new FakeNotifier();
        await ParseAsync(context, draftId, parser, notifier);

        Assert.Equal("соли не надо", parser.LastRequest!.Correction);
        Assert.Contains("\"name\":\"Соль\"", parser.LastRequest.CurrentRecipeJson);
        Assert.Equal([draftId], notifier.Ready);

        var preview = (await new RecipeDraftRepositoryService(context).GetByIdAsync(draftId, userId, CancellationToken.None)).Value;
        Assert.Equal(["куриное филе"], preview.Ingredients.Select(i => i.Name));
        Assert.False(preview.IsBeingCorrected);
        Assert.True(preview.ServingsIsEstimate); // «Оценить» не слетает после правки
    }

    [Fact]
    public async Task Correction_WhilePending_BlocksSaveAndSecondCorrection_ButPreviewIsVisible()
    {
        var (context, userId, draftId) = await CreateParsedDraftAsync();
        await using var _ = context;

        await CorrectAsync(context, draftId, userId, "соли не надо");

        AssertError(await ConfirmAsync(context, draftId, userId), ErrorCode.LogicConflict);
        AssertError(await CorrectAsync(context, draftId, userId, "и перца"), ErrorCode.LogicConflict);

        var preview = await new RecipeDraftRepositoryService(context).GetByIdAsync(draftId, userId, CancellationToken.None);
        Assert.True(preview.Value.IsBeingCorrected);
    }

    [Fact]
    public async Task Correction_Failed_KeepsPreviousVersionAndNotifies()
    {
        var (context, userId, draftId) = await CreateParsedDraftAsync();
        await using var _ = context;
        await CorrectAsync(context, draftId, userId, "соли не надо");
        var notifier = new FakeNotifier();

        await ParseAsync(context, draftId, new FakeParser(Result.Fail(new AppError("timeout", ErrorCode.ExternalService))), notifier);

        Assert.Equal([RecipeDraftFailureReason.ParserError], notifier.CorrectionFailed);
        Assert.Empty(notifier.Failed);
        var preview = (await new RecipeDraftRepositoryService(context).GetByIdAsync(draftId, userId, CancellationToken.None)).Value;
        Assert.Equal(2, preview.Ingredients.Count);
        Assert.False(preview.IsBeingCorrected);
        Assert.True((await ConfirmAsync(context, draftId, userId)).IsSuccess);
    }

    [Fact]
    public async Task Correction_OfUnparsedOrForeignDraft_IsRejected()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = await CreateDraftAsync(context, userId);

        AssertError(await CorrectAsync(context, draftId, userId, "соли не надо"), ErrorCode.LogicConflict);
        AssertError(await CorrectAsync(context, draftId, Guid.NewGuid(), "соли не надо"), ErrorCode.NotFound);
    }

    private static async Task<Guid> SaveChickenRecipeAsync(CookingDbContext context, Guid userId)
    {
        var draftId = await CreateDraftAsync(context, userId);
        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), new FakeNotifier());
        return (await ConfirmAsync(context, draftId, userId)).Value;
    }

    private static Task<Result<Guid>> EditAsync(CookingDbContext context, Guid recipeId, Guid userId, string text) =>
        new EditRecipeCommandHandler(new RecipeRepositoryService(context), new RecipeDraftRepositoryService(context), new NoopQueue())
            .Handle(new EditRecipeCommand(recipeId, userId, text), CancellationToken.None);

    [Fact]
    public async Task EditRecipe_CorrectsThroughLlmAndUpdatesSameRecipeKeepingSource()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var recipeId = await SaveChickenRecipeAsync(context, userId);
        var saved = await context.Recipes.SingleAsync();
        saved.SourceUrl = "https://instagram.com/reel/1";
        saved.SourceTypeId = ReferenceIds.SourceTypes.Instagram;
        await context.SaveChangesAsync();

        var draftId = (await EditAsync(context, recipeId, userId, "соли не надо, порций 3")).Value;

        var parser = new FakeParser(Result.Ok(ChickenRecipe with { Servings = 3, Ingredients = [ChickenRecipe.Ingredients[0]] }));
        var notifier = new FakeNotifier();
        await ParseAsync(context, draftId, parser, notifier);

        Assert.Equal("соли не надо, порций 3", parser.LastRequest!.Correction);
        Assert.Contains("Соль", parser.LastRequest.CurrentRecipeJson); // LLM видит текущую версию рецепта
        Assert.Equal([draftId], notifier.Ready);
        Assert.Equal(recipeId, (await new RecipeDraftRepositoryService(context).GetByIdAsync(draftId, userId, CancellationToken.None)).Value.RecipeId);

        Assert.Equal(recipeId, (await ConfirmAsync(context, draftId, userId)).Value);

        var recipe = (await new RecipeRepositoryService(context).GetByIdAsync(recipeId, userId, CancellationToken.None)).Value;
        Assert.Equal(1, await context.Recipes.CountAsync());
        Assert.Equal(3, recipe.Servings);
        Assert.Equal(["Куриное филе"], recipe.Ingredients.Select(i => i.IngredientName)); // имя — из каталога
        Assert.Equal("https://instagram.com/reel/1", recipe.SourceUrl);
        Assert.Equal(ReferenceIds.SourceTypes.Instagram, recipe.SourceTypeId);
    }

    [Fact]
    public async Task EditRecipe_FamilyMemberCan_StrangerCannot()
    {
        var (context, authorId, _) = await CreateAsync();
        await using var _ = context;
        var family = new Family { Name = "Семья" };
        var author = await context.Users.FindAsync(authorId);
        author!.Family = family;
        var member = new User { TelegramId = 2, FirstName = "Маша", Family = family };
        var stranger = new User { TelegramId = 3, FirstName = "Пётр" };
        context.Users.AddRange(member, stranger);
        await context.SaveChangesAsync();
        var recipeId = await SaveChickenRecipeAsync(context, authorId);

        Assert.True((await EditAsync(context, recipeId, member.Id, "порций 3")).IsSuccess);
        AssertError(await EditAsync(context, recipeId, stranger.Id, "порций 3"), ErrorCode.NotFound);
    }

    private static Task<Result<Guid>> CreateFromUrlAsync(CookingDbContext context, Guid userId, string url) =>
        new CreateRecipeDraftFromUrlCommandHandler(new RecipeDraftRepositoryService(context), new NoopQueue())
            .Handle(new CreateRecipeDraftFromUrlCommand(userId, url), CancellationToken.None);

    private static string TempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cooking-test-{Guid.NewGuid():N}.m4a");
        File.WriteAllText(path, "audio");
        return path;
    }

    [Fact]
    public async Task FromUrl_CombinesCaptionAndTranscript_AndSavesRecipeWithInstagramSource()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = (await CreateFromUrlAsync(context, userId, "смотри https://www.instagram.com/reel/DaSFnBZsJLo/?igsh=abc")).Value;
        var audio = TempFile();
        var loader = new FakeVideoLoader(Result.Ok(new VideoSource("Филе 500 г", audio)));
        var speech = new FakeSpeechToText(Result.Ok("Обжарить курицу 10 минут"));
        var parser = new FakeParser(Result.Ok(ChickenRecipe));
        var notifier = new FakeNotifier();

        await ParseAsync(context, draftId, parser, notifier, loader, speech);

        Assert.Equal("https://www.instagram.com/reel/DaSFnBZsJLo/", loader.LastUrl);
        Assert.Equal(audio, speech.LastFile);
        Assert.False(File.Exists(audio)); // временный звук удалён после расшифровки
        Assert.Contains("Филе 500 г", parser.LastRequest!.Text);
        Assert.Contains("Обжарить курицу 10 минут", parser.LastRequest.Text);
        Assert.Equal([draftId], notifier.Ready);

        var recipeId = (await ConfirmAsync(context, draftId, userId)).Value;
        var recipe = (await new RecipeRepositoryService(context).GetByIdAsync(recipeId, userId, CancellationToken.None)).Value;
        Assert.Equal("https://www.instagram.com/reel/DaSFnBZsJLo/", recipe.SourceUrl);
        Assert.Equal(ReferenceIds.SourceTypes.Instagram, recipe.SourceTypeId);
    }

    [Fact]
    public async Task FromUrl_DownloadFailed_ReportsVideoUnavailableAndDiscardsDraft()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = (await CreateFromUrlAsync(context, userId, "https://instagram.com/reel/abc123")).Value;
        var notifier = new FakeNotifier();

        await ParseAsync(context, draftId, new FakeParser(Result.Ok(ChickenRecipe)), notifier,
            new FakeVideoLoader(Result.Fail(new AppError("private", ErrorCode.NotFound))));

        Assert.Equal([RecipeDraftFailureReason.VideoUnavailable], notifier.Failed);
        Assert.Empty(context.RecipeDrafts);
    }

    [Fact]
    public async Task FromUrl_MusicOnlyAndEmptyCaption_ReportsNoText_WithoutCallingLlm()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = (await CreateFromUrlAsync(context, userId, "https://instagram.com/reel/abc123")).Value;
        var parser = new FakeParser(Result.Ok(ChickenRecipe));
        var notifier = new FakeNotifier();

        await ParseAsync(context, draftId, parser, notifier,
            new FakeVideoLoader(Result.Ok(new VideoSource("  ", TempFile()))), new FakeSpeechToText(Result.Ok("")));

        Assert.Equal([RecipeDraftFailureReason.NoTextInVideo], notifier.Failed);
        Assert.Null(parser.LastRequest);
    }

    [Fact]
    public async Task FromUrl_TranscriptionNotConfigured_UsesCaptionOnly()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var draftId = (await CreateFromUrlAsync(context, userId, "https://instagram.com/reel/abc123")).Value;
        var parser = new FakeParser(Result.Ok(ChickenRecipe));

        await ParseAsync(context, draftId, parser, new FakeNotifier(),
            new FakeVideoLoader(Result.Ok(new VideoSource("Рецепт в описании", TempFile()))),
            new FakeSpeechToText(Result.Fail(new AppError("no key", ErrorCode.Unavailable))));

        Assert.Equal("Описание под видео:\nРецепт в описании", parser.LastRequest!.Text.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task FromVideoFile_TranscribesFileWithCaption_AndDeletesIt()
    {
        var (context, userId, _) = await CreateAsync();
        await using var _ = context;
        var video = TempFile();
        var draftId = (await new CreateRecipeDraftFromVideoCommandHandler(new RecipeDraftRepositoryService(context), new NoopQueue())
            .Handle(new CreateRecipeDraftFromVideoCommand(userId, video, "Описание из подписи"), CancellationToken.None)).Value;
        var speech = new FakeSpeechToText(Result.Ok("Речь из видео"));
        var parser = new FakeParser(Result.Ok(ChickenRecipe));

        await ParseAsync(context, draftId, parser, new FakeNotifier(), speechToText: speech);

        Assert.Equal(video, speech.LastFile);
        Assert.False(File.Exists(video));
        Assert.Contains("Описание из подписи", parser.LastRequest!.Text);
        Assert.Contains("Речь из видео", parser.LastRequest.Text);
        Assert.Null((await context.RecipeDrafts.SingleAsync()).MediaFilePath);
    }
}
