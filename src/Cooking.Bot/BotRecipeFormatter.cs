using System.Globalization;
using System.Net;
using System.Text;
using Cooking.Application.Nutrition;
using Cooking.Application.RecipeDrafts;
using Cooking.Domain.ReferenceData;
using Cooking.Application.Recipes;

namespace Cooking.Bot;

/// <summary>
/// Текст карточки рецепта и превью черновика для Telegram (ParseMode.Html).
/// Ингредиенты и шаги — в сворачиваемых цитатах (&lt;blockquote expandable&gt;): длинный рецепт не занимает весь экран,
/// а заголовок и итоговое КБЖУ видны сразу.
/// </summary>
public static class BotRecipeFormatter
{
    // Лимит Telegram — 4096 символов на сообщение; оставляем запас под «обрезано».
    private const int MaxMessageLength = 4000;

    private const string LlmMark = "🤖";
    private const string NewMark = "🆕";

    public const string DraftCorrectionHint = "✏️ Что-то не так? Нажми «Исправить» или просто ответь на это сообщение.";

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

        var sections = new List<Section> { new TextSection(header.ToString()) };

        if (recipe.Ingredients.Count > 0)
        {
            sections.Add(FormatIngredients(
                recipe.Ingredients.Select(i => new IngredientLine(
                    i.IngredientName, i.Amount, i.UnitId, i.UnitAbbreviation, IsNew: false,
                    i.BaseAmount, i.BaseUnitAbbreviation, i.Nutrition))));
            sections.Add(new TextSection(FormatTotals(
                recipe.Nutrition, recipe.Servings,
                recipe.Ingredients.Count(i => i.IsNutritionEstimatedByLlm), recipe.Ingredients.Count)));
        }

