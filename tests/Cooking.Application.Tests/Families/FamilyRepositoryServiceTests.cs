using Cooking.Application.Common.Errors;
using Cooking.Domain.Entities.Users;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Families;

public class FamilyRepositoryServiceTests
{
    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Guid> SeedUserAsync(CookingDbContext context, long telegramId, string firstName)
    {
        var user = new User { TelegramId = telegramId, FirstName = firstName };
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
    public async Task CreateAsync_MakesUserTheFirstMember()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, 1, "Иван");
        var repository = new FamilyRepositoryService(context);

        var result = await repository.CreateAsync(userId, "Семья Ивана", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var family = await repository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal("Семья Ивана", family.Value.Name);
        Assert.Equal(userId, Assert.Single(family.Value.Members).UserId);
    }

    [Fact]
    public async Task CreateAsync_WhenUserAlreadyInFamily_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, 1, "Иван");
        var repository = new FamilyRepositoryService(context);
        await repository.CreateAsync(userId, "Первая", CancellationToken.None);

        var result = await repository.CreateAsync(userId, "Вторая", CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownUser_ReturnsValidationError()
    {
        await using var context = CreateContext();
        var repository = new FamilyRepositoryService(context);

        var result = await repository.CreateAsync(Guid.NewGuid(), "Семья", CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateInviteAsync_ByNonMember_ReturnsForbidden()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var strangerId = await SeedUserAsync(context, 2, "Пётр");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья", CancellationToken.None)).Value;

        var result = await repository.CreateInviteAsync(familyId, strangerId, CancellationToken.None);

        AssertError(result, ErrorCode.Forbidden);
    }

    [Fact]
    public async Task AcceptInviteAsync_WithValidCode_JoinsFamilyAndConsumesInvite()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var friendId = await SeedUserAsync(context, 2, "Маша");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья", CancellationToken.None)).Value;
        var invite = (await repository.CreateInviteAsync(familyId, ownerId, CancellationToken.None)).Value;

        var result = await repository.AcceptInviteAsync(invite.Code, friendId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(familyId, result.Value);
        Assert.Equal(familyId, (await context.Users.FindAsync(friendId))!.FamilyId);
        Assert.NotNull((await context.FamilyInvites.SingleAsync()).UsedAt);
    }

    [Fact]
    public async Task AcceptInviteAsync_WithUsedCode_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var firstId = await SeedUserAsync(context, 2, "Маша");
        var secondId = await SeedUserAsync(context, 3, "Пётр");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья", CancellationToken.None)).Value;
        var invite = (await repository.CreateInviteAsync(familyId, ownerId, CancellationToken.None)).Value;
        await repository.AcceptInviteAsync(invite.Code, firstId, CancellationToken.None);

        var result = await repository.AcceptInviteAsync(invite.Code, secondId, CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
    }

    [Fact]
    public async Task AcceptInviteAsync_WithExpiredCode_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var friendId = await SeedUserAsync(context, 2, "Маша");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья", CancellationToken.None)).Value;
        context.FamilyInvites.Add(new FamilyInvite
        {
            Code = "expired",
            FamilyId = familyId,
            CreatedByUserId = ownerId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await repository.AcceptInviteAsync("expired", friendId, CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
        Assert.Null((await context.Users.FindAsync(friendId))!.FamilyId);
    }

    [Fact]
    public async Task AcceptInviteAsync_WhenUserInAnotherFamily_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var otherOwnerId = await SeedUserAsync(context, 2, "Маша");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья Ивана", CancellationToken.None)).Value;
        await repository.CreateAsync(otherOwnerId, "Семья Маши", CancellationToken.None);
        var invite = (await repository.CreateInviteAsync(familyId, ownerId, CancellationToken.None)).Value;

        var result = await repository.AcceptInviteAsync(invite.Code, otherOwnerId, CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
    }

    [Fact]
    public async Task AcceptInviteAsync_WithUnknownCode_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, 1, "Иван");
        var repository = new FamilyRepositoryService(context);

        var result = await repository.AcceptInviteAsync("nope", userId, CancellationToken.None);

        AssertError(result, ErrorCode.NotFound);
    }

    [Fact]
    public async Task LeaveAsync_WithOtherMembersLeft_KeepsFamily()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var friendId = await SeedUserAsync(context, 2, "Маша");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья", CancellationToken.None)).Value;
        var invite = (await repository.CreateInviteAsync(familyId, ownerId, CancellationToken.None)).Value;
        await repository.AcceptInviteAsync(invite.Code, friendId, CancellationToken.None);

        var result = await repository.LeaveAsync(familyId, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null((await context.Users.FindAsync(ownerId))!.FamilyId);
        var family = await repository.GetByIdAsync(familyId, CancellationToken.None);
        Assert.Equal(friendId, Assert.Single(family.Value.Members).UserId);
    }

    [Fact]
    public async Task LeaveAsync_ByLastMember_DeletesFamily()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, 1, "Иван");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(userId, "Семья", CancellationToken.None)).Value;

        var result = await repository.LeaveAsync(familyId, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await context.Families.AnyAsync());
    }

    [Fact]
    public async Task LeaveAsync_ByNonMember_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var ownerId = await SeedUserAsync(context, 1, "Иван");
        var strangerId = await SeedUserAsync(context, 2, "Пётр");
        var repository = new FamilyRepositoryService(context);
        var familyId = (await repository.CreateAsync(ownerId, "Семья", CancellationToken.None)).Value;

        var result = await repository.LeaveAsync(familyId, strangerId, CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
    }
}
