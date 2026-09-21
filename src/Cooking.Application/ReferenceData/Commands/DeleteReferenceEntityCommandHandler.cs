using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Commands;

public class DeleteReferenceEntityCommandHandler<TEntity>(IDataContext dataContext)
    : IRequestHandler<DeleteReferenceEntityCommand<TEntity>, Result>
    where TEntity : ReferenceEntity
{
    public async Task<Result> Handle(DeleteReferenceEntityCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await dataContext.Set<TEntity>().FindAsync([request.Id], cancellationToken);

        if (entity is null)
            return Result.Fail(new AppError($"{typeof(TEntity).Name} with id '{request.Id}' was not found.", ErrorCode.NotFound));

        dataContext.Set<TEntity>().Remove(entity);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
