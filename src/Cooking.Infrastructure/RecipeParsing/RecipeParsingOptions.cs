namespace Cooking.Infrastructure.RecipeParsing;

public class RecipeParsingOptions
{
    public const string SectionName = "RecipeParsing";

    /// <summary>
    /// Сколько черновиков разбирается одновременно. Остальные ждут в очереди — так несколько пользователей
    /// не блокируют друг друга, а всплеск запросов не упирается в лимиты API LLM.
    /// </summary>
    public int MaxParallelism { get; set; } = 3;
}
