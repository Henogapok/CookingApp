using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public class UpdateTagTypeCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<UpdateTagTypeCommand, Result>
{
    public Task<Result> Handle(UpdateTagTypeCommand request, CancellationToken cancellationToken) =>
        repository.UpdateTagTypeAsync(request.Id, request.Name, cancellationToken);
}
