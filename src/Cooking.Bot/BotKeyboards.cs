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

    public static InlineKeyboardMarkup ConfirmLeave() =>
        new(new[]
        {
            InlineKeyboardButton.WithCallbackData("Да, выйти", BotCallbacks.FamilyLeaveConfirm),
            InlineKeyboardButton.WithCallbackData("Отмена", BotCallbacks.FamilyLeaveCancel),
        });
}
