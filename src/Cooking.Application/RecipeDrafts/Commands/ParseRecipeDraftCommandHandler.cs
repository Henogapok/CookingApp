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
        var sourceResult = await drafts.GetSourceAsync(request.DraftId, cancellationToken);
        if (sourceResult.IsFailed)
            return sourceResult.ToResult(); // черновик успели удалить — разбирать нечего

        var source = sourceResult.Value;

        // Черновик уже разобран и правки нет — делать нечего (например, задача пришла повторно).
        if (source.Content is not null && source.PendingCorrection is null)
            return Result.Ok();

        var isCorrection = source.Content is not null;

        var catalogResult = await catalog.GetAllAsync(cancellationToken);
        var tagsResult = await tags.GetAllAsync(null, cancellationToken);
        if (catalogResult.IsFailed || tagsResult.IsFailed)
            return await FailAsync(source, isCorrection, RecipeDraftFailureReason.ParserError,
                Result.Merge(catalogResult.ToResult(), tagsResult.ToResult()), cancellationToken);

        var parsed = await parser.ParseAsync(
            new RecipeParsingRequest(
                source.SourceText,
                catalogResult.Value.Select(i => i.Name).ToList(),
                tagsResult.Value.Select(t => t.Name).ToList(),
                isCorrection ? RecipeDraftMapper.ToCorrectionJson(source.Content!, tagsResult.Value.ToDictionary(t => t.Id, t => t.Name)) : null,
                source.PendingCorrection),
            cancellationToken);

        if (parsed.IsFailed)
        {
            var reason = parsed.Errors.OfType<AppError>().FirstOrDefault()?.Code == ErrorCode.Unavailable
                ? RecipeDraftFailureReason.ParserUnavailable
                : RecipeDraftFailureReason.ParserError;

            return await FailAsync(source, isCorrection, reason, parsed.ToResult(), cancellationToken);
        }

        var content = RecipeDraftMapper.ToDraftContent(
            parsed.Value,
            ToKeyMap(catalogResult.Value.Select(i => (i.Name, i.Id))),
            ToKeyMap(tagsResult.Value.Select(t => (t.Name, t.Id))));

        if (content is null)
        {
            // Не ошибка сервиса: пользователь прислал не рецепт (или правка «удалила» весь рецепт).
            await FailAsync(source, isCorrection, RecipeDraftFailureReason.NotARecipe, Result.Ok(), cancellationToken);
            return Result.Ok();
        }

        // После правки пользователь не должен заново нажимать «Оценить».
        if (isCorrection)
            content = content with { UseEstimates = source.Content!.UseEstimates };

        var saved = await drafts.SetContentAsync(source.Id, content, cancellationToken);
        if (saved.IsFailed)
            return saved; // черновик удалили, пока шёл разбор, — сообщать некому

        await notifier.DraftReadyAsync(source.Id, source.UserId, cancellationToken);
        return Result.Ok();
    }

    /// <summary>
    /// Неудачный первый разбор удаляет черновик; неудачная правка — только снимает её, прежняя версия остаётся.
    /// </summary>
    private async Task<Result> FailAsync(
        RecipeDraftSource source, bool isCorrection, RecipeDraftFailureReason reason, Result error, CancellationToken cancellationToken)
    {
        if (isCorrection)
        {
            await drafts.ClearCorrectionAsync(source.Id, cancellationToken);
            await notifier.DraftCorrectionFailedAsync(source.Id, source.UserId, reason, cancellationToken);
        }
        else
        {
            await drafts.DiscardAsync(source.Id, cancellationToken);
            await notifier.DraftFailedAsync(source.UserId, reason, cancellationToken);
        }

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
