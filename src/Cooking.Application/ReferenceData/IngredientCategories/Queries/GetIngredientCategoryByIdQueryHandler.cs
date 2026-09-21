using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Queries;

public class GetIngredientCategoryByIdQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetIngredientCategoryByIdQuery, Result<ReferenceEntityDto>>
{
    public Task<Result<ReferenceEntityDto>> Handle(GetIngredientCategoryByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetIngredientCategoryByIdAsync(request.Id, cancellationToken);
}
