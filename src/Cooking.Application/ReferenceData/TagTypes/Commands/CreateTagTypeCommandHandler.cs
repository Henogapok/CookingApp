using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public class CreateTagTypeCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<CreateTagTypeCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateTagTypeCommand request, CancellationToken cancellationToken) =>
        repository.CreateTagTypeAsync(request.Name, cancellationToken);
}
