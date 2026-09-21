using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public class CreateIngredientCategoryCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<CreateIngredientCategoryCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateIngredientCategoryCommand request, CancellationToken cancellationToken) =>
        repository.CreateIngredientCategoryAsync(request.Name, cancellationToken);
}
