using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Cooking.Bot;

public static class BotButtons
{
    public const string CreateRecipe = "📝 Создать рецепт";
    public const string FindRecipe = "🔍 Найти рецепт";
    public const string MyFamily = "👨‍👩‍👧 Моя семья";
    public const string CreateFamily = "👨‍👩‍👧 Создать семью";
    public const string JoinFamily = "🔗 Вступить по приглашению";
}

public static class BotCallbacks
{
    public const string FamilyInvite = "family:invite";
    public const string FamilyLeave = "family:leave";
    public const string FamilyLeaveConfirm = "family:leave:yes";
    public const string FamilyLeaveCancel = "family:leave:no";

    /// <summary>Префикс кнопки рецепта: "recipe:{guid}" — 43 байта, в лимит callback_data (64) влезает.</summary>
    public const string RecipePrefix = "recipe:";

    public static string Recipe(Guid recipeId) => RecipePrefix + recipeId;

    // Кнопки карточки рецепта: "recipe:delete:yes:{guid}" — 54 байта, в лимит callback_data (64) влезает.
    // Более длинные префиксы проверяются раньше коротких (MatchId с "recipe:" на них и так не срабатывает — после него не Guid).
    public const string RecipeEditPrefix = "recipe:edit:";
    public const string RecipeDeletePrefix = "recipe:delete:";
    public const string RecipeDeleteConfirmPrefix = "recipe:delete:yes:";
    public const string RecipeKeep = "recipe:keep";

    public static string RecipeEdit(Guid recipeId) => RecipeEditPrefix + recipeId;
    public static string RecipeDelete(Guid recipeId) => RecipeDeletePrefix + recipeId;
    public static string RecipeDeleteConfirm(Guid recipeId) => RecipeDeleteConfirmPrefix + recipeId;

    // Кнопки превью черновика: "draft:estimate:{guid}" — 51 байт, в лимит callback_data (64) влезает.
    public const string DraftSavePrefix = "draft:save:";
    public const string DraftCancelPrefix = "draft:cancel:";
    public const string DraftEstimatePrefix = "draft:estimate:";
    public const string DraftEditPrefix = "draft:edit:";

    public static string DraftSave(Guid draftId) => DraftSavePrefix + draftId;
    public static string DraftCancel(Guid draftId) => DraftCancelPrefix + draftId;
    public static string DraftEstimate(Guid draftId) => DraftEstimatePrefix + draftId;
    public static string DraftEdit(Guid draftId) => DraftEditPrefix + draftId;

    // Сообщение «Что поправить?» несёт ForceReply, а не inline-кнопки, поэтому Id черновика (и превью, у которого
    // надо убрать кнопки) прячем в ссылку на невидимом символе: Telegram вернёт её в reply_to_message вместе с ответом.
    // Домен .invalid зарезервирован и никуда не ведёт — ссылку никто не открывает.
    private const string DraftEditLinkPrefix = "https://draft.invalid/";

    public static string DraftEditLink(Guid draftId, int previewMessageId) => $"{DraftEditLinkPrefix}{draftId}/{previewMessageId}";

    /// <summary>Черновик и превью из ссылки DraftEditLink; null — это не она.</summary>
    public static (Guid DraftId, int PreviewMessageId)? ParseDraftEditLink(string? url)
    {
        if (url is null || !url.StartsWith(DraftEditLinkPrefix, StringComparison.Ordinal))
            return null;

        var parts = url[DraftEditLinkPrefix.Length..].Split('/');

        return parts.Length == 2 && Guid.TryParse(parts[0], out var draftId) && int.TryParse(parts[1], out var messageId)
            ? (draftId, messageId)
            : null;
    }

    /// <summary>Ищет DraftEditLink среди ссылок сообщения «Что поправить?».</summary>
    public static (Guid DraftId, int PreviewMessageId)? FindDraftEditTarget(Message? message) =>
        FindLink(message, ParseDraftEditLink);

