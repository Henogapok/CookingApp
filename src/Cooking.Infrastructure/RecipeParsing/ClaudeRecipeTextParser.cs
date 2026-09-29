using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using Cooking.Application.Common.Errors;
using ErrorCode = Cooking.Application.Common.Errors.ErrorCode;
using Cooking.Application.RecipeDrafts.Parsing;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cooking.Infrastructure.RecipeParsing;

/// <summary>
/// Разбор текста рецепта через Claude со structured outputs: ответ гарантированно соответствует JSON-схеме,
/// а в Id его переводит RecipeDraftMapper (Application). Singleton: клиент (и его HTTP-соединения) общий на все запросы.
/// </summary>
public class ClaudeRecipeTextParser : IRecipeTextParser
{
    private const string FallbackBeta = "server-side-fallback-2026-07-01";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Промпт и схема не зависят от запроса — меняется только сообщение пользователя.
    private const string SystemPrompt =
        """
        Ты разбираешь кулинарные рецепты из свободного текста (пост, заметка, расшифровка видео) в структуру.

        Правила:
        - Если в тексте нет рецепта, верни isRecipe = false, остальные поля заполни пустыми значениями.
        - Пиши по-русски. title — короткое название блюда, как в кулинарной книге. description — одно-два предложения о блюде или null.
        - Ничего не выдумывай. servings и cookingTimeMinutes — только если они явно есть в тексте, иначе null.
          Свою оценку давай всегда отдельно: servingsEstimate и cookingTimeMinutesEstimate (общее время: подготовка + готовка).
        - Ингредиенты: если продукт есть в списке «Каталог ингредиентов», пиши название в точности как в каталоге.
          Иначе — короткое название в именительном падеже, без количества и способа нарезки («Лук репчатый», а не «2 луковицы, мелко нарезать»).
        - amount и unit: переведи количество в одну из единиц g, ml, pcs, tbsp, tsp, cup, pinch.
          Если количества нет («по вкусу», «для жарки», «немного») — amount = null и unit = null. Не заменяй «по вкусу» щепоткой.
        - Для каждого ингредиента укажи category, baseUnit (g — для твёрдых продуктов, ml — для жидкостей)
          и типичные справочные КБЖУ на 100 г или 100 мл сырого продукта.
        - steps — по порядку, в повелительном наклонении, одно действие или этап на шаг.
          timerSeconds — только если в шаге названо конкретное время («варить 10 минут» → 600; для диапазона — нижняя граница), иначе null.
        - tags — только из списка «Доступные теги», и только явно подходящие. Нет списка — пустой массив.
        - complexity: easy, medium или hard.
        """;

    private static readonly Dictionary<string, JsonElement> Schema = BuildSchema();

    private readonly AnthropicOptions _settings;
    private readonly ILogger<ClaudeRecipeTextParser> _logger;
    private readonly AnthropicClient? _client;

    public ClaudeRecipeTextParser(IOptions<AnthropicOptions> options, ILogger<ClaudeRecipeTextParser> logger)
    {
        _settings = options.Value;
        _logger = logger;

        // Без ключа Api всё равно стартует — разбор просто отвечает «недоступно».
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            _client = new AnthropicClient
            {
                ApiKey = _settings.ApiKey,
                // По умолчанию SDK ждёт до 10 минут — зависший запрос надолго занял бы слот очереди.
                Timeout = TimeSpan.FromMinutes(3),
            };
    }

