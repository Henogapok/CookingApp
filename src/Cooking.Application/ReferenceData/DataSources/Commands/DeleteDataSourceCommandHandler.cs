using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public class DeleteDataSourceCommandHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<DeleteDataSourceCommand, Result>
{
    public Task<Result> Handle(DeleteDataSourceCommand request, CancellationToken cancellationToken) =>
        repository.DeleteDataSourceAsync(request.Id, cancellationToken);
}
