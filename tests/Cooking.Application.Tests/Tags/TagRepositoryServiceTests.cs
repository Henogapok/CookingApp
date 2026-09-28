using Cooking.Application.Common.Errors;
using Cooking.Domain.Entities.Tags;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Tags;

public class TagRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(Guid mealTypeId, Guid cuisineId)> SeedTagTypesAsync(CookingDbContext context)
    {
        var mealType = new TagType { Name = "MealType" };
        var cuisine = new TagType { Name = "Cuisine" };

        context.TagTypes.AddRange(mealType, cuisine);
        await context.SaveChangesAsync(CancellationToken.None);

        return (mealType.Id, cuisine.Id);
    }

    [Fact]
    public async Task CreateAsync_WithExistingTagType_AddsTag()
    {
        await using var context = CreateContext();
        var (mealTypeId, _) = await SeedTagTypesAsync(context);
        var repository = new TagRepositoryService(context);

        var result = await repository.CreateAsync("Ужин", mealTypeId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tag = (await repository.GetByIdAsync(result.Value, CancellationToken.None)).Value;
        Assert.Equal("Ужин", tag.Name);
        Assert.Equal("MealType", tag.TagTypeName);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownTagType_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var repository = new TagRepositoryService(context);

        var result = await repository.CreateAsync("Ужин", Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(ErrorCode.Validation, Assert.IsType<AppError>(Assert.Single(result.Errors)).Code);
        Assert.Empty(context.Tags);
    }

    [Fact]
    public async Task GetAllAsync_WithTagType_ReturnsOnlyThatType()
    {
        await using var context = CreateContext();
        var (mealTypeId, cuisineId) = await SeedTagTypesAsync(context);
        var repository = new TagRepositoryService(context);
        await repository.CreateAsync("Ужин", mealTypeId, CancellationToken.None);
        await repository.CreateAsync("Итальянская", cuisineId, CancellationToken.None);

        var result = await repository.GetAllAsync(cuisineId, CancellationToken.None);

        Assert.Equal("Итальянская", Assert.Single(result.Value).Name);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var (mealTypeId, _) = await SeedTagTypesAsync(context);
        var repository = new TagRepositoryService(context);

        var result = await repository.UpdateAsync(Guid.NewGuid(), "Ужин", mealTypeId, CancellationToken.None);

        Assert.Equal(ErrorCode.NotFound, Assert.IsType<AppError>(Assert.Single(result.Errors)).Code);
    }
}
