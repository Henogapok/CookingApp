using System.Text.Encodings.Web;
using System.Text.Json;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Application.Recipes;
using Cooking.Domain.ReferenceData;

namespace Cooking.Application.RecipeDrafts;

/// <summary>
/// Чистые преобразования без БД и LLM: ответ LLM → черновик → поля рецепта.
/// Ответу LLM не доверяем: неизвестные коды, пустые строки и мусорные числа отбрасываются здесь.
/// </summary>
public static class RecipeDraftMapper
{
    // Лимиты совпадают с RecipeFieldsValidator и конфигурацией каталога.
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 2000;
    private const int MaxInstructionLength = 4000;
    private const int MaxIngredientNameLength = 200;

    /// <summary>
    /// Ключ для сопоставления названий ингредиентов/тегов: без регистра, лишних пробелов и различия «ё»/«е».
    /// </summary>
    public static string NameKey(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant()
            .Replace('ё', 'е');

    /// <summary>
    /// null — это не рецепт: нет ни названия, ни ингредиентов/шагов.
    /// </summary>
    /// <param name="catalogIdsByKey">Каталог ингредиентов: NameKey(название) → Id.</param>
    /// <param name="tagIdsByKey">Теги: NameKey(название) → Id.</param>
    public static RecipeDraftContent? ToDraftContent(
        ParsedRecipe parsed,
        IReadOnlyDictionary<string, Guid> catalogIdsByKey,
        IReadOnlyDictionary<string, Guid> tagIdsByKey)
    {
        var title = Truncate(parsed.Title?.Trim(), MaxTitleLength);

        var ingredients = (parsed.Ingredients ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => ToDraftIngredient(i, catalogIdsByKey))
            .ToList();

        var steps = (parsed.Steps ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Instruction))
            .Select(s => new RecipeStepFields(
                Truncate(s.Instruction.Trim(), MaxInstructionLength)!,
                s.TimerSeconds > 0 ? s.TimerSeconds : null))
            .ToList();

        if (string.IsNullOrEmpty(title) || (ingredients.Count == 0 && steps.Count == 0))
            return null;

        var tagIds = (parsed.Tags ?? [])
            .Select(name => tagIdsByKey.TryGetValue(NameKey(name), out var id) ? id : (Guid?)null)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        return new RecipeDraftContent(
            title,
            Truncate(NullIfBlank(parsed.Description), MaxDescriptionLength),
            RecipeParsingCodes.Complexities.GetValueOrDefault(parsed.Complexity ?? "", ReferenceIds.Complexities.Medium),
            Positive(parsed.Servings),
            Positive(parsed.ServingsEstimate),
            Positive(parsed.CookingTimeMinutes),
            Positive(parsed.CookingTimeMinutesEstimate),
            UseEstimates: false,
            ingredients,
            steps,
            tagIds);
    }

    /// <summary>
    /// Поля рецепта для сохранения. Новые ингредиенты к этому моменту уже должны быть в каталоге:
    /// их Id ищутся по названию в catalogIdsByKey.
    /// </summary>
    public static RecipeFields ToRecipeFields(RecipeDraftContent content, IReadOnlyDictionary<string, Guid> catalogIdsByKey) =>
        new(
            content.Title,
            content.Description,
            content.SourceUrl,
            content.SourceTypeId ?? ReferenceIds.SourceTypes.Manual,
            content.ComplexityId,
            content.EffectiveServings,
            content.EffectiveCookingTimeMinutes,
            content.Ingredients
                .Select(i => new RecipeIngredientFields(
                    i.IngredientCatalogId ?? catalogIdsByKey[NameKey(i.Name)],
                    i.Amount,
                    i.UnitId))
                .ToList(),
            content.Steps,
            content.TagIds);

    /// <summary>
    /// Черновик из сохранённого рецепта — отправная точка для его изменения. Порции/время считаем «указанными»:
    /// рецепт их уже прошёл, оценивать заново нечего.
    /// </summary>
    public static RecipeDraftContent FromRecipe(RecipeDto recipe) =>
        new(
            recipe.Title,
            recipe.Description,
            recipe.ComplexityId,
            recipe.Servings,
            ServingsEstimate: null,
            recipe.CookingTimeMinutes,
            CookingTimeMinutesEstimate: null,
            UseEstimates: false,
            recipe.Ingredients
                .Select(i => new RecipeDraftIngredient(i.IngredientName, i.IngredientCatalogId, null, i.Amount, i.UnitId))
                .ToList(),
            recipe.Steps.Select(s => new RecipeStepFields(s.Instruction, s.TimerSeconds)).ToList(),
            recipe.Tags.Select(t => t.Id).ToList(),
            recipe.Id,
            recipe.SourceUrl,
            recipe.SourceTypeId);

