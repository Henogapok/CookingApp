using Cooking.Application.Common.Interfaces;
using MediatR;

namespace Cooking.Application.HealthChecks.Queries.CheckDatabaseConnection;

public class CheckDatabaseConnectionQueryHandler(IDataContext dataContext)
    : IRequestHandler<CheckDatabaseConnectionQuery, bool>
{
    public Task<bool> Handle(CheckDatabaseConnectionQuery request, CancellationToken cancellationToken)
        => dataContext.CanConnectAsync(cancellationToken);
}
