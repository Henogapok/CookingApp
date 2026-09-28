using FluentResults;

namespace Cooking.Application.Tags;

public interface ITagRepositoryService
{
    Task<Result<Guid>> CreateAsync(string name, Guid tagTypeId, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(Guid id, string name, Guid tagTypeId, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<TagDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Все теги; если задан tagTypeId — только теги этого типа.</summary>
    Task<Result<List<TagDto>>> GetAllAsync(Guid? tagTypeId, CancellationToken cancellationToken);
}
