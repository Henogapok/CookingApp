using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

/// <summary>Выбрать продукт для его ингредиента: по нему будут считаться рецепты у всей семьи.</summary>
public record SelectProductCommand(Guid Id, Guid UserId) : IRequest<Result>;
