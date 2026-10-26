using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public record UpdateProductCommand(Guid Id, Guid UserId, ProductFields Product) : IRequest<Result>;
