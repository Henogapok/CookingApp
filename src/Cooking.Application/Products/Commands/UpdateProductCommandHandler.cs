using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public class UpdateProductCommandHandler(IProductRepositoryService repository)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(request.Id, request.UserId, request.Product, cancellationToken);
}
