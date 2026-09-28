using FluentResults;

namespace Cooking.Application.Recipes;

/// <summary>
/// Доступ: рецепт видят и редактируют автор и участники его семьи, удаляет только автор.
/// Для постороннего рецепт «не существует» (NotFound), чтобы не раскрывать чужие Id.
/// userId — действующий пользователь (временно передаётся явно, пока нет авторизации).
/// </summary>
public interface IRecipeRepositoryService
{
    Task<Result<Guid>> CreateAsync(Guid userId, RecipeFields fields, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(Guid id, Guid userId, RecipeFields fields, CancellationToken cancellationToken);

    /// <summary>Soft delete: проставляет DeletedAt.</summary>
    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<Result<RecipeDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Рецепты пользователя и его семьи; search — подстрока названия без учёта регистра.</summary>
    Task<Result<List<RecipeSummaryDto>>> GetAllAsync(Guid userId, string? search, CancellationToken cancellationToken);
}
