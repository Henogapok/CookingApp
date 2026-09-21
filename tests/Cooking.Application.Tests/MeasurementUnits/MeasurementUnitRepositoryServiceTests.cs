using Cooking.Application.Common.Errors;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.MeasurementUnits;

public class MeasurementUnitRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateAsync_AddsEntity_AndReturnsItsId()
    {
        await using var context = CreateContext();
        var repository = new MeasurementUnitRepositoryService(context);

        var result = await repository.CreateAsync("грамм", "г", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.MeasurementUnits.FindAsync(result.Value);
        Assert.Equal("грамм", stored?.Name);
        Assert.Equal("г", stored?.Abbreviation);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var repository = new MeasurementUnitRepositoryService(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndAbbreviation()
    {
        await using var context = CreateContext();
        var repository = new MeasurementUnitRepositoryService(context);
        var id = (await repository.CreateAsync("столовая ложка", "ст.л.", CancellationToken.None)).Value;

        var result = await repository.UpdateAsync(id, "чайная ложка", "ч.л.", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.MeasurementUnits.FindAsync(id);
        Assert.Equal("чайная ложка", stored?.Name);
        Assert.Equal("ч.л.", stored?.Abbreviation);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        await using var context = CreateContext();
        var repository = new MeasurementUnitRepositoryService(context);
        var id = (await repository.CreateAsync("штука", "шт", CancellationToken.None)).Value;

        var result = await repository.DeleteAsync(id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await context.MeasurementUnits.FindAsync(id));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByName()
    {
        await using var context = CreateContext();
        var repository = new MeasurementUnitRepositoryService(context);
        await repository.CreateAsync("штука", "шт", CancellationToken.None);
        await repository.CreateAsync("грамм", "г", CancellationToken.None);

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["грамм", "штука"], result.Value.Select(x => x.Name));
    }
}
