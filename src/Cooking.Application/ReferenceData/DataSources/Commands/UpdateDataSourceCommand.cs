using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public record UpdateDataSourceCommand(Guid Id, string Name) : IRequest<Result>;
