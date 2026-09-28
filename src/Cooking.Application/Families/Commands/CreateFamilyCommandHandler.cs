using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public class CreateFamilyCommandHandler(IFamilyRepositoryService repository)
    : IRequestHandler<CreateFamilyCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateFamilyCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request.UserId, request.Name, cancellationToken);
}
