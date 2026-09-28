using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Commands;

public class UpdateRecipeCommandHandler(IRecipeRepositoryService repository)
    : IRequestHandler<UpdateRecipeCommand, Result>
{
    public Task<Result> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(request.Id, request.UserId, request.Recipe, cancellationToken);
}