    public async Task<Result<ParsedRecipe>> ParseAsync(RecipeParsingRequest request, CancellationToken cancellationToken)
    {
        if (_client is null)
            return Result.Fail(new AppError("Recipe parsing is not configured: Anthropic:ApiKey is empty.", ErrorCode.Unavailable));

        var settings = _settings;

        try
        {
            var parameters = new MessageCreateParams
            {
                Model = settings.Model,
                MaxTokens = settings.MaxTokens,
                System = SystemPrompt,
                Messages = [new() { Role = Role.User, Content = BuildUserMessage(request) }],
                OutputConfig = new BetaOutputConfig
                {
                    Format = new BetaJsonOutputFormat { Schema = Schema },
                    Effort = string.IsNullOrWhiteSpace(settings.Effort) ? null : ParseEffort(settings.Effort),
                },
            };

            if (settings.RefusalFallback)
                parameters = parameters with { Betas = [FallbackBeta], Fallbacks = new Default() };

            var response = await _client.Beta.Messages.Create(parameters, cancellationToken);

            _logger.LogInformation(
                "Recipe parsed by {Model}: stop {StopReason}, {InputTokens} input / {OutputTokens} output tokens",
                response.Model, response.StopReason, response.Usage.InputTokens, response.Usage.OutputTokens);

            if (response.StopReason != "end_turn")
                return ExternalError($"LLM stopped with '{response.StopReason}' instead of finishing the answer.");

            var json = string.Concat(response.Content
                .Select(block => block.TryPickText(out var text) ? text.Text : null)
                .OfType<string>());

            return JsonSerializer.Deserialize<ParsedRecipe>(json, JsonOptions) is { } parsed
                ? Result.Ok(parsed)
                : ExternalError("LLM returned an empty answer.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Recipe parsing request to Claude failed");
            return ExternalError($"Recipe parsing request failed: {ex.Message}");
        }
    }

    private static string BuildUserMessage(RecipeParsingRequest request)
    {
        var message = new StringBuilder();

        message.AppendLine("Каталог ингредиентов:");
        foreach (var name in request.CatalogIngredientNames)
            message.Append("- ").AppendLine(name);

        message.AppendLine().AppendLine("Доступные теги:");
        foreach (var name in request.TagNames)
            message.Append("- ").AppendLine(name);

        message.AppendLine()
            .AppendLine("Текст рецепта:")
            .AppendLine("<recipe>")
            .AppendLine(request.Text)
            .AppendLine("</recipe>");

        return message.ToString();
    }

    private static Result<ParsedRecipe> ExternalError(string message) =>
        Result.Fail(new AppError(message, ErrorCode.ExternalService));

    private static Effort ParseEffort(string value) => value.Trim().ToLowerInvariant() switch
    {
        "low" => Effort.Low,
        "medium" => Effort.Medium,
        "high" => Effort.High,
        "xhigh" => Effort.Xhigh,
        "max" => Effort.Max,
        _ => throw new InvalidOperationException($"Unknown Anthropic:Effort '{value}'."),
    };

    /// <summary>
    /// JSON-схема ответа. Structured outputs требуют additionalProperties: false и перечисления всех полей в required;
    /// «необязательное» поле — это anyOf с null. Имена полей — camelCase-версии свойств ParsedRecipe.
    /// </summary>
    private static Dictionary<string, JsonElement> BuildSchema()
    {
        static object Nullable(object schema) => new { anyOf = new[] { schema, new { type = "null" } } };
        static object Enum(IEnumerable<string> values) => new { type = "string", @enum = values.ToArray() };
        static object Obj(Dictionary<string, object> properties) => new
        {
            type = "object",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false,
        };

        var ingredient = Obj(new()
        {
            ["name"] = new { type = "string" },
            ["amount"] = Nullable(new { type = "number" }),
            ["unit"] = Nullable(Enum(RecipeParsingCodes.Units.Keys)),
            ["category"] = Enum(RecipeParsingCodes.Categories.Keys),
            ["baseUnit"] = Enum(RecipeParsingCodes.BaseUnits.Keys),
            ["caloriesPer100G"] = new { type = "number" },
            ["proteinPer100G"] = new { type = "number" },
            ["fatPer100G"] = new { type = "number" },
            ["carbsPer100G"] = new { type = "number" },
        });

        var step = Obj(new()
        {
            ["instruction"] = new { type = "string" },
            ["timerSeconds"] = Nullable(new { type = "integer" }),
        });

        var recipe = Obj(new()
        {
            ["isRecipe"] = new { type = "boolean" },
            ["title"] = new { type = "string" },
            ["description"] = Nullable(new { type = "string" }),
            ["complexity"] = Enum(RecipeParsingCodes.Complexities.Keys),
            ["servings"] = Nullable(new { type = "integer" }),
            ["servingsEstimate"] = Nullable(new { type = "integer" }),
            ["cookingTimeMinutes"] = Nullable(new { type = "integer" }),
            ["cookingTimeMinutesEstimate"] = Nullable(new { type = "integer" }),
            ["ingredients"] = new { type = "array", items = ingredient },
            ["steps"] = new { type = "array", items = step },
            ["tags"] = new { type = "array", items = new { type = "string" } },
        });

        return JsonSerializer.SerializeToElement(recipe)
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }
}
