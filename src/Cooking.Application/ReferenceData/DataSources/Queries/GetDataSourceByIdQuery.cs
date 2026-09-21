using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Queries;

public record GetDataSourceByIdQuery(Guid Id) : IRequest<Result<ReferenceEntityDto>>;