    // То же для «Что поменять в рецепте?» — там нужен только Id рецепта.
    private const string RecipeEditLinkPrefix = "https://recipe.invalid/";

    public static string RecipeEditLink(Guid recipeId) => RecipeEditLinkPrefix + recipeId;

    public static Guid? ParseRecipeEditLink(string? url) =>
        url is not null && url.StartsWith(RecipeEditLinkPrefix, StringComparison.Ordinal)
                        && Guid.TryParse(url.AsSpan(RecipeEditLinkPrefix.Length), out var id)
            ? id
            : null;

    public static Guid? FindRecipeEditTarget(Message? message) => FindLink(message, ParseRecipeEditLink);

    private static T? FindLink<T>(Message? message, Func<string?, T?> parse) where T : struct =>
        message?.Entities?
            .Where(e => e.Type == MessageEntityType.TextLink)
            .Select(e => parse(e.Url))
            .FirstOrDefault(target => target is not null);

    /// <summary>
    /// Id черновика по кнопкам превью. Ответ на сообщение приходит вместе с ним самим (и его кнопками) —
    /// так бот понимает, к какому черновику правка, ничего не храня.
    /// </summary>
    public static Guid? FindDraftId(InlineKeyboardMarkup? markup) =>
        markup?.InlineKeyboard
            .SelectMany(row => row)
            .Select(button => button.CallbackData is { } data ? MatchId(data, DraftSavePrefix) : null)
            .FirstOrDefault(id => id is not null);

    /// <summary>Если data — это prefix + Guid, возвращает Guid; иначе null.</summary>
    public static Guid? MatchId(string data, string prefix) =>
        data.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParse(data.AsSpan(prefix.Length), out var id)
            ? id
            : null;
}

/// <summary>Команды бота; список уходит в Telegram (меню «/» в клиенте) при старте.</summary>
public static class BotCommandNames
{
    public const string Start = "/start";
    public const string Search = "/search";

    public static readonly BotCommand[] All =
    [
        new() { Command = "start", Description = "Главное меню" },
        new() { Command = "search", Description = "Найти рецепт: /search курица" },
    ];

