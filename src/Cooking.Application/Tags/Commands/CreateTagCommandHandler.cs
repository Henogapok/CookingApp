using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Commands;

public class CreateTagCommandHandler(ITagRepositoryService repository)
    : IRequestHandler<CreateTagCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateTagCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request.Name, request.TagTypeId, cancellationToken);
}
