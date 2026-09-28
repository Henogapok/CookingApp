using System.Globalization;
using System.Net;
using System.Text;
using Cooking.Application.Recipes;

namespace Cooking.Bot;

/// <summary>Текст карточки рецепта для Telegram (ParseMode.Html).</summary>
public static class BotRecipeFormatter
{
    // Лимит Telegram — 4096 символов на сообщение; оставляем запас под «обрезано».
    private const int MaxMessageLength = 4000;

    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public static string FormatCard(RecipeDto recipe, Guid viewerUserId)
    {
        var header = new StringBuilder();
        header.AppendLine($"🍽 <b>{Html(recipe.Title)}</b>");

        if (!string.IsNullOrWhiteSpace(recipe.Description))
            header.AppendLine().AppendLine($"<i>{Html(recipe.Description)}</i>");

        header.AppendLine()
            .AppendLine($"⏱ {FormatMinutes(recipe.CookingTimeMinutes)} · 👥 {recipe.Servings} порц. · 📊 {Html(recipe.ComplexityName)}");

        if (recipe.Tags.Count > 0)
            header.AppendLine("🏷 " + string.Join(", ", recipe.Tags.Select(t => Html(t.Name))));

        if (recipe.CreatedByUserId != viewerUserId)
            header.AppendLine($"👤 Автор: {Html(recipe.CreatedByFirstName)}");

        var sections = new List<string> { header.ToString() };

        if (recipe.Ingredients.Count > 0)
        {
            var ingredients = recipe.Ingredients.Select(i =>
                $"• {Html(i.IngredientName)} — {i.Amount.ToString("0.##", Russian)} {Html(i.UnitAbbreviation)}");
            sections.Add("<b>Ингредиенты</b>\n" + string.Join("\n", ingredients) + "\n");
        }

        if (recipe.Steps.Count > 0)
        {
            sections.Add("<b>Приготовление</b>");
            sections.AddRange(recipe.Steps.Select(s =>
                $"{s.StepNumber}. {Html(s.Instruction)}" + (s.TimerSeconds is { } seconds ? $" ⏲ {FormatSeconds(seconds)}" : "")));
        }

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl))
            sections.Add($"\n🔗 <a href=\"{Html(recipe.SourceUrl)}\">Источник</a>");

        return JoinWithinLimit(sections);
    }

    /// <summary>Подпись кнопки в списке рецептов: чужие рецепты помечаем именем автора.</summary>
    public static string FormatListButton(RecipeSummaryDto recipe, Guid viewerUserId) =>
        recipe.CreatedByUserId == viewerUserId
            ? recipe.Title
            : $"{recipe.Title} · {recipe.CreatedByFirstName}";

    // Секции добавляются целиком, пока влезают в лимит, — так не рвём HTML-теги посередине.
    private static string JoinWithinLimit(List<string> sections)
    {
        const string truncated = "\n…рецепт слишком длинный для одного сообщения";
        var text = new StringBuilder();

        foreach (var section in sections)
        {
            if (text.Length + section.Length + 1 > MaxMessageLength - truncated.Length)
                return text.Append(truncated).ToString();

            text.AppendLine(section);
        }

        return text.ToString().TrimEnd();
    }

    private static string FormatMinutes(int minutes) =>
        minutes >= 60
            ? $"{minutes / 60} ч" + (minutes % 60 > 0 ? $" {minutes % 60} мин" : "")
            : $"{minutes} мин";

    private static string FormatSeconds(int seconds) =>
        seconds >= 60
            ? FormatMinutes(seconds / 60) + (seconds % 60 > 0 ? $" {seconds % 60} сек" : "")
            : $"{seconds} сек";

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
