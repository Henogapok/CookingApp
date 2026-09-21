using Cooking.Application.Common.Errors;
using Cooking.Application.Ingredients;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Ingredients;

public class IngredientCatalogRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(Guid categoryId, Guid unitId, Guid sourceId)> SeedReferenceDataAsync(CookingDbContext context)
    {
        var category = new IngredientCategory { Name = "Мясо" };
        var unit = new MeasurementUnit { Name = "грамм", Abbreviation = "г" };
        var source = new DataSource { Name = "Manual" };

        context.IngredientCategories.Add(category);
        context.MeasurementUnits.Add(unit);
        context.DataSources.Add(source);
        await context.SaveChangesAsync(CancellationToken.None);

        return (category.Id, unit.Id, source.Id);
    }

    private static IngredientCatalogFields ValidFields(Guid categoryId, Guid unitId, Guid sourceId) => new(
        "Куриная грудка", categoryId, unitId, 25.0m, 165.0m, 31.0m, 3.6m, 0.0m, sourceId, sourceId);

    [Fact]
    public async Task CreateAsync_WithValidForeignKeys_AddsEntity()
    {
        await using var context = CreateContext();
        var (categoryId, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);

        var result = await repository.CreateAsync(ValidFields(categoryId, unitId, sourceId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.IngredientCatalog.FindAsync(result.Value);
        Assert.Equal("Куриная грудка", stored?.Name);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCategoryId_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var (_, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);

        var result = await repository.CreateAsync(ValidFields(Guid.NewGuid(), unitId, sourceId), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.Validation, error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ResolvesDisplayNamesOfForeignKeys()
    {
        await using var context = CreateContext();
        var (categoryId, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);
        var id = (await repository.CreateAsync(ValidFields(categoryId, unitId, sourceId), CancellationToken.None)).Value;

        var result = await repository.GetByIdAsync(id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Мясо", result.Value.CategoryName);
        Assert.Equal("грамм", result.Value.BaseUnitName);
        Assert.Equal("г", result.Value.BaseUnitAbbreviation);
        Assert.Equal("Manual", result.Value.CreatedBySourceName);
        Assert.Equal("Manual", result.Value.NutritionSourceName);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var repository = new IngredientCatalogRepositoryService(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var (categoryId, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);

        var result = await repository.UpdateAsync(Guid.NewGuid(), ValidFields(categoryId, unitId, sourceId), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownForeignKey_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var (categoryId, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);
        var id = (await repository.CreateAsync(ValidFields(categoryId, unitId, sourceId), CancellationToken.None)).Value;

        var result = await repository.UpdateAsync(id, ValidFields(categoryId, Guid.NewGuid(), sourceId), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.Validation, error.Code);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        await using var context = CreateContext();
        var (categoryId, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);
        var id = (await repository.CreateAsync(ValidFields(categoryId, unitId, sourceId), CancellationToken.None)).Value;

        var result = await repository.DeleteAsync(id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await context.IngredientCatalog.FindAsync(id));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByName()
    {
        await using var context = CreateContext();
        var (categoryId, unitId, sourceId) = await SeedReferenceDataAsync(context);
        var repository = new IngredientCatalogRepositoryService(context);
        await repository.CreateAsync(ValidFields(categoryId, unitId, sourceId) with { Name = "Яйцо" }, CancellationToken.None);
        await repository.CreateAsync(ValidFields(categoryId, unitId, sourceId) with { Name = "Авокадо" }, CancellationToken.None);

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Авокадо", "Яйцо"], result.Value.Select(x => x.Name));
    }
}
