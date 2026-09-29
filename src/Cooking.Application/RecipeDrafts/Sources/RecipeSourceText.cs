using System.Text;
using System.Text.RegularExpressions;

namespace Cooking.Application.RecipeDrafts.Sources;

/// <summary>Чистые функции вокруг видео-источников: распознавание ссылок и сборка текста для LLM.</summary>
public static partial class RecipeSourceText
{
    [GeneratedRegex(@"https?://(?:www\.|m\.)?instagram\.com/(?:reels?|p|tv)/([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex InstagramLinkRegex();

    /// <summary>
    /// Первая ссылка на пост/Reels Instagram в тексте, приведённая к виду https://www.instagram.com/reel/{код}/ —
    /// без хвостов вроде ?igsh=…, чтобы одна и та же Reels всегда давала одну и ту же ссылку. null — ссылки нет.
    /// </summary>
    public static string? FindInstagramLink(string text)
    {
        var match = InstagramLinkRegex().Match(text);
        return match.Success ? $"https://www.instagram.com/reel/{match.Groups[1].Value}/" : null;
    }

    /// <summary>
    /// Текст для разбора: описание под видео и расшифровка речи — рецепт бывает в любом из них.
    /// null — ни там, ни там ничего нет (например, видео с музыкой и пустым описанием).
    /// </summary>
    public static string? Build(string? caption, string? transcript)
    {
        var text = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(caption))
            text.AppendLine("Описание под видео:").AppendLine(caption.Trim());

        if (!string.IsNullOrWhiteSpace(transcript))
        {
            if (text.Length > 0)
                text.AppendLine();
            text.AppendLine("Расшифровка речи из видео:").AppendLine(transcript.Trim());
        }

        return text.Length == 0 ? null : text.ToString().TrimEnd();
    }
}
