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
}

public static class BotTexts
{
    /// <summary>
    /// Текст приглашения к поиску. Ответ пользователя на это сообщение (reply) — поисковый запрос:
    /// так бот не хранит состояние диалога, а обычный текст остаётся свободным под парсинг рецептов.
    /// </summary>
    public const string SearchPrompt = "🔍 Напиши часть названия — я поищу среди твоих и семейных рецептов.";
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

    public static InlineKeyboardMarkup ConfirmLeave() =>
        new(new[]
        {
            InlineKeyboardButton.WithCallbackData("Да, выйти", BotCallbacks.FamilyLeaveConfirm),
            InlineKeyboardButton.WithCallbackData("Отмена", BotCallbacks.FamilyLeaveCancel),
        });
}