    /// <summary>
    /// Текущая версия черновика для LLM при правке — в тех же кодах, что и ответ (unit, complexity, названия тегов),
    /// чтобы модель вернула её же с изменениями.
    /// </summary>
    public static string ToCorrectionJson(RecipeDraftContent content, IReadOnlyDictionary<Guid, string> tagNamesById)
    {
        var unitCodes = RecipeParsingCodes.Units.ToDictionary(x => x.Value, x => x.Key);
        var complexityCodes = RecipeParsingCodes.Complexities.ToDictionary(x => x.Value, x => x.Key);

        var recipe = new
        {
            title = content.Title,
            description = content.Description,
            complexity = complexityCodes.GetValueOrDefault(content.ComplexityId, "medium"),
            servings = content.Servings,
            servingsEstimate = content.ServingsEstimate,
            cookingTimeMinutes = content.CookingTimeMinutes,
            cookingTimeMinutesEstimate = content.CookingTimeMinutesEstimate,
            ingredients = content.Ingredients.Select(i => new
            {
                name = i.Name,
                amount = i.Amount,
                unit = i.UnitId is { } unitId ? unitCodes.GetValueOrDefault(unitId) : null,
            }),
            steps = content.Steps.Select(s => new { instruction = s.Instruction, timerSeconds = s.TimerSeconds }),
            tags = content.TagIds.Select(id => tagNamesById.GetValueOrDefault(id)).OfType<string>(),
        };

        // Кириллица без \uXXXX-экранирования — короче и понятнее модели.
        return JsonSerializer.Serialize(recipe, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    /// <summary>
    /// Названия блюд, из которых пользователь выберет нужные: без пустых и повторов, обрезанные до лимитов.
    /// </summary>
    public static List<string> ToDishChoices(IEnumerable<string>? dishes) =>
        (dishes ?? [])
            .Select(d => Truncate(NullIfBlank(d), RecipeDraftLimits.MaxDishTitleLength))
            .OfType<string>()
            .DistinctBy(NameKey)
            .Take(RecipeDraftLimits.MaxDishChoices)
            .ToList();

    private static RecipeDraftIngredient ToDraftIngredient(ParsedIngredient parsed, IReadOnlyDictionary<string, Guid> catalogIdsByKey)
    {
        var name = Truncate(parsed.Name.Trim(), MaxIngredientNameLength)!;

        // Количество без понятной единицы (и наоборот) не имеет смысла — считаем «по вкусу».
        Guid? unitId = parsed.Unit is not null && RecipeParsingCodes.Units.TryGetValue(parsed.Unit, out var knownUnitId)
            ? knownUnitId
            : null;
        var amount = parsed.Amount > 0 ? parsed.Amount : null;
        if (unitId is null || amount is null)
            (unitId, amount) = (null, null);

        var pieceWeight = parsed.PieceWeight > 0 ? Math.Round(parsed.PieceWeight.Value, 2) : (decimal?)null;

        if (catalogIdsByKey.TryGetValue(NameKey(name), out var catalogId))
            return new RecipeDraftIngredient(name, catalogId, null, amount, unitId, pieceWeight);

        var newIngredient = new NewIngredientDraft(
            RecipeParsingCodes.Categories.GetValueOrDefault(parsed.Category ?? "", ReferenceIds.IngredientCategories.Spices),
            RecipeParsingCodes.BaseUnits.GetValueOrDefault(parsed.BaseUnit ?? "", ReferenceIds.MeasurementUnits.Gram),
            NonNegative(parsed.CaloriesPer100G),
            NonNegative(parsed.ProteinPer100G),
            NonNegative(parsed.FatPer100G),
            NonNegative(parsed.CarbsPer100G));

        return new RecipeDraftIngredient(name, null, newIngredient, amount, unitId, pieceWeight);
    }

    private static int? Positive(int? value) => value > 0 ? value : null;

    private static decimal NonNegative(decimal value) => Math.Max(0, Math.Round(value, 2));

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;
}
