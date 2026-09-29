using FluentResults;

namespace Cooking.Application.RecipeDrafts.Parsing;

/// <summary>
/// Разбор свободного текста рецепта (LLM). Ошибки: ErrorCode.Unavailable — парсер не настроен,
/// ErrorCode.ExternalService — сервис не ответил или вернул не то.
/// </summary>
public interface IRecipeTextParser
{
    Task<Result<ParsedRecipe>> ParseAsync(RecipeParsingRequest request, CancellationToken cancellationToken);
}

/// <param name="CatalogIngredientNames">Названия из каталога — LLM должен переиспользовать их, а не плодить дубли.</param>
/// <param name="TagNames">Теги из базы — LLM выбирает только из них.</param>
/// <param name="CurrentRecipeJson">Для правки: текущая версия черновика (RecipeDraftMapper.ToCorrectionJson).</param>
/// <param name="Correction">Для правки: что пользователь просит изменить.</param>
public record RecipeParsingRequest(
    string Text,
    IReadOnlyList<string> CatalogIngredientNames,
    IReadOnlyList<string> TagNames,
    string? CurrentRecipeJson = null,
    string? Correction = null);
