using Cooking.Application.Users;
using Cooking.Auth.Identity;
using Cooking.Auth.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.Tests;

public class UserPrincipalFactoryTests
{
    private const long AdminTelegramId = 111;

    private static readonly AdminOptions Admin = new() { TelegramIds = [AdminTelegramId] };

    private static UserDto User(long telegramId) => new(Guid.NewGuid(), telegramId, "Иван", null, null, null);

    [Fact]
    public void GetRoles_TelegramIdInAdminList_ReturnsAdmin()
    {
        Assert.Equal([UserRoles.Admin], UserPrincipalFactory.GetRoles(User(AdminTelegramId), Admin));
    }

    [Fact]
    public void GetRoles_TelegramIdNotInAdminList_ReturnsNoRoles()
    {
        Assert.Empty(UserPrincipalFactory.GetRoles(User(222), Admin));
    }

    [Fact]
    public void GetRoles_NoAdminsConfigured_ReturnsNoRoles()
    {
        Assert.Empty(UserPrincipalFactory.GetRoles(User(AdminTelegramId), new AdminOptions()));
    }

    [Fact]
    public void CreateForTokens_WithAdminRole_PutsRoleOnlyIntoAccessToken()
    {
        var principal = UserPrincipalFactory.CreateForTokens(
            User(AdminTelegramId), [Scopes.OpenId, Scopes.Profile], [UserRoles.Admin]);

        var role = Assert.Single(principal.Claims, c => c.Type == Claims.Role);
        Assert.Equal(UserRoles.Admin, role.Value);
        Assert.Equal([Destinations.AccessToken], role.GetDestinations().ToArray());
    }

    [Fact]
    public void CreateForTokens_WithoutRoles_HasNoRoleClaim()
    {
        var principal = UserPrincipalFactory.CreateForTokens(User(222), [Scopes.OpenId], []);

        Assert.DoesNotContain(principal.Claims, c => c.Type == Claims.Role);
    }
}
