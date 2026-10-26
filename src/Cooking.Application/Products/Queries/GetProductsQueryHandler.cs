using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Queries;

public class GetProductsQueryHandler(IProductRepositoryService repository)
    : IRequestHandler<GetProductsQuery, Result<List<IngredientProductsDto>>>
{
    public Task<Result<List<IngredientProductsDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken) =>
        repository.GetHouseholdAsync(request.UserId, cancellationToken);
}
