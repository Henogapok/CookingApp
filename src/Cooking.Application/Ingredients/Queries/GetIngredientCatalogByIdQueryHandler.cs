using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Queries;

public class GetIngredientCatalogByIdQueryHandler(IIngredientCatalogRepositoryService repository)
    : IRequestHandler<GetIngredientCatalogByIdQuery, Result<IngredientCatalogDto>>
{
    public Task<Result<IngredientCatalogDto>> Handle(GetIngredientCatalogByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
