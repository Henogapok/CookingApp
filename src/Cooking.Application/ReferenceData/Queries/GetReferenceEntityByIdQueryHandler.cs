using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Queries;

public class GetReferenceEntityByIdQueryHandler<TEntity>(IDataContext dataContext)
    : IRequestHandler<GetReferenceEntityByIdQuery<TEntity>, Result<ReferenceEntityDto>>
    where TEntity : ReferenceEntity
{
    public async Task<Result<ReferenceEntityDto>> Handle(
        GetReferenceEntityByIdQuery<TEntity> request,
        CancellationToken cancellationToken)
    {
        var entity = await dataContext.Set<TEntity>().FindAsync([request.Id], cancellationToken);

        return entity is null
            ? Result.Fail(new AppError($"{typeof(TEntity).Name} with id '{request.Id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(new ReferenceEntityDto(entity.Id, entity.Name));
    }
}
