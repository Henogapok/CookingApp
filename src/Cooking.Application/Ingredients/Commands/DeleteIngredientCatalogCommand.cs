using FluentResults;
using MediatR;

namespace Cooking.Application.Ingredients.Commands;

public record DeleteIngredientCatalogCommand(Guid Id) : IRequest<Result>;
