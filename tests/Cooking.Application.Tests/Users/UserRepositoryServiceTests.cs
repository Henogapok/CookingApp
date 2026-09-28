using Cooking.Application.Common.Errors;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Users;

public class UserRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task RegisterAsync_WithNewTelegramId_CreatesUserWithoutFamily()
    {
        await using var context = CreateContext();
        var repository = new UserRepositoryService(context);

        var result = await repository.RegisterAsync(42, "Иван", null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.TelegramId);
        Assert.Null(result.Value.FamilyId);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingTelegramId_UpdatesNameInsteadOfDuplicating()
    {
        await using var context = CreateContext();
        var repository = new UserRepositoryService(context);
        var first = await repository.RegisterAsync(42, "Иван", null, CancellationToken.None);

        var second = await repository.RegisterAsync(42, "Ваня", "Петров", CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Equal("Ваня", second.Value.FirstName);
        Assert.Equal("Петров", second.Value.LastName);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task GetByTelegramIdAsync_WithUnknownTelegramId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var repository = new UserRepositoryService(context);

        var result = await repository.GetByTelegramIdAsync(42, CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(ErrorCode.NotFound, error.Code);
    }
}
