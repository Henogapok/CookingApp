using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Queries;

public record GetReferenceEntityByIdQuery<TEntity>(Guid Id) : IRequest<Result<ReferenceEntityDto>>
    where TEntity : ReferenceEntity;
