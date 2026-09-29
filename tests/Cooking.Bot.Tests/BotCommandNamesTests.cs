namespace Cooking.Bot.Tests;

public class BotCommandNamesTests
{
    [Theory]
    [InlineData("/search курица", "курица")]
    [InlineData("/search   курица с рисом  ", "курица с рисом")]
    [InlineData("/search@CookingBot курица", "курица")]
    [InlineData("/SEARCH Курица", "Курица")]
    [InlineData("/search", "")]
    [InlineData("/search   ", "")]
    [InlineData("/search@CookingBot", "")]
    public void MatchArguments_SearchCommand_ReturnsArguments(string text, string expected)
    {
        Assert.Equal(expected, BotCommandNames.MatchArguments(text, BotCommandNames.Search));
    }

    [Theory]
    [InlineData("/searching курица")]
    [InlineData("/start")]
    [InlineData("курица")]
    [InlineData("")]
    public void MatchArguments_OtherText_ReturnsNull(string text)
    {
        Assert.Null(BotCommandNames.MatchArguments(text, BotCommandNames.Search));
    }

    [Fact]
    public void MatchArguments_StartWithDeepLinkCode_ReturnsCode()
    {
        Assert.Equal("abc_123", BotCommandNames.MatchArguments("/start abc_123", BotCommandNames.Start));
    }
}
