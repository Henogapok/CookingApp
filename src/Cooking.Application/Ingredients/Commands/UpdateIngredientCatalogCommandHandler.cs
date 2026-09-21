using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Commands;

public class UpdateIngredientCatalogCommandHandler(IIngredientCatalogRepositoryService repository)
    : IRequestHandler<UpdateIngredientCatalogCommand, Result>
{
    public Task<Result> Handle(UpdateIngredientCatalogCommand request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(
            request.Id,
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
