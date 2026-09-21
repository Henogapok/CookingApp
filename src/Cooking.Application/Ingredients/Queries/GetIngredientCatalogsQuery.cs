using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Queries;

public record GetIngredientCatalogsQuery : IRequest<Result<List<IngredientCatalogDto>>>;
