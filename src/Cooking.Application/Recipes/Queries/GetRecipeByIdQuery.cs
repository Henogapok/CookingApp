using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Queries;

public record GetRecipeByIdQuery(Guid Id, Guid UserId) : IRequest<Result<RecipeDto>>;
