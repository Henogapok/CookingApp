using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Queries;

public record GetDataSourcesQuery : IRequest<Result<List<ReferenceEntityDto>>>;
