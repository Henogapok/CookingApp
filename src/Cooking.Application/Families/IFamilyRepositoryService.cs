using FluentResults;

namespace Cooking.Application.Families;

public interface IFamilyRepositoryService
{
    /// <summary>Создаёт семью и делает userId её первым участником.</summary>
    Task<Result<Guid>> CreateAsync(Guid userId, string name, CancellationToken cancellationToken);

    /// <summary>Семью видят только её участники: для остальных её «нет» (NotFound), как и чужого рецепта.</summary>
    Task<Result<FamilyDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Выпускает одноразовый код приглашения; создать его может только участник семьи.</summary>
    Task<Result<FamilyInviteDto>> CreateInviteAsync(Guid familyId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Вступление по коду приглашения. Возвращает Id семьи.</summary>
    Task<Result<Guid>> AcceptInviteAsync(string code, Guid userId, CancellationToken cancellationToken);

    /// <summary>Выход из семьи. Если ушёл последний участник — семья удаляется.</summary>
    Task<Result> LeaveAsync(Guid familyId, Guid userId, CancellationToken cancellationToken);
}
