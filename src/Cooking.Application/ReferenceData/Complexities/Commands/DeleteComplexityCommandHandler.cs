using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public class DeleteComplexityCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<DeleteComplexityCommand, Result>
{
    public Task<Result> Handle(DeleteComplexityCommand request, CancellationToken cancellationToken) =>
        repository.DeleteComplexityAsync(request.Id, cancellationToken);
}
