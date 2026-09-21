using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public class CreateDataSourceCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<CreateDataSourceCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateDataSourceCommand request, CancellationToken cancellationToken) =>
        repository.CreateDataSourceAsync(request.Name, cancellationToken);
}
