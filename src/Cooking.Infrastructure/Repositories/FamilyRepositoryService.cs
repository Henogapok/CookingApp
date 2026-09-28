using System.Security.Cryptography;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Families;
using Cooking.Domain.Entities.Users;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class FamilyRepositoryService(IDataContext dataContext) : IFamilyRepositoryService
{
    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    // Без похожих символов (0/O, 1/l/I) — на случай, если код когда-нибудь придётся вводить руками.
    // Все символы допустимы в start-параметре Telegram deep link'а.
    private const string InviteCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
    private const int InviteCodeLength = 16;

    public async Task<Result<Guid>> CreateAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

        if (user.FamilyId is not null)
            return Result.Fail(new AppError("User is already a member of a family. Leave it before creating a new one.", ErrorCode.LogicConflict));

        var family = new Family { Name = name };
        dataContext.Families.Add(family);
        user.Family = family;

        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(family.Id);
    }

    public async Task<Result<FamilyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await dataContext.Families
            .Where(f => f.Id == id)
            .Select(f => new FamilyDto(
                f.Id,
                f.Name,
                f.Users
                    .OrderBy(u => u.CreatedAt)
                    .Select(u => new FamilyMemberDto(u.Id, u.FirstName, u.LastName))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Fail(new AppError($"Family with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(dto);
    }

    public async Task<Result<FamilyInviteDto>> CreateInviteAsync(Guid familyId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await dataContext.Families.AnyAsync(x => x.Id == familyId, cancellationToken))
            return Result.Fail(new AppError($"Family with id '{familyId}' was not found.", ErrorCode.NotFound));

        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

        if (user.FamilyId != familyId)
            return Result.Fail(new AppError("Only a member of the family can invite others.", ErrorCode.Forbidden));

        var invite = new FamilyInvite
        {
            Code = RandomNumberGenerator.GetString(InviteCodeAlphabet, InviteCodeLength),
            FamilyId = familyId,
            CreatedByUserId = userId,
            ExpiresAt = DateTime.UtcNow.Add(InviteLifetime),
        };

        dataContext.FamilyInvites.Add(invite);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new FamilyInviteDto(invite.Code, invite.FamilyId, invite.ExpiresAt));
    }

    public async Task<Result<Guid>> AcceptInviteAsync(string code, Guid userId, CancellationToken cancellationToken)
    {
        var invite = await dataContext.FamilyInvites.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (invite is null)
            return Result.Fail(new AppError($"Invite with code '{code}' was not found.", ErrorCode.NotFound));

        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

        if (invite.UsedAt is not null)
            return Result.Fail(new AppError("This invite has already been used.", ErrorCode.LogicConflict));

        var now = DateTime.UtcNow;

        if (invite.ExpiresAt <= now)
            return Result.Fail(new AppError("This invite has expired.", ErrorCode.LogicConflict));

        if (user.FamilyId == invite.FamilyId)
            return Result.Fail(new AppError("User is already a member of this family.", ErrorCode.LogicConflict));

        if (user.FamilyId is not null)
            return Result.Fail(new AppError("User is already a member of another family. Leave it before joining a new one.", ErrorCode.LogicConflict));

        invite.UsedAt = now;
        user.FamilyId = invite.FamilyId;

        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(invite.FamilyId);
    }

    public async Task<Result> LeaveAsync(Guid familyId, Guid userId, CancellationToken cancellationToken)
    {
        var family = await dataContext.Families.FindAsync([familyId], cancellationToken);

        if (family is null)
            return Result.Fail(new AppError($"Family with id '{familyId}' was not found.", ErrorCode.NotFound));

        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

        if (user.FamilyId != familyId)
            return Result.Fail(new AppError("User is not a member of this family.", ErrorCode.LogicConflict));

        user.FamilyId = null;

        // Рецепты принадлежат авторам, поэтому пустую семью можно просто удалить — ничего не теряется.
        var hasOtherMembers = await dataContext.Users.AnyAsync(x => x.FamilyId == familyId && x.Id != userId, cancellationToken);
        if (!hasOtherMembers)
            dataContext.Families.Remove(family);

        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
