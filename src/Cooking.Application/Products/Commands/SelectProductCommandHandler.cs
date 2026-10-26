using FluentResults;
using MediatR;

namespace Cooking.Application.Products.Commands;

public class SelectProductCommandHandler(IProductRepositoryService repository)
    : IRequestHandler<SelectProductCommand, Result>
{
    public Task<Result> Handle(SelectProductCommand request, CancellationToken cancellationToken) =>
        repository.SelectAsync(request.Id, request.UserId, cancellationToken);
}
