using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public class DeleteSourceTypeCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<DeleteSourceTypeCommand, Result>
{
    public Task<Result> Handle(DeleteSourceTypeCommand request, CancellationToken cancellationToken) =>
        repository.DeleteSourceTypeAsync(request.Id, cancellationToken);
}
