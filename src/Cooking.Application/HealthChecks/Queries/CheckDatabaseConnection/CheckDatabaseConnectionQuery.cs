using MediatR;

namespace Cooking.Application.HealthChecks.Queries.CheckDatabaseConnection;

public record CheckDatabaseConnectionQuery : IRequest<bool>;
