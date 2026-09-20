using Cooking.Domain.Entities.Common;
using FluentValidation;

namespace Cooking.Application.ReferenceData.Commands;

public class CreateReferenceEntityCommandValidator<TEntity> : AbstractValidator<CreateReferenceEntityCommand<TEntity>>
    where TEntity : ReferenceEntity, new()
{
    public CreateReferenceEntityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
