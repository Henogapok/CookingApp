using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
                     (BotCallbacks.DraftEdit(draftId), BotCallbacks.DraftEditPrefix),
                     (BotCallbacks.RecipeEdit(draftId), BotCallbacks.RecipeEditPrefix),
                     (BotCallbacks.RecipeDelete(draftId), BotCallbacks.RecipeDeletePrefix),
                     (BotCallbacks.RecipeDeleteConfirm(draftId), BotCallbacks.RecipeDeleteConfirmPrefix),
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

    [Fact]
    public void DraftEditLink_RoundTrips()
    {
        var draftId = Guid.NewGuid();

        Assert.Equal((draftId, 42), BotCallbacks.ParseDraftEditLink(BotCallbacks.DraftEditLink(draftId, 42)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://example.com/")]
    [InlineData("https://draft.invalid/not-a-guid/1")]
    [InlineData("https://draft.invalid/00000000-0000-0000-0000-000000000001")]
    public void ParseDraftEditLink_OtherUrls_ReturnNull(string? url)
    {
        Assert.Null(BotCallbacks.ParseDraftEditLink(url));
    }

    [Fact]
    public void FindDraftEditTarget_ReadsHiddenLinkFromPromptEntities()
    {
        var draftId = Guid.NewGuid();
        var prompt = new Message
        {
            Text = "\u200b✏️ Что поправить?",
            Entities =
            [
                new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = 1 },
                new MessageEntity { Type = MessageEntityType.TextLink, Offset = 0, Length = 1, Url = BotCallbacks.DraftEditLink(draftId, 7) },
            ],
        };

        Assert.Equal((draftId, 7), BotCallbacks.FindDraftEditTarget(prompt));
        Assert.Null(BotCallbacks.FindDraftEditTarget(new Message { Text = "привет" }));
    }

    [Fact]
    public void DraftActions_HasEditButton()
    {
        var draftId = Guid.NewGuid();

        var buttons = BotKeyboards.DraftActions(draftId, canApplyEstimates: false).InlineKeyboard.SelectMany(row => row).ToList();

        Assert.Contains(buttons, b => b.CallbackData == BotCallbacks.DraftEdit(draftId));
        Assert.DoesNotContain(buttons, b => b.CallbackData == BotCallbacks.DraftEstimate(draftId));
    }

    [Fact]
    public void RecipeCallbacks_DoNotMatchEachOtherOrOpenCard()
    {
        var recipeId = Guid.NewGuid();

        Assert.Null(BotCallbacks.MatchId(BotCallbacks.RecipeDeleteConfirm(recipeId), BotCallbacks.RecipeDeletePrefix));
        Assert.Null(BotCallbacks.MatchId(BotCallbacks.RecipeEdit(recipeId), BotCallbacks.RecipePrefix));
        Assert.Equal(recipeId, BotCallbacks.MatchId(BotCallbacks.Recipe(recipeId), BotCallbacks.RecipePrefix));
    }

    [Fact]
    public void RecipeActions_DeleteOnlyForAuthor()
    {
        var recipeId = Guid.NewGuid();

        var author = BotKeyboards.RecipeActions(recipeId, canDelete: true).InlineKeyboard.SelectMany(r => r).Select(b => b.CallbackData);
        var family = BotKeyboards.RecipeActions(recipeId, canDelete: false).InlineKeyboard.SelectMany(r => r).Select(b => b.CallbackData);

        Assert.Equal([BotCallbacks.RecipeEdit(recipeId), BotCallbacks.RecipeDelete(recipeId)], author);
        Assert.Equal([BotCallbacks.RecipeEdit(recipeId)], family);
    }

    [Fact]
    public void RecipeEditLink_IsFoundInPromptAndDiffersFromDraftLink()
    {
        var recipeId = Guid.NewGuid();
        var prompt = new Message
        {
            Text = "\u200b✏️ Что поменять?",
            Entities = [new MessageEntity { Type = MessageEntityType.TextLink, Offset = 0, Length = 1, Url = BotCallbacks.RecipeEditLink(recipeId) }],
        };

        Assert.Equal(recipeId, BotCallbacks.FindRecipeEditTarget(prompt));
        Assert.Null(BotCallbacks.FindDraftEditTarget(prompt));
        Assert.Null(BotCallbacks.ParseRecipeEditLink(BotCallbacks.DraftEditLink(recipeId, 1)));
    }
}
