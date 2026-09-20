using Cooking.Domain.Entities.Common;
using FluentValidation;

namespace Cooking.Application.ReferenceData.Commands;

public class UpdateReferenceEntityCommandValidator<TEntity> : AbstractValidator<UpdateReferenceEntityCommand<TEntity>>
    where TEntity : ReferenceEntity
{
    public UpdateReferenceEntityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