        if (recipe.Steps.Count > 0)
            sections.Add(FormatSteps(recipe.Steps));

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl))
            sections.Add(SourceLink(recipe.SourceUrl));

        return JoinWithinLimit(sections);
    }

    /// <summary>Превью разобранного рецепта: что именно сохранится, если пользователь подтвердит.</summary>
    public static string FormatDraft(RecipeDraftDto draft)
    {
        var header = new StringBuilder();
        header.AppendLine(draft.RecipeId is null
            ? "📝 Вот что я разобрал — проверь, пожалуйста:"
            : "✏️ Так будет выглядеть рецепт после изменений — проверь:").AppendLine();
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

        var sections = new List<Section> { new TextSection(header.ToString()) };

        if (draft.Ingredients.Count > 0)
        {
            sections.Add(FormatIngredients(
                draft.Ingredients.Select(i => new IngredientLine(
                    i.Name, i.Amount, i.UnitId, i.UnitAbbreviation, i.IsNew,
                    i.BaseAmount, i.BaseUnitAbbreviation, i.Nutrition))));
            sections.Add(new TextSection(FormatTotals(
                draft.Nutrition, draft.Servings,
                draft.Ingredients.Count(i => i.IsNutritionEstimatedByLlm), draft.Ingredients.Count)));
        }

        if (draft.Steps.Count > 0)
            sections.Add(FormatSteps(draft.Steps));

        if (!string.IsNullOrWhiteSpace(draft.SourceUrl))
            sections.Add(SourceLink(draft.SourceUrl));

        sections.Add(new TextSection(DraftCorrectionHint));

        return JoinWithinLimit(sections);
    }

    /// <summary>Подпись кнопки в списке рецептов: чужие рецепты помечаем именем автора.</summary>
    public static string FormatListButton(RecipeSummaryDto recipe, Guid viewerUserId) =>
        recipe.CreatedByUserId == viewerUserId
            ? recipe.Title
            : $"{recipe.Title} · {recipe.CreatedByFirstName}";

    private sealed record IngredientLine(
        string Name,
        decimal? Amount,
        Guid? UnitId,
        string? UnitAbbreviation,
        bool IsNew,
        decimal? BaseAmount,
        string? BaseUnitAbbreviation,
        NutritionFacts? Nutrition);

    private abstract record Section;

    private sealed record TextSection(string Text) : Section;

    /// <summary>Заголовок + строки в сворачиваемой цитате + необязательная подпись под ней.</summary>
    private sealed record CollapsibleSection(string Title, List<string> Lines, string? Footer) : Section;

    /// <summary>«• Свёкла 🤖 — 1 шт (≈250 г) · 108/4/0/24» — к/б/ж/у на указанное количество.</summary>
    private static CollapsibleSection FormatIngredients(IEnumerable<IngredientLine> ingredients)
    {
        var list = ingredients.ToList();

        var lines = list.Select(i =>
        {
            var line = $"• {Html(i.Name)}{(i.IsNew ? " " + NewMark : "")} — ";

            if (i.Amount is not { } amount)
                return line + "по вкусу";

            line += $"{Number(amount)} {Html(i.UnitAbbreviation ?? "")}".TrimEnd();

            // Штуки пересчитаны по весу из каталога — показываем, сколько это граммов, чтобы было видно ошибку оценки.
            if (i.UnitId == ReferenceIds.MeasurementUnits.Piece && i.BaseAmount is { } baseAmount)
                line += $" (≈{Number(Math.Round(baseAmount))} {Html(i.BaseUnitAbbreviation ?? "")})";

            // Соль, вода, уксус — «0/0/0/0» ничего не сообщает, только шумит.
            if (i.Nutrition is { } n && !IsZero(n))
                line += $" · {Round(n.Calories)}/{Round(n.Protein)}/{Round(n.Fat)}/{Round(n.Carbs)}";

            return line;
        }).ToList();

        return new CollapsibleSection(
            $"<b>Ингредиенты ({list.Count})</b> · <i>ккал/б/ж/у</i>",
            lines,
            list.Any(i => i.IsNew) ? $"<i>{NewMark} — новый ингредиент, добавлю в каталог при сохранении</i>" : null);
    }

    private static bool IsZero(NutritionFacts n) =>
        Round(n.Calories) == "0" && Round(n.Protein) == "0" && Round(n.Fat) == "0" && Round(n.Carbs) == "0";

    /// <summary>
    /// Итог по сырым продуктам: всё блюдо, на порцию (если порции известны), чьё КБЖУ — оценка ИИ, что не учтено, стоимость.
    /// </summary>
    private static string FormatTotals(RecipeNutrition nutrition, int? servings, int estimatedByLlm, int ingredientsCount)
    {
        var text = new StringBuilder();

        if (nutrition.CountedIngredients > 0)
        {
            text.AppendLine($"🔥 Всё блюдо: {FormatFacts(nutrition.Total)}");
            if (nutrition.PerServing is { } perServing)
                text.AppendLine($"🍽 На порцию (из {servings}): {FormatFacts(perServing)}");
        }

        // Одной строкой, а не значком у каждого ингредиента: почти весь каталог пока заполнен ИИ.
        if (estimatedByLlm > 0)
            text.AppendLine($"{LlmMark} КБЖУ оценил ИИ: {estimatedByLlm} из {ingredientsCount} ингредиентов");

        if (nutrition.NotCounted.Count > 0)
            text.AppendLine($"⚠️ Не учтено в КБЖУ: {string.Join(", ", nutrition.NotCounted.Select(Html))}");

        // Цены в каталоге пока заполнены не у всех — показываем стоимость, только если есть хоть одна цена.
        if (nutrition.Cost > 0)
        {
            text.Append($"💰 ~{Round(nutrition.Cost)} ₸");
            if (nutrition.IngredientsWithoutPrice > 0)
                text.Append($" (без цены: {nutrition.IngredientsWithoutPrice})");
            text.AppendLine();
        }

        return text.ToString();
    }

    private static string FormatFacts(NutritionFacts n) =>
        $"{Round(n.Calories)} ккал · Б {Round(n.Protein)} · Ж {Round(n.Fat)} · У {Round(n.Carbs)}";

    private static string Round(decimal value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString("N0", Russian);

    private static string Number(decimal value) => value.ToString("0.##", Russian);

    private static CollapsibleSection FormatSteps(List<RecipeStepDto> steps) =>
        new(
            $"<b>Приготовление ({steps.Count} шаг.)</b>",
            steps
                .Select(s => $"{s.StepNumber}. {Html(s.Instruction)}" + (s.TimerSeconds is { } seconds ? $" ⏲ {FormatSeconds(seconds)}" : ""))
                .ToList(),
            null);

    private static TextSection SourceLink(string url) => new($"🔗 <a href=\"{Html(url)}\">Источник</a>");

    private static string EstimateNote(bool isEstimate) => isEstimate ? $" ({LlmMark} оценка ИИ)" : "";

    /// <summary>
    /// Собирает сообщение в пределах лимита Telegram. Текстовые секции добавляются целиком, у сворачиваемых —
    /// строки по одной, и цитата всегда закрывается: HTML-теги посередине не рвутся.
    /// </summary>
    private static string JoinWithinLimit(List<Section> sections)
    {
        const string truncated = "\n…рецепт слишком длинный для одного сообщения";
        const string openQuote = "<blockquote expandable>";
        const string closeQuote = "</blockquote>";
        var budget = MaxMessageLength - truncated.Length;
        var text = new StringBuilder();

        foreach (var section in sections)
        {
            switch (section)
            {
                case TextSection { Text: var sectionText }:
                    if (text.Length + sectionText.Length + 1 > budget)
                        return Finish(text.Append(truncated));

                    text.AppendLine(sectionText);
                    break;

                case CollapsibleSection collapsible:
                    if (text.Length + collapsible.Title.Length + openQuote.Length + closeQuote.Length + 2 > budget)
                        return Finish(text.Append(truncated));

                    text.AppendLine(collapsible.Title).Append(openQuote);

                    for (var i = 0; i < collapsible.Lines.Count; i++)
                    {
                        var line = (i > 0 ? "\n" : "") + collapsible.Lines[i];
                        if (text.Length + line.Length + closeQuote.Length > budget)
                            return Finish(text.Append(closeQuote).Append(truncated));

                        text.Append(line);
                    }

                    text.AppendLine(closeQuote);

                    if (collapsible.Footer is { } footer)
                    {
                        if (text.Length + footer.Length + 1 > budget)
                            return Finish(text.Append(truncated));

                        text.AppendLine(footer);
                    }

                    text.AppendLine();
                    break;
            }
        }

        return Finish(text);

        // AppendLine пишет Environment.NewLine (\r\n на Windows) — приводим к \n, чтобы текст не зависел от ОС.
        static string Finish(StringBuilder builder) => builder.ToString().Replace("\r\n", "\n").TrimEnd();
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
