using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public class UpdateSourceTypeCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<UpdateSourceTypeCommand, Result>
{
    public Task<Result> Handle(UpdateSourceTypeCommand request, CancellationToken cancellationToken) =>
        repository.UpdateSourceTypeAsync(request.Id, request.Name, cancellationToken);
}
