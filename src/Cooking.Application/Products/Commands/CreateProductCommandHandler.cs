using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public class CreateProductCommandHandler(IProductRepositoryService repository)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request.UserId, request.Product, cancellationToken);
}
