using Telegram.Bot.Types.ReplyMarkups;

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

    [Fact]
    public void Callbacks_DraftButtons_FitTelegramLimitAndRoundTrip()
    {
        var draftId = Guid.NewGuid();

        foreach (var (data, prefix) in new[]
                 {
                     (BotCallbacks.DraftSave(draftId), BotCallbacks.DraftSavePrefix),
                     (BotCallbacks.DraftCancel(draftId), BotCallbacks.DraftCancelPrefix),
                     (BotCallbacks.DraftEstimate(draftId), BotCallbacks.DraftEstimatePrefix),
                 })
        {
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(data) <= 64, data);
            Assert.Equal(draftId, BotCallbacks.MatchId(data, prefix));
        }
    }

    [Theory]
    [InlineData("draft:save:not-a-guid")]
    [InlineData("recipe:00000000-0000-0000-0000-000000000001")]
    public void Callbacks_MatchId_OtherData_ReturnsNull(string data)
    {
        Assert.Null(BotCallbacks.MatchId(data, BotCallbacks.DraftSavePrefix));
    }

    [Fact]
    public void FindDraftId_FromPreviewButtons()
    {
        var draftId = Guid.NewGuid();

        Assert.Equal(draftId, BotCallbacks.FindDraftId(BotKeyboards.DraftActions(draftId, canApplyEstimates: true)));
        Assert.Null(BotCallbacks.FindDraftId(BotKeyboards.FamilyActions()));
        Assert.Null(BotCallbacks.FindDraftId(null));
    }
}
