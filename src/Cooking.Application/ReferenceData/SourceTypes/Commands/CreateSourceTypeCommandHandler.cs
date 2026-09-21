using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public class CreateSourceTypeCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<CreateSourceTypeCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateSourceTypeCommand request, CancellationToken cancellationToken) =>
        repository.CreateSourceTypeAsync(request.Name, cancellationToken);
}
