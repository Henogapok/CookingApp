using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Queries;

public record GetReferenceEntitiesQuery<TEntity> : IRequest<Result<List<ReferenceEntityDto>>>
    where TEntity : ReferenceEntity;
