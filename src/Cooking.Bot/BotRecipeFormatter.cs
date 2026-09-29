using System.Globalization;
using System.Net;
using System.Text;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.Recipes;

namespace Cooking.Bot;

/// <summary>Текст карточки рецепта и превью черновика для Telegram (ParseMode.Html).</summary>
public static class BotRecipeFormatter
{
    // Лимит Telegram — 4096 символов на сообщение; оставляем запас под «обрезано».
    private const int MaxMessageLength = 4000;

    private const string LlmMark = "🤖";

    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public static string FormatCard(RecipeDto recipe, Guid viewerUserId)
    {
        var header = new StringBuilder();
        header.AppendLine($"🍽 <b>{Html(recipe.Title)}</b>");

        if (!string.IsNullOrWhiteSpace(recipe.Description))
            header.AppendLine().AppendLine($"<i>{Html(recipe.Description)}</i>");

        // Не указанные в рецепте порции/время просто не показываем.
        var meta = new List<string>();
        if (recipe.CookingTimeMinutes is { } minutes)
            meta.Add($"⏱ {FormatMinutes(minutes)}");
        if (recipe.Servings is { } servings)
            meta.Add($"👥 {servings} порц.");
        meta.Add($"📊 {Html(recipe.ComplexityName)}");

        header.AppendLine().AppendLine(string.Join(" · ", meta));

        if (recipe.Tags.Count > 0)
            header.AppendLine("🏷 " + string.Join(", ", recipe.Tags.Select(t => Html(t.Name))));

        if (recipe.CreatedByUserId != viewerUserId)
            header.AppendLine($"👤 Автор: {Html(recipe.CreatedByFirstName)}");

        var sections = new List<string> { header.ToString() };

        if (recipe.Ingredients.Count > 0)
        {
            sections.Add(FormatIngredients(
                recipe.Ingredients.Select(i => (i.IngredientName, i.Amount, i.UnitAbbreviation, i.IsNutritionEstimatedByLlm)),
                $"{LlmMark} — КБЖУ оценил ИИ"));
        }

        sections.AddRange(FormatSteps(recipe.Steps));

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl))
            sections.Add($"\n🔗 <a href=\"{Html(recipe.SourceUrl)}\">Источник</a>");

        return JoinWithinLimit(sections);
    }

    /// <summary>Превью разобранного рецепта: что именно сохранится, если пользователь подтвердит.</summary>
    public static string FormatDraft(RecipeDraftDto draft)
    {
        var header = new StringBuilder();
        header.AppendLine("📝 Вот что я разобрал — проверь, пожалуйста:").AppendLine();
        header.AppendLine($"🍽 <b>{Html(draft.Title)}</b>");

        if (!string.IsNullOrWhiteSpace(draft.Description))
            header.AppendLine().AppendLine($"<i>{Html(draft.Description)}</i>");

        header.AppendLine()
            .AppendLine(draft.CookingTimeMinutes is { } minutes
                ? $"⏱ {FormatMinutes(minutes)}{EstimateNote(draft.CookingTimeIsEstimate)}"
                : "⏱ время не указано")
            .AppendLine(draft.Servings is { } servings
                ? $"👥 {servings} порц.{EstimateNote(draft.ServingsIsEstimate)}"
                : "👥 порции не указаны")
            .AppendLine($"📊 {Html(draft.ComplexityName)}");

        if (draft.TagNames.Count > 0)
            header.AppendLine("🏷 " + string.Join(", ", draft.TagNames.Select(Html)));

        var sections = new List<string> { header.ToString() };

        if (draft.Ingredients.Count > 0)
        {
            sections.Add(FormatIngredients(
                draft.Ingredients.Select(i => (i.Name, i.Amount, i.UnitAbbreviation, i.IsNew)),
                $"{LlmMark} — новый ингредиент: добавлю в каталог, КБЖУ оценил ИИ"));
        }

        sections.AddRange(FormatSteps(draft.Steps));

        return JoinWithinLimit(sections);
    }

    /// <summary>Подпись кнопки в списке рецептов: чужие рецепты помечаем именем автора.</summary>
    public static string FormatListButton(RecipeSummaryDto recipe, Guid viewerUserId) =>
        recipe.CreatedByUserId == viewerUserId
            ? recipe.Title
            : $"{recipe.Title} · {recipe.CreatedByFirstName}";

    private static string FormatIngredients(
        IEnumerable<(string Name, decimal? Amount, string? Unit, bool MarkedByLlm)> ingredients, string llmLegend)
    {
        var list = ingredients.ToList();

        var lines = list.Select(i =>
            $"• {Html(i.Name)}{(i.MarkedByLlm ? " " + LlmMark : "")} — " +
            (i.Amount is { } amount ? $"{amount.ToString("0.##", Russian)} {Html(i.Unit ?? "")}".TrimEnd() : "по вкусу"));

        var text = "<b>Ингредиенты</b>\n" + string.Join("\n", lines) + "\n";

        return list.Any(i => i.MarkedByLlm) ? text + $"<i>{llmLegend}</i>\n" : text;
    }

    private static IEnumerable<string> FormatSteps(List<RecipeStepDto> steps)
    {
        if (steps.Count == 0)
            yield break;

        yield return "<b>Приготовление</b>";

        foreach (var s in steps)
            yield return $"{s.StepNumber}. {Html(s.Instruction)}" + (s.TimerSeconds is { } seconds ? $" ⏲ {FormatSeconds(seconds)}" : "");
    }

    private static string EstimateNote(bool isEstimate) => isEstimate ? $" ({LlmMark} оценка ИИ)" : "";

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
