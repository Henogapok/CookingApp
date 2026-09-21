using Cooking.Application.Common.Errors;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.ReferenceData;

public class ReferenceDataRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateSourceTypeAsync_AddsEntity_AndReturnsItsId()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);

        var result = await repository.CreateSourceTypeAsync("Instagram", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.SourceTypes.FindAsync(result.Value);
        Assert.Equal("Instagram", stored?.Name);
    }

    [Fact]
    public async Task GetSourceTypeByIdAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);

        var result = await repository.GetSourceTypeByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }

    [Fact]
    public async Task UpdateSourceTypeAsync_UpdatesName()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);
        var id = (await repository.CreateSourceTypeAsync("Old", CancellationToken.None)).Value;

        var result = await repository.UpdateSourceTypeAsync(id, "New", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.SourceTypes.FindAsync(id);
        Assert.Equal("New", stored?.Name);
    }

    [Fact]
    public async Task UpdateSourceTypeAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);

        var result = await repository.UpdateSourceTypeAsync(Guid.NewGuid(), "New", CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }

    [Fact]
    public async Task DeleteSourceTypeAsync_RemovesEntity()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);
        var id = (await repository.CreateSourceTypeAsync("Temp", CancellationToken.None)).Value;

        var result = await repository.DeleteSourceTypeAsync(id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await context.SourceTypes.FindAsync(id));
    }

    [Fact]
    public async Task DeleteSourceTypeAsync_WithUnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);

        var result = await repository.DeleteSourceTypeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }

    [Fact]
    public async Task GetSourceTypesAsync_ReturnsAllOrderedByName()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);
        await repository.CreateSourceTypeAsync("Website", CancellationToken.None);
        await repository.CreateSourceTypeAsync("Instagram", CancellationToken.None);

        var result = await repository.GetSourceTypesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Instagram", "Website"], result.Value.Select(x => x.Name));
    }

    [Fact]
    public async Task CreateComplexityAsync_AddsEntity_IndependentlyOfSourceType()
    {
        await using var context = CreateContext();
        var repository = new ReferenceDataRepositoryService(context);

        var result = await repository.CreateComplexityAsync("Easy", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.Complexities.FindAsync(result.Value);
        Assert.Equal("Easy", stored?.Name);
    }
}
