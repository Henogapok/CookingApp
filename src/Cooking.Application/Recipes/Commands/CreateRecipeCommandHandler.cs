using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Commands;

public class CreateRecipeCommandHandler(IRecipeRepositoryService repository)
    : IRequestHandler<CreateRecipeCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateRecipeCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request.UserId, request.Recipe, cancellationToken);
}
