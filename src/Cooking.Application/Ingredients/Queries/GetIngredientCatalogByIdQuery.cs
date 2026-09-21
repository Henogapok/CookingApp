using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Queries;

public record GetIngredientCatalogByIdQuery(Guid Id) : IRequest<Result<IngredientCatalogDto>>;
