using FluentResults;

namespace Cooking.Application.Ingredients;

public interface IIngredientCatalogRepositoryService
{
    Task<Result<Guid>> CreateAsync(IngredientCatalogFields fields, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(Guid id, IngredientCatalogFields fields, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<IngredientCatalogDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<IngredientCatalogDto>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Проставляет вес 1 шт, только если он ещё не задан (ручное значение не перетираем).</summary>
    Task SetPieceWeightIfMissingAsync(Guid id, decimal pieceWeight, CancellationToken cancellationToken);
}
