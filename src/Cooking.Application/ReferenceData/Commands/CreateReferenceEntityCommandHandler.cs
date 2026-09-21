using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Commands;

public class CreateReferenceEntityCommandHandler<TEntity>(IDataContext dataContext)
    : IRequestHandler<CreateReferenceEntityCommand<TEntity>, Result<Guid>>
    where TEntity : ReferenceEntity, new()
{
    public async Task<Result<Guid>> Handle(CreateReferenceEntityCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = new TEntity { Name = request.Name };

        dataContext.Set<TEntity>().Add(entity);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(entity.Id);
    }
}
