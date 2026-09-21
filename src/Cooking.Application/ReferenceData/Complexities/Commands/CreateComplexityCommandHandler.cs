using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public class CreateComplexityCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<CreateComplexityCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateComplexityCommand request, CancellationToken cancellationToken) =>
        repository.CreateComplexityAsync(request.Name, cancellationToken);
}
