using System.Security.Cryptography;
using System.Text;
using Cooking.Application.Auth;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Users;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class LoginRequestRepositoryService(IDataContext dataContext) : ILoginRequestRepositoryService
{
    /// <summary>Сколько ждём подтверждения в боте.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    // Только [A-Za-z0-9]: код идёт в start-параметр после префикса (см. LoginRequestLink).
    private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int CodeLength = 32;

    public async Task<Result<LoginRequestCreatedDto>> CreateAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Как с черновиками: отдельной фоновой чистки нет, просроченные убираем при создании нового.
        var expired = await dataContext.LoginRequests.Where(r => r.ExpiresAt < now).ToListAsync(cancellationToken);
        dataContext.LoginRequests.RemoveRange(expired);

        var pollToken = Base64Url(RandomNumberGenerator.GetBytes(32));
        var request = new LoginRequest
        {
            Code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength),
            PollTokenHash = Hash(pollToken),
            ExpiresAt = now + Lifetime,
        };

        dataContext.LoginRequests.Add(request);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LoginRequestCreatedDto(request.Code, pollToken, request.ExpiresAt));
    }

    public async Task<Result> ConfirmAsync(string code, Guid userId, CancellationToken cancellationToken)
    {
        var request = await FindPendingByCodeAsync(code, cancellationToken);
        if (request.IsFailed)
            return request.ToResult();

        if (!await dataContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            return Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

        request.Value.UserId = userId;
        request.Value.ConfirmedAt = DateTime.UtcNow;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeclineAsync(string code, CancellationToken cancellationToken)
    {
        var request = await FindPendingByCodeAsync(code, cancellationToken);
        if (request.IsFailed)
            return request.ToResult();

        request.Value.DeclinedAt = DateTime.UtcNow;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<LoginRequestStatusDto>> CompleteAsync(string pollToken, CancellationToken cancellationToken)
    {
        var hash = Hash(pollToken);
        var request = await dataContext.LoginRequests.FirstOrDefaultAsync(r => r.PollTokenHash == hash, cancellationToken);

        // Использованный, просроченный и несуществующий для браузера одинаковы: начать вход заново.
        if (request is null || request.CompletedAt is not null || request.ExpiresAt < DateTime.UtcNow)
            return Result.Fail(new AppError("Login request was not found or has expired.", ErrorCode.NotFound));

        if (request.DeclinedAt is not null)
            return Result.Ok(new LoginRequestStatusDto(LoginRequestState.Declined, null));

        if (request.UserId is not { } userId)
            return Result.Ok(new LoginRequestStatusDto(LoginRequestState.Pending, null));

        request.CompletedAt = DateTime.UtcNow;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LoginRequestStatusDto(LoginRequestState.Confirmed, userId));
    }

    /// <summary>Запрос, который ещё можно подтвердить или отклонить.</summary>
    private async Task<Result<LoginRequest>> FindPendingByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var request = await dataContext.LoginRequests.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (request is null)
            return Result.Fail(new AppError($"Login request '{code}' was not found.", ErrorCode.NotFound));

        if (request.ExpiresAt < DateTime.UtcNow)
            return Result.Fail(new AppError("Login request has expired.", ErrorCode.LogicConflict));

        if (request.ConfirmedAt is not null || request.DeclinedAt is not null)
            return Result.Fail(new AppError("Login request has already been answered.", ErrorCode.LogicConflict));

        return Result.Ok(request);
    }

    private static string Hash(string pollToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pollToken)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
