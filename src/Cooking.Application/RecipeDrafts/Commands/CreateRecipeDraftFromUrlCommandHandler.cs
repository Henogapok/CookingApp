using Cooking.Application.RecipeDrafts.Sources;
using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CreateRecipeDraftFromUrlCommandHandler(IRecipeDraftRepositoryService drafts, IRecipeParsingQueue queue)
    : IRequestHandler<CreateRecipeDraftFromUrlCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRecipeDraftFromUrlCommand request, CancellationToken cancellationToken)
    {
        // Валидатор уже проверил, что ссылка есть; храним её в нормальном виде, без хвостов ?igsh=….
        var url = RecipeSourceText.FindInstagramLink(request.Url)!;

        var result = await drafts.CreateFromMediaAsync(request.UserId, url, mediaFilePath: null, caption: null, cancellationToken);
        if (result.IsFailed)
            return result;

        await queue.EnqueueAsync(new RecipeParsingJob(result.Value), cancellationToken);
        return result;
    }
}
