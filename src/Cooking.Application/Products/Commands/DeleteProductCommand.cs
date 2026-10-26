using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public record DeleteProductCommand(Guid Id, Guid UserId) : IRequest<Result>;
