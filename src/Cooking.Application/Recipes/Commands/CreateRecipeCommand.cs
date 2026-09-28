using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Commands;

public record CreateRecipeCommand(Guid UserId, RecipeFields Recipe) : IRequest<Result<Guid>>;
