using FluentResults;

namespace Cooking.Application.Products;

/// <summary>
/// Продукты — то, что пользователь покупает, с ценой и КБЖУ с упаковки. Видят и правят их автор и его семья;
/// чужой продукт — NotFound.
/// </summary>
public interface IProductRepositoryService
{
    /// <summary>Ингредиенты из рецептов семьи и ингредиенты с продуктами семьи — для экрана «Мои продукты» и выбора в рецепте.</summary>
    Task<Result<List<IngredientProductsDto>>> GetHouseholdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Новый продукт сразу становится выбранным: купили — значит, он сейчас и в холодильнике.</summary>
    Task<Result<Guid>> CreateAsync(Guid userId, ProductFields fields, CancellationToken cancellationToken);

    /// <summary>Ингредиент продукта не меняется — это был бы другой продукт.</summary>
    Task<Result> UpdateAsync(Guid id, Guid userId, ProductFields fields, CancellationToken cancellationToken);

    /// <summary>Рецепты ссылаются на ингредиент, а не на продукт, — после удаления выбранным станет следующий продукт или каталог.</summary>
    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Выбрать продукт для ингредиента — для всей семьи.</summary>
    Task<Result> SelectAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}
