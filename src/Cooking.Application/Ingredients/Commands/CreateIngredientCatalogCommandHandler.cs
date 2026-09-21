using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Commands;

public class CreateIngredientCatalogCommandHandler(IIngredientCatalogRepositoryService repository)
    : IRequestHandler<CreateIngredientCatalogCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateIngredientCatalogCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(
            new IngredientCatalogFields(
                request.Name,
                request.CategoryId,
                request.BaseUnitId,
                request.PricePer100G,
                request.CaloriesPer100G,
                request.ProteinPer100G,
                request.FatPer100G,
                request.CarbsPer100G,
                request.CreatedBySourceId,
                request.NutritionSourceId),
            cancellationToken);
}
