using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Queries;

public record GetTagTypeByIdQuery(Guid Id) : IRequest<Result<ReferenceEntityDto>>;
