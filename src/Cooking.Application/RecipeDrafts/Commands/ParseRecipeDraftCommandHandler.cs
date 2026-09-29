using Cooking.Application.Common.Errors;
using Cooking.Application.Ingredients;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Application.RecipeDrafts.Sources;
using Cooking.Domain.ReferenceData;
using Cooking.Application.Tags;
using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class ParseRecipeDraftCommandHandler(
    IRecipeDraftRepositoryService drafts,
    IIngredientCatalogRepositoryService catalog,
    ITagRepositoryService tags,
    IRecipeTextParser parser,
    IRecipeDraftNotifier notifier,
    IVideoSourceLoader videoLoader,
    ISpeechToText speechToText)
    : IRequestHandler<ParseRecipeDraftCommand, Result>
{
    public async Task<Result> Handle(ParseRecipeDraftCommand request, CancellationToken cancellationToken)
    {
        var sourceResult = await drafts.GetSourceAsync(request.DraftId, cancellationToken);
        if (sourceResult.IsFailed)
            return sourceResult.ToResult(); // черновик успели удалить — разбирать нечего

        var source = sourceResult.Value;

        // Черновик уже разобран и правки нет, или ждёт выбора блюд — делать нечего (например, задача пришла повторно).
        if ((source.Content is not null && source.PendingCorrection is null) || source.IsAwaitingDishChoice)
            return Result.Ok();

        var isCorrection = source.Content is not null;

        // Черновик из видео: сначала получаем текст (описание + расшифровка), дальше — как с обычным текстом.
        if (!source.IsSourceLoaded)
        {
            var (text, failure) = await LoadVideoTextAsync(source, cancellationToken);
            if (text is null)
                return await FailAsync(source, isCorrection: false, failure.Reason, failure.Error, cancellationToken);

            await drafts.SetSourceTextAsync(source.Id, text, cancellationToken);
            source = source with { SourceText = text, MediaFilePath = null, IsSourceLoaded = true };
        }

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
                source.PendingCorrection,
                isCorrection ? null : source.SelectedDishes),
            cancellationToken);

        if (parsed.IsFailed)
        {
            var reason = parsed.Errors.OfType<AppError>().FirstOrDefault()?.Code == ErrorCode.Unavailable
                ? RecipeDraftFailureReason.ParserUnavailable
                : RecipeDraftFailureReason.ParserError;

            return await FailAsync(source, isCorrection, reason, parsed.ToResult(), cancellationToken);
        }

        var catalogIds = ToKeyMap(catalogResult.Value.Select(i => (i.Name, i.Id)));
        var tagIds = ToKeyMap(tagsResult.Value.Select(t => (t.Name, t.Id)));
        var contents = (parsed.Value.Recipes ?? [])
            .Select(recipe => RecipeDraftMapper.ToDraftContent(recipe, catalogIds, tagIds))
            .OfType<RecipeDraftContent>()
            .ToList();

        return isCorrection
            ? await ApplyCorrectionAsync(source, contents.FirstOrDefault(), cancellationToken)
            : await ApplyFirstParseAsync(source, parsed.Value, contents, cancellationToken);
    }

    /// <summary>
    /// Каждое блюдо — свой черновик: первое кладём в этот, остальные — в новые с тем же исходным текстом.
    /// Блюд больше лимита (и пользователь ещё не выбирал) — предлагаем выбрать.
    /// </summary>
    private async Task<Result> ApplyFirstParseAsync(
        RecipeDraftSource source, ParsedRecipes parsed, List<RecipeDraftContent> contents, CancellationToken cancellationToken)
    {
        if (contents.Count == 0 && source.SelectedDishes is null
            && RecipeDraftMapper.ToDishChoices(parsed.Dishes) is { Count: > RecipeDraftLimits.MaxDishes } dishes)
        {
            var choiceSaved = await drafts.SetDishChoicesAsync(source.Id, dishes, cancellationToken);
            if (choiceSaved.IsFailed)
                return choiceSaved; // черновик удалили, пока шёл разбор

            await notifier.DishChoiceRequiredAsync(source.Id, source.UserId, dishes, cancellationToken);
            return Result.Ok();
        }

        if (contents.Count == 0)
        {
            // Не ошибка сервиса: пользователь прислал не рецепт.
            await FailAsync(source, isCorrection: false, RecipeDraftFailureReason.NotARecipe, Result.Ok(), cancellationToken);
            return Result.Ok();
        }

        // LLM мог и не послушаться лимита — лишнее отбрасываем.
        contents = contents.Take(RecipeDraftLimits.MaxDishes).ToList();

        var draftIds = new List<Guid>();
        for (var i = 0; i < contents.Count; i++)
        {
            var content = contents[i];

            // Рецепт из Reels помнит, откуда он.
            if (source.SourceUrl is not null)
                content = content with { SourceUrl = source.SourceUrl, SourceTypeId = ReferenceIds.SourceTypes.Instagram };

            if (contents.Count > 1)
                content = content with { DishNumber = i + 1, DishCount = contents.Count };

            if (i == 0)
            {
                var saved = await drafts.SetContentAsync(source.Id, content, cancellationToken);
                if (saved.IsFailed)
                    return saved; // черновик удалили, пока шёл разбор, — сообщать некому

                draftIds.Add(source.Id);
            }
            else
            {
                var sibling = await drafts.CreateSiblingAsync(source.Id, content, cancellationToken);
                if (sibling.IsFailed)
                    return sibling.ToResult();

                draftIds.Add(sibling.Value);
            }
        }

        foreach (var draftId in draftIds)
            await notifier.DraftReadyAsync(draftId, source.UserId, cancellationToken);

        return Result.Ok();
    }

    private async Task<Result> ApplyCorrectionAsync(
        RecipeDraftSource source, RecipeDraftContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            // Правка «удалила» весь рецепт.
            await FailAsync(source, isCorrection: true, RecipeDraftFailureReason.NotARecipe, Result.Ok(), cancellationToken);
            return Result.Ok();
        }

        // После правки не должны слетать «Оценить», привязка к изменяемому рецепту (с его источником) и номер блюда.
        var current = source.Content!;
        content = content with
        {
            UseEstimates = current.UseEstimates,
            RecipeId = current.RecipeId,
            SourceUrl = current.SourceUrl,
            SourceTypeId = current.SourceTypeId,
            DishNumber = current.DishNumber,
            DishCount = current.DishCount,
        };

        var saved = await drafts.SetContentAsync(source.Id, content, cancellationToken);
        if (saved.IsFailed)
            return saved; // черновик удалили, пока шёл разбор, — сообщать некому

        await notifier.DraftReadyAsync(source.Id, source.UserId, cancellationToken);
        return Result.Ok();
    }

    /// <summary>
    /// Текст из видео: описание (из Instagram или из подписи к присланному файлу) + расшифровка речи.
    /// Text = null — не получилось: видео не скачалось (VideoUnavailable) или текста нет ни там, ни там (NoTextInVideo).
    /// Расшифровка не обязательна: без ключа или при сбое сервиса работаем по одному описанию.
    /// </summary>
    private async Task<(string? Text, (RecipeDraftFailureReason Reason, Result Error) Failure)> LoadVideoTextAsync(
        RecipeDraftSource source, CancellationToken cancellationToken)
    {
        string? caption;
        string? transcript;

        if (source.MediaFilePath is { } mediaFile)
        {
            caption = source.SourceText;
            try
            {
                transcript = await TranscribeAsync(mediaFile, cancellationToken);
            }
            finally
            {
                TryDelete(mediaFile);
            }
        }
        else if (source.SourceUrl is { } url)
        {
            var video = await videoLoader.LoadAsync(url, cancellationToken);
            if (video.IsFailed)
                return (null, (RecipeDraftFailureReason.VideoUnavailable, video.ToResult()));

            using var _ = video.Value;
            caption = video.Value.Caption;
            transcript = video.Value.AudioFilePath is { } audio ? await TranscribeAsync(audio, cancellationToken) : null;
        }
        else
        {
            caption = source.SourceText;
            transcript = null;
        }

        return RecipeSourceText.Build(caption, transcript) is { } text
            ? (text, default)
            : (null, (RecipeDraftFailureReason.NoTextInVideo, Result.Ok())); // не ошибка сервиса — просто нечего разбирать
    }

    private async Task<string?> TranscribeAsync(string mediaFile, CancellationToken cancellationToken)
    {
        var transcript = await speechToText.TranscribeAsync(mediaFile, cancellationToken);
        return transcript.IsSuccess ? transcript.Value : null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Временный файл — не страшно, если не удалился сразу.
        }
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
            if (source.MediaFilePath is { } mediaFile)
                TryDelete(mediaFile);

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
