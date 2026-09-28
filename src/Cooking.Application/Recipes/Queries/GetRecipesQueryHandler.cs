using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Queries;

public class GetRecipesQueryHandler(IRecipeRepositoryService repository)
    : IRequestHandler<GetRecipesQuery, Result<List<RecipeSummaryDto>>>
{
    public Task<Result<List<RecipeSummaryDto>>> Handle(GetRecipesQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(request.UserId, request.Search, cancellationToken);
}
