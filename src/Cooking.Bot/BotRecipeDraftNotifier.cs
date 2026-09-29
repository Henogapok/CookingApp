using Cooking.Application.RecipeDrafts;
using Cooking.Application.RecipeDrafts.Queries;
using Cooking.Application.Users.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Cooking.Bot;

/// <summary>
/// Итог фонового разбора — сообщением в Telegram. Личный чат с ботом имеет тот же Id, что и пользователь,
/// поэтому писать можно по TelegramId, без хранения чата.
/// </summary>
public class BotRecipeDraftNotifier(
    ITelegramBotClient bot,
    ISender mediator,
    ILogger<BotRecipeDraftNotifier> logger) : IRecipeDraftNotifier
{
    public async Task DraftReadyAsync(Guid draftId, Guid userId, CancellationToken cancellationToken)
    {
        if (await GetChatIdAsync(userId, cancellationToken) is not { } chatId)
            return;

        await SendPreviewAsync(chatId, draftId, userId, cancellationToken);
    }

    public async Task DraftCorrectionFailedAsync(
        Guid draftId, Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken)
    {
        if (await GetChatIdAsync(userId, cancellationToken) is not { } chatId)
            return;

        var text = reason switch
        {
            RecipeDraftFailureReason.NotARecipe => "После такой правки от рецепта ничего не осталось 🤔 Оставил прежнюю версию.",
            RecipeDraftFailureReason.ParserUnavailable => "Разбор рецептов пока не настроен 😔 Оставил прежнюю версию.",
            _ => "Не получилось применить правку 😔 Оставил прежнюю версию — попробуй ещё раз.",
        };

        await bot.SendMessage(chatId, text, cancellationToken: cancellationToken);

        // Старое превью уже без кнопок — присылаем актуальную версию, чтобы можно было продолжить.
        await SendPreviewAsync(chatId, draftId, userId, cancellationToken);
    }

    private async Task SendPreviewAsync(long chatId, Guid draftId, Guid userId, CancellationToken cancellationToken)
    {
        var draft = await mediator.Send(new GetRecipeDraftQuery(draftId, userId), cancellationToken);
        if (draft.IsFailed)
        {
            logger.LogWarning("Recipe draft {DraftId} could not be loaded: {Errors}",
                draftId, string.Join("; ", draft.Errors.Select(e => e.Message)));
            return;
        }

        await bot.SendMessage(chatId, BotRecipeFormatter.FormatDraft(draft.Value),
            parseMode: ParseMode.Html,
            replyMarkup: BotKeyboards.DraftActions(draftId, draft.Value.CanApplyEstimates),
            cancellationToken: cancellationToken);
    }

    public async Task DraftFailedAsync(Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken)
    {
        if (await GetChatIdAsync(userId, cancellationToken) is not { } chatId)
            return;

        var text = reason switch
        {
            RecipeDraftFailureReason.NotARecipe =>
                "Не нашёл тут рецепта 🤔 Пришли текст, где есть ингредиенты или шаги приготовления.",
            RecipeDraftFailureReason.ParserUnavailable =>
                "Разбор рецептов пока не настроен 😔 Загляни чуть позже.",
            _ => "Не получилось разобрать рецепт 😔 Попробуй прислать его ещё раз чуть позже.",
        };

        await bot.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }

    private async Task<long?> GetChatIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
        if (user.IsSuccess)
            return user.Value.TelegramId;

        logger.LogWarning("Cannot notify user {UserId} about recipe draft: user not found", userId);
        return null;
    }
}
