using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Commands;

/// <summary>Полная замена содержимого рецепта, включая списки ингредиентов, шагов и тегов.</summary>
public record UpdateRecipeCommand(Guid Id, Guid UserId, RecipeFields Recipe) : IRequest<Result>;