    /// <summary>
    /// Если text — это команда command (в т.ч. в форме "/search@BotName"), возвращает её аргументы
    /// (пустая строка — без аргументов); иначе null.
    /// </summary>
    public static string? MatchArguments(string text, string command)
    {
        if (!text.StartsWith(command, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = text[command.Length..];

        if (rest.StartsWith('@'))
        {
            var spaceIndex = rest.IndexOf(' ');
            rest = spaceIndex < 0 ? "" : rest[spaceIndex..];
        }
        else if (rest.Length > 0 && !char.IsWhiteSpace(rest[0]))
        {
            return null; // "/searching" — другая команда
        }

        return rest.Trim();
    }
}

public static class BotTexts
{
    /// <summary>
    /// Текст приглашения к поиску. Ответ пользователя на это сообщение (reply) — поисковый запрос:
    /// так бот не хранит состояние диалога, а обычный текст остаётся свободным под парсинг рецептов.
    /// </summary>
    public const string SearchPrompt = "🔍 Напиши часть названия — я поищу среди твоих и семейных рецептов.";

    /// <summary>Приглашение прислать рецепт; ответ на это сообщение — текст рецепта (как и просто длинный текст).</summary>
    public const string RecipePrompt = "📝 Пришли текст рецепта — я разберу его, покажу, что получилось, и сохраню после твоего «ок».";
}

public static class BotKeyboards
{
    /// <summary>Главное меню внизу чата. Набор кнопок зависит от того, состоит ли пользователь в семье.</summary>
    public static ReplyKeyboardMarkup MainMenu(bool hasFamily) =>
        new(hasFamily
            ? new[]
            {
                new KeyboardButton[] { BotButtons.CreateRecipe, BotButtons.FindRecipe },
                new KeyboardButton[] { BotButtons.MyFamily },
            }
            : new[]
            {
                new KeyboardButton[] { BotButtons.CreateRecipe, BotButtons.FindRecipe },
                new KeyboardButton[] { BotButtons.CreateFamily, BotButtons.JoinFamily },
            })
        {
            ResizeKeyboard = true,
        };

    public static InlineKeyboardMarkup FamilyActions() =>
        new(new[]
        {
            InlineKeyboardButton.WithCallbackData("➕ Пригласить", BotCallbacks.FamilyInvite),
            InlineKeyboardButton.WithCallbackData("🚪 Выйти", BotCallbacks.FamilyLeave),
        });

    /// <summary>По кнопке на рецепт, каждая в своей строке — длинные названия так читаются лучше.</summary>
    public static InlineKeyboardMarkup RecipeList(IEnumerable<(Guid Id, string Label)> recipes) =>
        new(recipes.Select(r => new[] { InlineKeyboardButton.WithCallbackData(r.Label, BotCallbacks.Recipe(r.Id)) }));

    /// <summary>Открывает поле ответа на сообщение с SearchPrompt.</summary>
    public static ForceReplyMarkup SearchReply() =>
        new() { InputFieldPlaceholder = "например, курица" };

    /// <summary>Кнопки под карточкой рецепта. Удалить может только автор, изменить — автор и семья.</summary>
    public static InlineKeyboardMarkup RecipeActions(Guid recipeId, bool canDelete) =>
        new(canDelete
            ? new[]
            {
                InlineKeyboardButton.WithCallbackData("✏️ Изменить", BotCallbacks.RecipeEdit(recipeId)),
                InlineKeyboardButton.WithCallbackData("🗑 Удалить", BotCallbacks.RecipeDelete(recipeId)),
            }
            : new[] { InlineKeyboardButton.WithCallbackData("✏️ Изменить", BotCallbacks.RecipeEdit(recipeId)) });

    public static InlineKeyboardMarkup ConfirmDeleteRecipe(Guid recipeId) =>
        new(new[]
        {
            InlineKeyboardButton.WithCallbackData("Да, удалить", BotCallbacks.RecipeDeleteConfirm(recipeId)),
            InlineKeyboardButton.WithCallbackData("Отмена", BotCallbacks.RecipeKeep),
        });

    /// <summary>Открывает поле ответа на сообщение «Что поменять в рецепте?».</summary>
    public static ForceReplyMarkup RecipeEditReply() =>
        new() { InputFieldPlaceholder = "например: порций 6, добавь чеснок" };

    /// <summary>Открывает поле ответа на сообщение «Что поправить?».</summary>
    public static ForceReplyMarkup CorrectionReply() =>
        new() { InputFieldPlaceholder = "например: лука не надо, порций 4" };

    /// <summary>Открывает поле ответа на сообщение с RecipePrompt.</summary>
    public static ForceReplyMarkup RecipeReply() =>
        new() { InputFieldPlaceholder = "текст рецепта" };

    /// <summary>Кнопки под превью черновика. «Оценить» — только если есть что оценивать.</summary>
    public static InlineKeyboardMarkup DraftActions(Guid draftId, bool canApplyEstimates)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", BotCallbacks.DraftSave(draftId)),
                InlineKeyboardButton.WithCallbackData("❌ Отмена", BotCallbacks.DraftCancel(draftId)),
            },
        };

        rows.Add([InlineKeyboardButton.WithCallbackData("✏️ Исправить", BotCallbacks.DraftEdit(draftId))]);

        if (canApplyEstimates)
            rows.Add([InlineKeyboardButton.WithCallbackData("🤖 Оценить порции и время", BotCallbacks.DraftEstimate(draftId))]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup ConfirmLeave() =>
        new(new[]
        {
            InlineKeyboardButton.WithCallbackData("Да, выйти", BotCallbacks.FamilyLeaveConfirm),
            InlineKeyboardButton.WithCallbackData("Отмена", BotCallbacks.FamilyLeaveCancel),
        });
}
