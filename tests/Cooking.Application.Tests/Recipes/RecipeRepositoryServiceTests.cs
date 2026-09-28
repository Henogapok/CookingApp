using Cooking.Application.Common.Errors;
using Cooking.Application.Recipes;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.Entities.Users;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Recipes;

public class RecipeRepositoryServiceTests
{
    /// <summary>Автор и его девушка в одной семье, посторонний — вне её; плюс минимальные справочники.</summary>
    private sealed record Setup(
        Guid AuthorId,
        Guid FamilyMemberId,
        Guid StrangerId,
        Guid SourceTypeId,
        Guid ComplexityId,
        Guid GramId,
        Guid PieceId,
        Guid ChickenId,
        Guid RiceId,
        Guid DinnerTagId,
        Guid AsianTagId,
        Guid FryingTagId);

    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Setup> SeedAsync(CookingDbContext context)
    {
        var family = new Family { Name = "Семья" };
        var author = new User { TelegramId = 1, FirstName = "Иван", Family = family };
        var member = new User { TelegramId = 2, FirstName = "Маша", Family = family };
        var stranger = new User { TelegramId = 3, FirstName = "Пётр" };

        var sourceType = new SourceType { Name = "Manual" };
        var complexity = new Complexity { Name = "Easy" };
        var gram = new MeasurementUnit { Name = "Грамм", Abbreviation = "г" };
        var piece = new MeasurementUnit { Name = "Штука", Abbreviation = "шт" };
        var category = new IngredientCategory { Name = "Птица" };
        var dataSource = new DataSource { Name = "Manual" };

        IngredientCatalog Ingredient(string name) => new()
        {
            Name = name,
            Category = category,
            BaseUnit = gram,
            CreatedBySource = dataSource,
            NutritionSource = dataSource,
        };

        var chicken = Ingredient("Куриная грудка");
        var rice = Ingredient("Рис");

        var mealType = new TagType { Name = "MealType" };
        var dinner = new Tag { Name = "Ужин", TagType = mealType };
        var asian = new Tag { Name = "Азиатская", TagType = mealType };
        var frying = new Tag { Name = "Жарка", TagType = mealType };

        context.AddRange(author, member, stranger, sourceType, complexity, piece, chicken, rice, dinner, asian, frying);
        await context.SaveChangesAsync(CancellationToken.None);

        return new Setup(
            author.Id, member.Id, stranger.Id,
            sourceType.Id, complexity.Id, gram.Id, piece.Id,
            chicken.Id, rice.Id,
            dinner.Id, asian.Id, frying.Id);
    }

    private static RecipeFields Fields(Setup s, string title = "Курица с рисом") => new(
        title,
        "Быстрый ужин",
        "https://example.com/recipe",
        s.SourceTypeId,
        s.ComplexityId,
        Servings: 2,
        CookingTimeMinutes: 30,
        [
            new RecipeIngredientFields(s.ChickenId, 300, s.GramId),
            new RecipeIngredientFields(s.RiceId, 150, s.GramId),
        ],
        [
            new RecipeStepFields("Отварить рис", 900),
            new RecipeStepFields("Обжарить курицу", null),
        ],
        [s.DinnerTagId, s.AsianTagId]);

    private static void AssertError(ResultBase result, ErrorCode expected)
    {
        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(expected, error.Code);
    }

