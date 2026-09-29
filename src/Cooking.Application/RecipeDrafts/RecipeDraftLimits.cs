namespace Cooking.Application.RecipeDrafts;

public static class RecipeDraftLimits
{
    /// <summary>
    /// Сколько блюд из одного текста разбираем сразу. Больше — пользователь сначала выбирает нужные:
    /// иначе ответ LLM не влезет в лимит токенов, а чат завалит превью.
    /// </summary>
    public const int MaxDishes = 5;

    /// <summary>Сколько названий блюд показываем на выбор (кнопками — длинный столбец неудобен).</summary>
    public const int MaxDishChoices = 30;

    public const int MaxDishTitleLength = 100;
}
