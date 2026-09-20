using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.ReferenceData.Queries;

public class GetReferenceEntitiesQueryHandler<TEntity>(IDataContext dataContext)
    : IRequestHandler<GetReferenceEntitiesQuery<TEntity>, Result<List<ReferenceEntityDto>>>
    where TEntity : ReferenceEntity
{
    public async Task<Result<List<ReferenceEntityDto>>> Handle(
        GetReferenceEntitiesQuery<TEntity> request,
        CancellationToken cancellationToken)
    {
        var entities = await dataContext.Set<TEntity>()
            .OrderBy(e => e.Name)
            .Select(e => new ReferenceEntityDto(e.Id, e.Name))
            .ToListAsync(cancellationToken);

        return Result.Ok(entities);
    }
}
