using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Commands;

public class DeleteIngredientCatalogCommandHandler(IIngredientCatalogRepositoryService repository)
    : IRequestHandler<DeleteIngredientCatalogCommand, Result>
{
    public Task<Result> Handle(DeleteIngredientCatalogCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
