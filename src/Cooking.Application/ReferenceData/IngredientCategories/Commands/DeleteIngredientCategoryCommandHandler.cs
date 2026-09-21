using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public class DeleteIngredientCategoryCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<DeleteIngredientCategoryCommand, Result>
{
    public Task<Result> Handle(DeleteIngredientCategoryCommand request, CancellationToken cancellationToken) =>
        repository.DeleteIngredientCategoryAsync(request.Id, cancellationToken);
}
