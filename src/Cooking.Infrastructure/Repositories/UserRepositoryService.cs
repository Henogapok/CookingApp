using System.Linq.Expressions;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Users;
using Cooking.Domain.Entities.Users;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class UserRepositoryService(IDataContext dataContext) : IUserRepositoryService
{
    private static readonly Expression<Func<User, UserDto>> ProjectToDto = e => new UserDto(
        e.Id, e.TelegramId, e.FirstName, e.LastName,
        e.FamilyId, e.Family == null ? null : e.Family.Name);

    public async Task<Result<UserDto>> RegisterAsync(long telegramId, string firstName, string? lastName, CancellationToken cancellationToken)
    {
        var entity = await dataContext.Users.FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken);

        if (entity is null)
        {
            entity = new User { TelegramId = telegramId, FirstName = firstName, LastName = lastName };
            dataContext.Users.Add(entity);
        }
        else
        {
            // Пользователь мог сменить имя в Telegram — держим актуальным.
            entity.FirstName = firstName;
            entity.LastName = lastName;
        }

        await dataContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await dataContext.Users
            .Where(e => e.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Fail(new AppError($"User with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(dto);
    }

    public async Task<Result<UserDto>> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken)
    {
        var dto = await dataContext.Users
            .Where(e => e.TelegramId == telegramId)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Fail(new AppError($"User with TelegramId '{telegramId}' was not found.", ErrorCode.NotFound))
            : Result.Ok(dto);
    }

    public async Task<Result<List<UserDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await dataContext.Users
            .OrderBy(e => e.FirstName)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);

        return Result.Ok(items);
    }
}
