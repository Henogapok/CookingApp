using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Queries;

public class GetIngredientCategoriesQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetIngredientCategoriesQuery, Result<List<ReferenceEntityDto>>>
{
    public Task<Result<List<ReferenceEntityDto>>> Handle(GetIngredientCategoriesQuery request, CancellationToken cancellationToken) =>
        repository.GetIngredientCategoriesAsync(cancellationToken);
}
