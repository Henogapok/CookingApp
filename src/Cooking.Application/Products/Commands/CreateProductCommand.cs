using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public record CreateProductCommand(Guid UserId, ProductFields Product) : IRequest<Result<Guid>>;
