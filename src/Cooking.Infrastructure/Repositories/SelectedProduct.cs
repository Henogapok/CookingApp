using Cooking.Application.Nutrition;

namespace Cooking.Infrastructure.Repositories;

/// <summary>Продукт, по которому считается ингредиент у этого пользователя.</summary>
internal sealed record SelectedProduct(Guid Id, string Name, ProductValues Values);
