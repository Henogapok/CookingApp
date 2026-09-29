using Cooking.Application.Common.Errors;
using Cooking.Application.Ingredients;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Application.Tags;
using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class ParseRecipeDraftCommandHandler(
    IRecipeDraftRepositoryService drafts,
    IIngredientCatalogRepositoryService catalog,
    ITagRepositoryService tags,
    IRecipeTextParser parser,
    IRecipeDraftNotifier notifier)
    : IRequestHandler<ParseRecipeDraftCommand, Result>
{
    public async Task<Result> Handle(ParseRecipeDraftCommand request, CancellationToken cancellationToken)
    {
        var source = await drafts.GetSourceAsync(request.DraftId, cancellationToken);
        if (source.IsFailed)
            return source.ToResult(); // черновик успели удалить — разбирать нечего

        var catalogResult = await catalog.GetAllAsync(cancellationToken);
        var tagsResult = await tags.GetAllAsync(null, cancellationToken);
        if (catalogResult.IsFailed || tagsResult.IsFailed)
            return await FailAsync(source.Value, RecipeDraftFailureReason.ParserError,
                Result.Merge(catalogResult.ToResult(), tagsResult.ToResult()), cancellationToken);

        var parsed = await parser.ParseAsync(
            new RecipeParsingRequest(
                source.Value.SourceText,
                catalogResult.Value.Select(i => i.Name).ToList(),
                tagsResult.Value.Select(t => t.Name).ToList()),
            cancellationToken);

        if (parsed.IsFailed)
        {
            var reason = parsed.Errors.OfType<AppError>().FirstOrDefault()?.Code == ErrorCode.Unavailable
                ? RecipeDraftFailureReason.ParserUnavailable
                : RecipeDraftFailureReason.ParserError;

            return await FailAsync(source.Value, reason, parsed.ToResult(), cancellationToken);
        }

        var content = RecipeDraftMapper.ToDraftContent(
            parsed.Value,
            ToKeyMap(catalogResult.Value.Select(i => (i.Name, i.Id))),
            ToKeyMap(tagsResult.Value.Select(t => (t.Name, t.Id))));

        if (content is null)
        {
            // Не ошибка: пользователь прислал не рецепт.
            await drafts.DiscardAsync(source.Value.Id, cancellationToken);
            await notifier.DraftFailedAsync(source.Value.UserId, RecipeDraftFailureReason.NotARecipe, cancellationToken);
            return Result.Ok();
        }

        var saved = await drafts.SetContentAsync(source.Value.Id, content, cancellationToken);
        if (saved.IsFailed)
            return await FailAsync(source.Value, RecipeDraftFailureReason.ParserError, saved, cancellationToken);

        await notifier.DraftReadyAsync(source.Value.Id, source.Value.UserId, cancellationToken);
        return Result.Ok();
    }

    private async Task<Result> FailAsync(
        RecipeDraftSource source, RecipeDraftFailureReason reason, Result error, CancellationToken cancellationToken)
    {
        await drafts.DiscardAsync(source.Id, cancellationToken);
        await notifier.DraftFailedAsync(source.UserId, reason, cancellationToken);
        return error;
    }

    /// <summary>Названия в каталоге уникальны, но NameKey может склеить, например, «Ёж» и «еж» — берём первое.</summary>
    private static Dictionary<string, Guid> ToKeyMap(IEnumerable<(string Name, Guid Id)> items)
    {
        var map = new Dictionary<string, Guid>();
        foreach (var (name, id) in items)
            map.TryAdd(RecipeDraftMapper.NameKey(name), id);
        return map;
    }
}
