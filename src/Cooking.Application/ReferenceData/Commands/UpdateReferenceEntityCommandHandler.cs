using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Commands;

public class UpdateReferenceEntityCommandHandler<TEntity>(IDataContext dataContext)
    : IRequestHandler<UpdateReferenceEntityCommand<TEntity>, Result>
    where TEntity : ReferenceEntity
{
    public async Task<Result> Handle(UpdateReferenceEntityCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await dataContext.Set<TEntity>().FindAsync([request.Id], cancellationToken);

        if (entity is null)
            return Result.Fail(new AppError($"{typeof(TEntity).Name} with id '{request.Id}' was not found.", ErrorCode.NotFound));

        entity.Name = request.Name;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
