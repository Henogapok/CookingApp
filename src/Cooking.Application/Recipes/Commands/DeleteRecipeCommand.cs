using FluentResults;
using MediatR;

namespace Cooking.Application.Recipes.Commands;

public record DeleteRecipeCommand(Guid Id, Guid UserId) : IRequest<Result>;
