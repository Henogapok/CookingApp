using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Queries;

public record GetSourceTypeByIdQuery(Guid Id) : IRequest<Result<ReferenceEntityDto>>;
