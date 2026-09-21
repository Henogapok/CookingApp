using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public class UpdateComplexityCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<UpdateComplexityCommand, Result>
{
    public Task<Result> Handle(UpdateComplexityCommand request, CancellationToken cancellationToken) =>
        repository.UpdateComplexityAsync(request.Id, request.Name, cancellationToken);
}
