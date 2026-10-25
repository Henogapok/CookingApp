using Cooking.Application.Auth;
using Cooking.Application.Common.Errors;
using Cooking.Domain.Entities.Users;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Auth;

public class LoginRequestRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Guid> SeedUserAsync(CookingDbContext context)
    {
        var user = new User { TelegramId = 1, FirstName = "Иван" };
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        return user.Id;
    }

    private static void AssertError(ResultBase result, ErrorCode expected)
    {
        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(expected, error.Code);
    }

    [Fact]
    public async Task CreateAsync_CodeFitsTelegramStartParameter_AndPollTokenIsStoredOnlyAsHash()
    {
        await using var context = CreateContext();
        var repository = new LoginRequestRepositoryService(context);

        var created = (await repository.CreateAsync(CancellationToken.None)).Value;

        Assert.Matches("^[A-Za-z0-9_-]{1,64}$", LoginRequestLink.StartPrefix + created.Code);
        var stored = await context.LoginRequests.SingleAsync();
        Assert.NotEqual(created.PollToken, stored.PollTokenHash);
        Assert.DoesNotContain(created.PollToken, stored.PollTokenHash);
    }

    [Fact]
    public async Task CompleteAsync_BeforeConfirmation_IsPending()
    {
        await using var context = CreateContext();
        var repository = new LoginRequestRepositoryService(context);
        var created = (await repository.CreateAsync(CancellationToken.None)).Value;

        var status = await repository.CompleteAsync(created.PollToken, CancellationToken.None);

        Assert.Equal(LoginRequestState.Pending, status.Value.State);
        Assert.Null(status.Value.UserId);
    }

    [Fact]
    public async Task CompleteAsync_AfterConfirmation_ReturnsUserOnlyOnce()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context);
        var repository = new LoginRequestRepositoryService(context);
        var created = (await repository.CreateAsync(CancellationToken.None)).Value;

        Assert.True((await repository.ConfirmAsync(created.Code, userId, CancellationToken.None)).IsSuccess);

        var first = await repository.CompleteAsync(created.PollToken, CancellationToken.None);
        Assert.Equal(LoginRequestState.Confirmed, first.Value.State);
        Assert.Equal(userId, first.Value.UserId);

        AssertError(await repository.CompleteAsync(created.PollToken, CancellationToken.None), ErrorCode.NotFound);
    }

    [Fact]
    public async Task CompleteAsync_WithCodeInsteadOfPollToken_IsNotFound()
    {
        // Код виден в ссылке на бота — по нему нельзя забрать вход.
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context);
        var repository = new LoginRequestRepositoryService(context);
        var created = (await repository.CreateAsync(CancellationToken.None)).Value;
        await repository.ConfirmAsync(created.Code, userId, CancellationToken.None);

        AssertError(await repository.CompleteAsync(created.Code, CancellationToken.None), ErrorCode.NotFound);
    }

    [Fact]
    public async Task DeclineAsync_ThenComplete_IsDeclined_AndCannotBeConfirmed()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context);
        var repository = new LoginRequestRepositoryService(context);
        var created = (await repository.CreateAsync(CancellationToken.None)).Value;

        Assert.True((await repository.DeclineAsync(created.Code, CancellationToken.None)).IsSuccess);

        Assert.Equal(LoginRequestState.Declined, (await repository.CompleteAsync(created.PollToken, CancellationToken.None)).Value.State);
        AssertError(await repository.ConfirmAsync(created.Code, userId, CancellationToken.None), ErrorCode.LogicConflict);
    }

    [Fact]
    public async Task ConfirmAsync_Twice_IsLogicConflict()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context);
        var repository = new LoginRequestRepositoryService(context);
        var created = (await repository.CreateAsync(CancellationToken.None)).Value;
        await repository.ConfirmAsync(created.Code, userId, CancellationToken.None);

        AssertError(await repository.ConfirmAsync(created.Code, userId, CancellationToken.None), ErrorCode.LogicConflict);
    }

    [Fact]
    public async Task ExpiredRequest_CannotBeConfirmedOrCompleted()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context);
        var repository = new LoginRequestRepositoryService(context);
        var created = (await repository.CreateAsync(CancellationToken.None)).Value;

        var stored = await context.LoginRequests.SingleAsync();
        stored.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();

        AssertError(await repository.ConfirmAsync(created.Code, userId, CancellationToken.None), ErrorCode.LogicConflict);
        AssertError(await repository.CompleteAsync(created.PollToken, CancellationToken.None), ErrorCode.NotFound);
    }

    [Fact]
    public async Task ConfirmAsync_UnknownCode_IsNotFound()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context);
        var repository = new LoginRequestRepositoryService(context);

        AssertError(await repository.ConfirmAsync("nope", userId, CancellationToken.None), ErrorCode.NotFound);
    }

    [Fact]
    public async Task CreateAsync_RemovesExpiredRequests()
    {
        await using var context = CreateContext();
        var repository = new LoginRequestRepositoryService(context);
        await repository.CreateAsync(CancellationToken.None);
        (await context.LoginRequests.SingleAsync()).ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        await repository.CreateAsync(CancellationToken.None);

        Assert.Single(await context.LoginRequests.ToListAsync());
    }

    [Theory]
    [InlineData("login_abc123", "abc123")]
    [InlineData("login_", null)]
    [InlineData("AbCdEfGh23456789", null)]
    [InlineData("", null)]
    public void LoginRequestLink_ParseCode(string payload, string? expected)
    {
        Assert.Equal(expected, LoginRequestLink.ParseCode(payload));
    }
}
