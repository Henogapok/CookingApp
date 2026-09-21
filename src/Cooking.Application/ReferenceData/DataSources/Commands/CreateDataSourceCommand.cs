using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public record CreateDataSourceCommand(string Name) : IRequest<Result<Guid>>;
