using Cooking.Application.Common.Errors;
using Cooking.Application.Families.Commands;
using Cooking.Application.Families.Queries;
using Cooking.Application.Users;
using Cooking.Application.Users.Commands;
using FluentResults;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUser = Telegram.Bot.Types.User;

namespace Cooking.Api.Bot;

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
    private const string StartCommand = "/start";
    private const string GenericErrorText = "Что-то пошло не так 😔 Попробуй ещё раз чуть позже.";

    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case { Message: { Text: { } text, From: { } from, Chat.Type: ChatType.Private } message }:
                await HandleMessageAsync(message.Chat.Id, from, text.Trim(), cancellationToken);
                break;

            case { CallbackQuery: { Data: { } data, From: { } from } callback }:
                await HandleCallbackAsync(callback, from, data, cancellationToken);
                break;
        }
    }

    private async Task HandleMessageAsync(long chatId, TelegramUser from, string text, CancellationToken cancellationToken)
    {
        // Регистрация идемпотентна, поэтому вызываем её на каждое сообщение: так бот работает, даже если
        // пользователь не нажимал /start (например, после очистки БД), а имя в профиле остаётся актуальным.
        var user = await RegisterAsync(from, cancellationToken);
        if (user is null)
        {
            await bot.SendMessage(chatId, GenericErrorText, cancellationToken: cancellationToken);
            return;
        }

        if (text.StartsWith(StartCommand, StringComparison.Ordinal))
        {
            // Deep link t.me/<bot>?start=<code> приходит как сообщение "/start <code>".
            var payload = text[StartCommand.Length..].Trim();

            if (payload.Length > 0)
                await JoinByInviteAsync(chatId, user, payload, cancellationToken);
            else
                await SendGreetingAsync(chatId, user, cancellationToken);

            return;
        }

        switch (text)
        {
            case BotButtons.CreateRecipe:
            case BotButtons.FindRecipe:
                await bot.SendMessage(chatId, "Скоро будет 🙂 Сейчас я учусь работать с рецептами.",
                    replyMarkup: BotKeyboards.MainMenu(user.FamilyId is not null), cancellationToken: cancellationToken);
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

            default:
                await bot.SendMessage(chatId, "Не понял 🤔 Выбери действие в меню ниже.",
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
        }
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
