namespace Cooking.Infrastructure.RecipeParsing;

/// <summary>
/// Настройки Claude API. Ключ — только в User Secrets / переменных окружения (Anthropic__ApiKey), не в git.
/// Модель и effort меняются конфигом без правки кода.
/// </summary>
public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    /// <summary>low / medium / high / xhigh / max. Пусто — не передавать (Haiku 4.5 effort не поддерживает).</summary>
    public string? Effort { get; set; } = "low";

    public int MaxTokens { get; set; } = 16000;

    /// <summary>
    /// Если модель откажется отвечать по соображениям безопасности (для рецептов — крайне маловероятно),
    /// запрос сам повторится на рекомендованной Anthropic модели. Поддерживается не всеми моделями —
    /// если выбранная модель ругается на параметр fallbacks, выключить.
    /// </summary>
    public bool RefusalFallback { get; set; } = true;
}
