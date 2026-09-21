using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Queries;

public class GetIngredientCatalogsQueryHandler(IIngredientCatalogRepositoryService repository)
    : IRequestHandler<GetIngredientCatalogsQuery, Result<List<IngredientCatalogDto>>>
{
    public Task<Result<List<IngredientCatalogDto>>> Handle(GetIngredientCatalogsQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);
}
