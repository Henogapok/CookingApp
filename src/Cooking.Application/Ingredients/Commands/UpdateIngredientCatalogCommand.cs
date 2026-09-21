using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Commands;

public record UpdateIngredientCatalogCommand(
    Guid Id,
    string Name,
    Guid CategoryId,
    Guid BaseUnitId,
    decimal PricePer100G,
    decimal CaloriesPer100G,
    decimal ProteinPer100G,
    decimal FatPer100G,
    decimal CarbsPer100G,
    Guid CreatedBySourceId,
    Guid NutritionSourceId) : IRequest<Result>;
