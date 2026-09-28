using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Queries;

public record GetRecipesQuery(Guid UserId, string? Search) : IRequest<Result<List<RecipeSummaryDto>>>;
