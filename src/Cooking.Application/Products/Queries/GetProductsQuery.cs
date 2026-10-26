using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Queries;

public record GetProductsQuery(Guid UserId) : IRequest<Result<List<IngredientProductsDto>>>;
