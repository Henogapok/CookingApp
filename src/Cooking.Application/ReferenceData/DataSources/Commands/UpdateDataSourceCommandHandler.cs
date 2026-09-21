using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public class UpdateDataSourceCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<UpdateDataSourceCommand, Result>
{
    public Task<Result> Handle(UpdateDataSourceCommand request, CancellationToken cancellationToken) =>
        repository.UpdateDataSourceAsync(request.Id, request.Name, cancellationToken);
}
