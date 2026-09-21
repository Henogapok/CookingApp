using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public class DeleteTagTypeCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<DeleteTagTypeCommand, Result>
{
    public Task<Result> Handle(DeleteTagTypeCommand request, CancellationToken cancellationToken) =>
        repository.DeleteTagTypeAsync(request.Id, cancellationToken);
}
