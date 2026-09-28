using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Commands;

public class DeleteTagCommandHandler(ITagRepositoryService repository)
    : IRequestHandler<DeleteTagCommand, Result>
{
    public Task<Result> Handle(DeleteTagCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
