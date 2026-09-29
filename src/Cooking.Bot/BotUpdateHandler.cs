using Cooking.Application.Common.Errors;
using Cooking.Application.Families.Commands;
using Cooking.Application.Families.Queries;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.RecipeDrafts.Commands;
using Cooking.Application.RecipeDrafts.Queries;
using Cooking.Application.Recipes;
using Cooking.Application.Recipes.Queries;
using Cooking.Application.Users;
using Cooking.Application.Users.Commands;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUser = Telegram.Bot.Types.User;

namespace Cooking.Bot;

/// <summary>
/// Вся логика бота. Не знает, как пришёл Update — через polling (разработка) или webhook (прод):
/// оба транспорта создают scope и вызывают HandleAsync.
/// </summary>
public class BotUpdateHandler(
    ITelegramBotClient bot,
    ISender mediator,
    BotInfoProvider botInfo,
    ILogger<BotUpdateHandler> logger)
{
    private const string GenericErrorText = "Что-то пошло не так 😔 Попробуй ещё раз чуть позже.";

    /// <summary>Текст короче — скорее случайное сообщение, чем рецепт: не тратим на него запрос к LLM.</summary>
    private const int MinRecipeTextLength = 40;

    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case { Message: { Text: { } text, From: { } from, Chat.Type: ChatType.Private } message }:
                await HandleMessageAsync(message.Chat.Id, from, text.Trim(), message.ReplyToMessage, cancellationToken);
                break;

            case { CallbackQuery: { Data: { } data, From: { } from } callback }:
                await HandleCallbackAsync(callback, from, data, cancellationToken);
                break;
        }
    }

    private async Task HandleMessageAsync(long chatId, TelegramUser from, string text, Message? replyTo, CancellationToken cancellationToken)
    {
        // Регистрация идемпотентна, поэтому вызываем её на каждое сообщение: так бот работает, даже если
        // пользователь не нажимал /start (например, после очистки БД), а имя в профиле остаётся актуальным.
        var user = await RegisterAsync(from, cancellationToken);
        if (user is null)
        {
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        if (BotCommandNames.MatchArguments(text, BotCommandNames.Search) is { } searchQuery)
        {
            // "/search" без аргументов (например, из меню команд) — как кнопка «Найти рецепт».
            if (searchQuery.Length > 0)
                await SearchRecipesAsync(chatId, user, searchQuery, cancellationToken);
            else
                await ShowRecipesAsync(chatId, user, cancellationToken);

            return;
        }

        if (BotCommandNames.MatchArguments(text, BotCommandNames.Start) is { } payload)
        {
            // Deep link t.me/<bot>?start=<code> приходит как сообщение "/start <code>".
            if (payload.Length > 0)
                await JoinByInviteAsync(chatId, user, payload, cancellationToken);
            else
                await SendGreetingAsync(chatId, user, cancellationToken);

            return;
        }

        switch (text)
        {
            case BotButtons.CreateRecipe:
                await bot.SendMessage(chatId, BotTexts.RecipePrompt,
                    replyMarkup: BotKeyboards.RecipeReply(), cancellationToken: cancellationToken);
                break;

            case BotButtons.FindRecipe:
                await ShowRecipesAsync(chatId, user, cancellationToken);
                break;

            // Кнопки меню проверяются раньше: нажатие кнопки при открытом поле ответа тоже может прийти как reply.
            case var query when replyTo is { From.IsBot: true, Text: BotTexts.SearchPrompt }:
                await SearchRecipesAsync(chatId, user, query, cancellationToken);
                break;

            case var recipeText when replyTo is { From.IsBot: true, Text: BotTexts.RecipePrompt }:
                await CreateDraftAsync(chatId, user, recipeText, cancellationToken);
                break;

            // Ответ на превью черновика — правка («лука не надо, порций 4»).
            case var correction when replyTo is { From.IsBot: true } && BotCallbacks.FindDraftId(replyTo.ReplyMarkup) is { } draftId:
                await CorrectDraftAsync(chatId, replyTo.MessageId, user, draftId, correction, cancellationToken);
                break;

            case BotButtons.CreateFamily:
                await CreateFamilyAsync(chatId, user, cancellationToken);
                break;

            case BotButtons.JoinFamily:
                await bot.SendMessage(chatId,
                    "Попроси кого-нибудь из семьи нажать «Моя семья» → «Пригласить» и прислать тебе ссылку. " +
                    "Открой её — и ты сразу окажешься в семье.",
                    replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
                break;

            case BotButtons.MyFamily:
                await ShowFamilyAsync(chatId, user, cancellationToken);
                break;

            // Обычный текст без команды — это рецепт (поиск — только через /search или «Найти рецепт»).
            case var recipeText when recipeText.Length >= MinRecipeTextLength:
                await CreateDraftAsync(chatId, user, recipeText, cancellationToken);
                break;

            default:
                await bot.SendMessage(chatId,
                    "Не понял 🤔 Чтобы добавить рецепт, пришли его текст целиком. Остальное — в меню ниже.",
                    replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, TelegramUser from, string data, CancellationToken cancellationToken)
    {
        // Telegram показывает "часики" на кнопке, пока на callback не ответили.
        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

        var chatId = callback.Message?.Chat.Id ?? from.Id;
        var user = await RegisterAsync(from, cancellationToken);
        if (user is null)
        {
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        switch (data)
        {
            case BotCallbacks.FamilyInvite:
                await CreateInviteAsync(chatId, user, cancellationToken);
                break;

            case BotCallbacks.FamilyLeave when callback.Message is not null:
                await bot.EditMessageText(chatId, callback.Message.Id,
                    "Точно выйти из семьи? Твои рецепты останутся с тобой, но рецепты остальных участников ты больше не увидишь.",
                    replyMarkup: BotKeyboards.ConfirmLeave(), cancellationToken: cancellationToken);
                break;

            case BotCallbacks.FamilyLeaveConfirm:
                await LeaveFamilyAsync(chatId, user, callback.Message?.Id, cancellationToken);
                break;

            case BotCallbacks.FamilyLeaveCancel when callback.Message is not null:
                await bot.EditMessageText(chatId, callback.Message.Id, "Хорошо, остаёмся 🙂",
                    cancellationToken: cancellationToken);
                break;

            case var _ when BotCallbacks.MatchId(data, BotCallbacks.RecipePrefix) is { } recipeId:
                await ShowRecipeAsync(chatId, user, recipeId, cancellationToken);
                break;

            case var _ when BotCallbacks.MatchId(data, BotCallbacks.DraftSavePrefix) is { } draftId && callback.Message is not null:
                await ConfirmDraftAsync(chatId, callback.Message.Id, user, draftId, cancellationToken);
                break;

            case var _ when BotCallbacks.MatchId(data, BotCallbacks.DraftCancelPrefix) is { } draftId && callback.Message is not null:
                await CancelDraftAsync(chatId, callback.Message.Id, user, draftId, cancellationToken);
                break;

            case var _ when BotCallbacks.MatchId(data, BotCallbacks.DraftEstimatePrefix) is { } draftId && callback.Message is not null:
                await ApplyDraftEstimatesAsync(chatId, callback.Message.Id, user, draftId, cancellationToken);
                break;
        }
    }

    /// <summary>Все доступные рецепты кнопками + приглашение к поиску по названию.</summary>
    private async Task ShowRecipesAsync(long chatId, UserDto user, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRecipesQuery(user.Id, null), cancellationToken);
        if (result.IsFailed)
        {
            LogFailure(result, "list recipes", user);
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        if (result.Value.Count == 0)
        {
            await bot.SendMessage(chatId, "Рецептов пока нет 📭 Нажми «Создать рецепт» или просто пришли мне текст рецепта.",
                replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
            return;
        }

        await SendRecipeListAsync(chatId, user, result.Value, $"📚 Рецепты ({result.Value.Count}):", cancellationToken);
        await bot.SendMessage(chatId, BotTexts.SearchPrompt,
            replyMarkup: BotKeyboards.SearchReply(), cancellationToken: cancellationToken);
    }

    private async Task SearchRecipesAsync(long chatId, UserDto user, string query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRecipesQuery(user.Id, query), cancellationToken);
        if (result.IsFailed)
        {
            // Validation — слишком длинный запрос; остальное — неожиданная ошибка.
            if (ErrorCodeOf(result) != ErrorCode.Validation)
                LogFailure(result, "search recipes", user);

            await bot.SendMessage(chatId,
                ErrorCodeOf(result) == ErrorCode.Validation ? "Слишком длинный запрос — попробуй покороче." : GenericErrorText,
                replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
            return;
        }

        if (result.Value.Count == 0)
        {
            await bot.SendMessage(chatId, $"По запросу «{query}» ничего не нашёл 🤷",
                replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
            return;
        }

        await SendRecipeListAsync(chatId, user, result.Value, $"🔍 Нашёл ({result.Value.Count}):", cancellationToken);
    }

    private async Task SendRecipeListAsync(
        long chatId, UserDto user, List<RecipeSummaryDto> recipes, string title, CancellationToken cancellationToken)
    {
        // Длинный столбец кнопок неудобен — показываем первые, остальное находится поиском.
        const int maxButtons = 20;

        var text = recipes.Count > maxButtons
            ? $"{title}\nПоказываю первые {maxButtons} — уточни поиск, чтобы найти остальные."
            : title;

        var buttons = recipes
            .Take(maxButtons)
            .Select(r => (r.Id, BotRecipeFormatter.FormatListButton(r, user.Id)));

        await bot.SendMessage(chatId, text,
            replyMarkup: BotKeyboards.RecipeList(buttons), cancellationToken: cancellationToken);
    }

    private async Task ShowRecipeAsync(long chatId, UserDto user, Guid recipeId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRecipeByIdQuery(recipeId, user.Id), cancellationToken);
        if (result.IsFailed)
        {
            // NotFound — рецепт удалили или автор ушёл из семьи, пока висела старая кнопка.
            if (ErrorCodeOf(result) != ErrorCode.NotFound)
                LogFailure(result, "get recipe", user);

            await bot.SendMessage(chatId,
                ErrorCodeOf(result) == ErrorCode.NotFound ? "Этот рецепт больше недоступен 🤷" : GenericErrorText,
                cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(chatId, BotRecipeFormatter.FormatCard(result.Value, user.Id),
            parseMode: ParseMode.Html,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Ставит текст в очередь на разбор. Сам разбор идёт в фоне (обработка апдейтов не ждёт LLM),
    /// превью пришлёт BotRecipeDraftNotifier.
    /// </summary>
    private async Task CreateDraftAsync(long chatId, UserDto user, string text, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateRecipeDraftCommand(user.Id, text), cancellationToken);
        if (result.IsFailed)
        {
            if (ErrorCodeOf(result) != ErrorCode.Validation)
                LogFailure(result, "create recipe draft", user);

            await bot.SendMessage(chatId,
                ErrorCodeOf(result) == ErrorCode.Validation ? "Слишком длинный текст — попробуй покороче." : GenericErrorText,
                replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(chatId, "⏳ Разбираю рецепт — обычно это меньше минуты.",
            replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
    }

    private async Task CorrectDraftAsync(
        long chatId, int previewMessageId, UserDto user, Guid draftId, string correction, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CorrectRecipeDraftCommand(draftId, user.Id, correction), cancellationToken);
        if (result.IsFailed)
        {
            if (ErrorCodeOf(result) == ErrorCode.Validation)
            {
                await bot.SendMessage(chatId, "Слишком длинная правка — попробуй покороче.", cancellationToken: cancellationToken);
                return;
            }

            await ReportDraftFailureAsync(chatId, user, result, "correct recipe draft", cancellationToken);
            return;
        }

        // Старое превью больше не актуально: убираем его кнопки, чтобы не сохранить прежнюю версию. Новое пришлёт notifier.
        var quoted = correction.Length > 200 ? correction[..200] + "…" : correction;
        await bot.EditMessageText(chatId, previewMessageId, $"✏️ Применяю правку: «{quoted}»",
            cancellationToken: cancellationToken);
    }

    private async Task ConfirmDraftAsync(long chatId, int messageId, UserDto user, Guid draftId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ConfirmRecipeDraftCommand(draftId, user.Id), cancellationToken);
        if (result.IsFailed)
        {
            await ReportDraftFailureAsync(chatId, user, result, "confirm recipe draft", cancellationToken);
            return;
        }

        // Превью превращается в карточку сохранённого рецепта — кнопки черновика исчезают.
        var recipe = await mediator.Send(new GetRecipeByIdQuery(result.Value, user.Id), cancellationToken);
        var text = recipe.IsSuccess
            ? "✅ Сохранил!\n\n" + BotRecipeFormatter.FormatCard(recipe.Value, user.Id)
            : "✅ Рецепт сохранён!";

        await bot.EditMessageText(chatId, messageId, text,
            parseMode: ParseMode.Html,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
            cancellationToken: cancellationToken);
    }

    private async Task CancelDraftAsync(long chatId, int messageId, UserDto user, Guid draftId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelRecipeDraftCommand(draftId, user.Id), cancellationToken);
        if (result.IsFailed)
        {
            await ReportDraftFailureAsync(chatId, user, result, "cancel recipe draft", cancellationToken);
            return;
        }

        await bot.EditMessageText(chatId, messageId, "❌ Черновик удалён. Можешь прислать текст рецепта заново.",
            cancellationToken: cancellationToken);
    }

    private async Task ApplyDraftEstimatesAsync(long chatId, int messageId, UserDto user, Guid draftId, CancellationToken cancellationToken)
    {
        var applied = await mediator.Send(new ApplyRecipeDraftEstimatesCommand(draftId, user.Id), cancellationToken);
        var draft = applied.IsSuccess
            ? await mediator.Send(new GetRecipeDraftQuery(draftId, user.Id), cancellationToken)
            : applied.ToResult<RecipeDraftDto>();

        if (draft.IsFailed)
        {
            await ReportDraftFailureAsync(chatId, user, draft, "apply recipe draft estimates", cancellationToken);
            return;
        }

        await bot.EditMessageText(chatId, messageId, BotRecipeFormatter.FormatDraft(draft.Value),
            parseMode: ParseMode.Html,
            replyMarkup: BotKeyboards.DraftActions(draftId, draft.Value.CanApplyEstimates),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// NotFound — черновик уже сохранён, отменён или истёк (нажата старая кнопка). Отвечаем новым сообщением,
    /// а не правкой: при двойном нажатии «Сохранить» правка затёрла бы только что показанную карточку.
    /// </summary>
    private async Task ReportDraftFailureAsync(
        long chatId, UserDto user, ResultBase result, string action, CancellationToken cancellationToken)
    {
        if (ErrorCodeOf(result) == ErrorCode.NotFound)
        {
            await bot.SendMessage(chatId,
                "Этот черновик уже сохранён, отменён или устарел 🤷 Пришли текст рецепта заново, если нужно.",
                cancellationToken: cancellationToken);
            return;
        }

        // LogicConflict — черновик ещё разбирается или к нему применяется правка.
        if (ErrorCodeOf(result) == ErrorCode.LogicConflict)
        {
            await bot.SendMessage(chatId,
                "⏳ Ещё обрабатываю этот черновик — пришлю обновлённую версию, тогда и продолжим.",
                cancellationToken: cancellationToken);
            return;
        }

        LogFailure(result, action, user);
        await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
    }

    private async Task SendGreetingAsync(long chatId, UserDto user, CancellationToken cancellationToken)
    {
        var text = user.FamilyId is null
            ? $"Привет, {user.FirstName}! 👋\n\n" +
              "Я помогаю хранить рецепты: пришли текст или ссылку на Reels — я разберу рецепт и посчитаю КБЖУ.\n\n" +
              "Рецептами можно делиться с семьёй: создай свою или вступи по приглашению."
            : $"С возвращением, {user.FirstName}! 👋 Ты в семье «{user.FamilyName}».";

        await bot.SendMessage(chatId, text,
            replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
    }

    private async Task CreateFamilyAsync(long chatId, UserDto user, CancellationToken cancellationToken)
    {
        if (user.FamilyId is not null)
        {
            await bot.SendMessage(chatId, $"Ты уже в семье «{user.FamilyName}».",
                replyMarkup: BotKeyboards.MainMenu(hasFamily: true), cancellationToken: cancellationToken);
            return;
        }

        // Название пока генерируем сами, чтобы не хранить состояние диалога "введи название".
        var result = await mediator.Send(new CreateFamilyCommand(user.Id, $"Семья {user.FirstName}"), cancellationToken);
        if (result.IsFailed)
        {
            LogFailure(result, "create family", user);
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(chatId,
            $"Готово! Создал «Семья {user.FirstName}» 🎉\n\nЧтобы позвать кого-нибудь, открой «Моя семья» → «Пригласить».",
            replyMarkup: BotKeyboards.MainMenu(hasFamily: true), cancellationToken: cancellationToken);
    }

    private async Task ShowFamilyAsync(long chatId, UserDto user, CancellationToken cancellationToken)
    {
        if (user.FamilyId is not { } familyId)
        {
            await bot.SendMessage(chatId, "Ты пока не в семье.",
                replyMarkup: BotKeyboards.MainMenu(hasFamily: false), cancellationToken: cancellationToken);
            return;
        }

        var result = await mediator.Send(new GetFamilyByIdQuery(familyId), cancellationToken);
        if (result.IsFailed)
        {
            LogFailure(result, "get family", user);
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        var members = string.Join("\n", result.Value.Members.Select(m =>
            $"• {m.FirstName}{(m.LastName is null ? "" : " " + m.LastName)}{(m.UserId == user.Id ? " (ты)" : "")}"));

        await bot.SendMessage(chatId, $"👨‍👩‍👧 {result.Value.Name}\n\nУчастники:\n{members}",
            replyMarkup: BotKeyboards.FamilyActions(), cancellationToken: cancellationToken);
    }

    private async Task CreateInviteAsync(long chatId, UserDto user, CancellationToken cancellationToken)
    {
        if (user.FamilyId is not { } familyId)
        {
            await bot.SendMessage(chatId, "Ты пока не в семье.",
                replyMarkup: BotKeyboards.MainMenu(hasFamily: false), cancellationToken: cancellationToken);
            return;
        }

        var result = await mediator.Send(new CreateFamilyInviteCommand(familyId, user.Id), cancellationToken);
        if (result.IsFailed)
        {
            LogFailure(result, "create invite", user);
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        var username = await botInfo.GetUsernameAsync(cancellationToken);
        var daysLeft = (int)Math.Round((result.Value.ExpiresAt - DateTime.UtcNow).TotalDays);

        await bot.SendMessage(chatId,
            $"Перешли эту ссылку тому, кого хочешь позвать в семью. Она одноразовая и действует {daysLeft} дн.\n\n" +
            $"https://t.me/{username}?start={result.Value.Code}",
            cancellationToken: cancellationToken);
    }

    private async Task JoinByInviteAsync(long chatId, UserDto user, string code, CancellationToken cancellationToken)
    {
        if (user.FamilyId is not null)
        {
            await bot.SendMessage(chatId,
                $"Ты уже в семье «{user.FamilyName}». Чтобы вступить в другую, сначала выйди из текущей: «Моя семья» → «Выйти».",
                replyMarkup: BotKeyboards.MainMenu(hasFamily: true), cancellationToken: cancellationToken);
            return;
        }

        var result = await mediator.Send(new AcceptFamilyInviteCommand(code, user.Id), cancellationToken);
        if (result.IsFailed)
        {
            var text = ErrorCodeOf(result) switch
            {
                ErrorCode.NotFound or ErrorCode.Validation => "Приглашение не найдено 🤔 Проверь ссылку или попроси новую.",
                ErrorCode.LogicConflict => "Это приглашение уже использовано или истекло. Попроси новую ссылку.",
                _ => GenericErrorText,
            };

            if (text == GenericErrorText)
                LogFailure(result, "accept invite", user);

            await bot.SendMessage(chatId, text,
                replyMarkup: BotKeyboards.MainMenu(hasFamily: false), cancellationToken: cancellationToken);
            return;
        }

        var family = await mediator.Send(new GetFamilyByIdQuery(result.Value), cancellationToken);
        var familyName = family.IsSuccess ? family.Value.Name : "семью";

        await bot.SendMessage(chatId,
            $"Добро пожаловать в «{familyName}»! 🎉\nТеперь вы видите рецепты друг друга.",
            replyMarkup: BotKeyboards.MainMenu(hasFamily: true), cancellationToken: cancellationToken);
    }

    private async Task LeaveFamilyAsync(long chatId, UserDto user, int? confirmationMessageId, CancellationToken cancellationToken)
    {
        if (user.FamilyId is not { } familyId)
        {
            await bot.SendMessage(chatId, "Ты уже не в семье.",
                replyMarkup: BotKeyboards.MainMenu(hasFamily: false), cancellationToken: cancellationToken);
            return;
        }

        var result = await mediator.Send(new LeaveFamilyCommand(familyId, user.Id), cancellationToken);
        if (result.IsFailed)
        {
            LogFailure(result, "leave family", user);
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        if (confirmationMessageId is { } messageId)
            await bot.EditMessageText(chatId, messageId, $"Готово, ты больше не в «{user.FamilyName}».", cancellationToken: cancellationToken);

        // Главное меню — reply-клавиатура, её можно обновить только новым сообщением.
        await bot.SendMessage(chatId, "Твои рецепты остались с тобой. Можешь создать новую семью или вступить в другую.",
            replyMarkup: BotKeyboards.MainMenu(hasFamily: false), cancellationToken: cancellationToken);
    }

    private async Task<UserDto?> RegisterAsync(TelegramUser from, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegisterUserCommand(from.Id, from.FirstName, from.LastName), cancellationToken);
        if (result.IsSuccess)
            return result.Value;

        logger.LogWarning("Failed to register Telegram user {TelegramId}: {Errors}",
            from.Id, string.Join("; ", result.Errors.Select(e => e.Message)));
        return null;
    }

    private void LogFailure(ResultBase result, string action, UserDto user) =>
        logger.LogWarning("Bot failed to {Action} for user {UserId}: {Errors}",
            action, user.Id, string.Join("; ", result.Errors.Select(e => e.Message)));

    private static ErrorCode? ErrorCodeOf(ResultBase result) =>
        result.Errors.OfType<AppError>().FirstOrDefault()?.Code;
}
