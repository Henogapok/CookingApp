using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Commands;

public class UpdateTagCommandHandler(ITagRepositoryService repository)
    : IRequestHandler<UpdateTagCommand, Result>
{
    public Task<Result> Handle(UpdateTagCommand request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(request.Id, request.Name, request.TagTypeId, cancellationToken);
}
