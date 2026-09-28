using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Queries;

public class GetRecipeByIdQueryHandler(IRecipeRepositoryService repository)
    : IRequestHandler<GetRecipeByIdQuery, Result<RecipeDto>>
{
    public Task<Result<RecipeDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, request.UserId, cancellationToken);
}
