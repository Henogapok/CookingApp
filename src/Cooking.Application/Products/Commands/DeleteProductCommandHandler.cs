using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public class DeleteProductCommandHandler(IProductRepositoryService repository)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, request.UserId, cancellationToken);
}
