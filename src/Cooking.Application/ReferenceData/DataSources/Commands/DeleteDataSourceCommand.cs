using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public record DeleteDataSourceCommand(Guid Id) : IRequest<Result>;
