using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public class UpdateIngredientCategoryCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<UpdateIngredientCategoryCommand, Result>
{
    public Task<Result> Handle(UpdateIngredientCategoryCommand request, CancellationToken cancellationToken) =>
        repository.UpdateIngredientCategoryAsync(request.Id, request.Name, cancellationToken);
}
