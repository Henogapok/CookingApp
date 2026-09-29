using Cooking.Application.RecipeDrafts.Sources;
using Cooking.Application.Common.Errors;
using Cooking.Infrastructure.RecipeParsing;

namespace Cooking.Application.Tests.RecipeDrafts;

public class RecipeSourceTextTests
{
    [Theory]
    [InlineData("https://www.instagram.com/reel/DXKB5TGjhnP/?stkn=ZmZ4", "https://www.instagram.com/reel/DXKB5TGjhnP/")]
    [InlineData("глянь instagram: https://instagram.com/reels/DaSFnBZsJLo?igsh=1 !", "https://www.instagram.com/reel/DaSFnBZsJLo/")]
    [InlineData("https://m.instagram.com/p/Abc_1-2/", "https://www.instagram.com/reel/Abc_1-2/")]
    [InlineData("HTTPS://WWW.INSTAGRAM.COM/REEL/XyZ/", "https://www.instagram.com/reel/XyZ/")]
    public void FindInstagramLink_NormalizesLink(string text, string expected)
    {
        Assert.Equal(expected, RecipeSourceText.FindInstagramLink(text));
    }

    [Theory]
    [InlineData("https://www.instagram.com/some_blogger/")]
    [InlineData("https://www.youtube.com/shorts/abc")]
    [InlineData("Курица 500 г, обжарить 10 минут")]
    public void FindInstagramLink_NoReelLink_ReturnsNull(string text)
    {
        Assert.Null(RecipeSourceText.FindInstagramLink(text));
    }

    [Fact]
    public void Build_CombinesNonEmptyParts()
    {
        Assert.Equal("Описание под видео:\nА\n\nРасшифровка речи из видео:\nБ", RecipeSourceText.Build(" А ", "Б")!.ReplaceLineEndings("\n"));
        Assert.Equal("Расшифровка речи из видео:\nБ", RecipeSourceText.Build(null, "Б")!.ReplaceLineEndings("\n"));
        Assert.Null(RecipeSourceText.Build("  ", ""));
    }

    [Fact]
    public void YtDlp_ReadDescription_FromJsonLine()
    {
        const string stdout = "{\"id\": \"x\", \"description\": \"Рецепт👇\\nИнгредиенты\"}\n";

        Assert.Equal("Рецепт👇\nИнгредиенты", YtDlpVideoSourceLoader.ReadDescription(stdout));
        Assert.Null(YtDlpVideoSourceLoader.ReadDescription("not json"));
        Assert.Null(YtDlpVideoSourceLoader.ReadDescription("{\"id\": \"x\", \"description\": null}"));
    }

    [Theory]
    [InlineData("ERROR: [Instagram] abc: This content is not available", ErrorCode.NotFound)]
    [InlineData("ERROR: [Instagram] abc: Requested content is not available, rate-limit reached or login required", ErrorCode.ExternalService)]
    [InlineData("ERROR: [Instagram] abc: Main webpage is locked behind the login page", ErrorCode.ExternalService)]
    [InlineData("ERROR: Unable to download webpage: HTTP Error 429: Too Many Requests", ErrorCode.ExternalService)]
    public void YtDlp_ClassifyError(string stderr, ErrorCode expected)
    {
        Assert.Equal(expected, YtDlpVideoSourceLoader.ClassifyError(stderr));
    }
}
