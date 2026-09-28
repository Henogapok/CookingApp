using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Commands;

public class DeleteRecipeCommandHandler(IRecipeRepositoryService repository)
    : IRequestHandler<DeleteRecipeCommand, Result>
{
    public Task<Result> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, request.UserId, cancellationToken);
}