    [Fact]
    public async Task CreateAsync_SavesIngredientsAndStepsInListOrder()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);

        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var recipe = (await repository.GetByIdAsync(id, s.AuthorId, CancellationToken.None)).Value;
        Assert.Equal("Курица с рисом", recipe.Title);
        Assert.Equal(s.AuthorId, recipe.CreatedByUserId);
        Assert.Equal(["Куриная грудка", "Рис"], recipe.Ingredients.Select(i => i.IngredientName));
        Assert.Equal([1, 2], recipe.Steps.Select(st => st.StepNumber));
        Assert.Equal(900, recipe.Steps[0].TimerSeconds);
        Assert.Equal(["Азиатская", "Ужин"], recipe.Tags.Select(t => t.Name));
    }

    [Fact]
    public async Task CreateAsync_WithUnknownIngredient_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var fields = Fields(s) with { Ingredients = [new RecipeIngredientFields(Guid.NewGuid(), 100, s.GramId)] };

        var result = await repository.CreateAsync(s.AuthorId, fields, CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
        Assert.Empty(context.Recipes);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownTag_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);

        var result = await repository.CreateAsync(s.AuthorId, Fields(s) with { TagIds = [Guid.NewGuid()] }, CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownUser_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);

        var result = await repository.CreateAsync(Guid.NewGuid(), Fields(s), CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
    }

    [Fact]
    public async Task GetByIdAsync_ByFamilyMember_ReturnsRecipe()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var result = await repository.GetByIdAsync(id, s.FamilyMemberId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_ByStranger_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var result = await repository.GetByIdAsync(id, s.StrangerId, CancellationToken.None);

        AssertError(result, ErrorCode.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_AfterAuthorLeftFamily_IsHiddenFromFormerFamily()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;
        (await context.Users.FindAsync(s.AuthorId))!.FamilyId = null;
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await repository.GetByIdAsync(id, s.FamilyMemberId, CancellationToken.None);

        AssertError(result, ErrorCode.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOwnAndFamilyRecipesOnly()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        await repository.CreateAsync(s.AuthorId, Fields(s, "Рецепт Ивана"), CancellationToken.None);
        await repository.CreateAsync(s.FamilyMemberId, Fields(s, "Рецепт Маши"), CancellationToken.None);
        await repository.CreateAsync(s.StrangerId, Fields(s, "Рецепт Петра"), CancellationToken.None);

        var result = await repository.GetAllAsync(s.FamilyMemberId, null, CancellationToken.None);

        Assert.Equal(["Рецепт Ивана", "Рецепт Маши"], result.Value.Select(r => r.Title));
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_MatchesTitleIgnoringCase()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        await repository.CreateAsync(s.AuthorId, Fields(s, "Курица с рисом"), CancellationToken.None);
        await repository.CreateAsync(s.AuthorId, Fields(s, "Борщ"), CancellationToken.None);

        var result = await repository.GetAllAsync(s.AuthorId, "  КУРИЦА ", CancellationToken.None);

        Assert.Equal("Курица с рисом", Assert.Single(result.Value).Title);
    }

    [Fact]
    public async Task UpdateAsync_ByFamilyMember_ReplacesContent()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;
        var updated = Fields(s, "Курица с рисом по-машиному") with
        {
            Ingredients = [new RecipeIngredientFields(s.ChickenId, 2, s.PieceId)],
            Steps = [new RecipeStepFields("Запечь всё вместе", 1800)],
            TagIds = [s.AsianTagId, s.FryingTagId],
        };

        var result = await repository.UpdateAsync(id, s.FamilyMemberId, updated, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var recipe = (await repository.GetByIdAsync(id, s.AuthorId, CancellationToken.None)).Value;
        Assert.Equal("Курица с рисом по-машиному", recipe.Title);
        Assert.Equal(s.AuthorId, recipe.CreatedByUserId);
        var ingredient = Assert.Single(recipe.Ingredients);
        Assert.Equal(s.PieceId, ingredient.UnitId);
        Assert.Equal("Запечь всё вместе", Assert.Single(recipe.Steps).Instruction);
        Assert.Equal(["Азиатская", "Жарка"], recipe.Tags.Select(t => t.Name));
        Assert.Equal(1, await context.RecipeIngredients.CountAsync());
        Assert.Equal(1, await context.RecipeSteps.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ByStranger_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var result = await repository.UpdateAsync(id, s.StrangerId, Fields(s, "Чужое"), CancellationToken.None);

        AssertError(result, ErrorCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ByAuthor_SoftDeletesAndHidesRecipe()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var result = await repository.DeleteAsync(id, s.AuthorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        AssertError(await repository.GetByIdAsync(id, s.AuthorId, CancellationToken.None), ErrorCode.NotFound);
        Assert.Empty((await repository.GetAllAsync(s.AuthorId, null, CancellationToken.None)).Value);
        var stored = await context.Recipes.IgnoreQueryFilters().SingleAsync(r => r.Id == id);
        Assert.NotNull(stored.DeletedAt);
    }

    [Fact]
    public async Task DeleteAsync_ByFamilyMember_ReturnsForbidden()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var result = await repository.DeleteAsync(id, s.FamilyMemberId, CancellationToken.None);

        AssertError(result, ErrorCode.Forbidden);
        Assert.Null((await context.Recipes.SingleAsync(r => r.Id == id)).DeletedAt);
    }

    [Fact]
    public async Task DeleteAsync_ByStranger_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new RecipeRepositoryService(context);
        var id = (await repository.CreateAsync(s.AuthorId, Fields(s), CancellationToken.None)).Value;

        var result = await repository.DeleteAsync(id, s.StrangerId, CancellationToken.None);

        AssertError(result, ErrorCode.NotFound);
    }
}
