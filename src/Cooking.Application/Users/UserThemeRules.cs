namespace Cooking.Application.Users;

/// <summary>
/// Какой может быть тема. Список тем сервер не повторяет (он в PWA, тема — только её настройка): проверяем лишь форму id,
/// а незнакомое значение PWA сама считает «Авто». Новая тема в PWA не требует изменений на сервере.
/// </summary>
public static class UserThemeRules
{
    public const int MaxLength = 32;

    /// <summary>Id тем в PWA — латинские буквы в нижнем регистре («sage», «midnight», «auto»).</summary>
    public const string Pattern = "^[a-z]+$";
}
