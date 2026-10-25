using Cooking.Auth.Identity;
using Cooking.Auth.Options;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.Seeding;

/// <summary>
/// Шаг запуска (как миграции): клиенты описаны в настройках (Auth:Clients), а OpenIddict хранит их в базе —
/// при каждом старте создаём новых и обновляем существующих. Так адреса возврата для разработки и прода
/// живут в конфиге, а не в миграциях.
/// </summary>
public class OAuthClientSeeder(IOpenIddictApplicationManager applications, IOptions<AuthOptions> options, ILogger<OAuthClientSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var client in options.Value.Clients)
        {
            var descriptor = CreateDescriptor(client);

            if (await applications.FindByClientIdAsync(client.ClientId, cancellationToken) is { } existing)
                await applications.UpdateAsync(existing, descriptor, cancellationToken);
            else
                await applications.CreateAsync(descriptor, cancellationToken);

            logger.LogInformation("OAuth client {ClientId} registered", client.ClientId);
        }
    }

    private static OpenIddictApplicationDescriptor CreateDescriptor(AuthClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            // Public: SPA и Swagger не могут хранить секрет — вместо него PKCE.
            ClientType = ClientTypes.Public,
            // Свои клиенты — экран «разрешить доступ?» не показываем.
            ConsentType = ConsentTypes.Implicit,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + AuthConstants.ApiScope,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };

        foreach (var uri in client.RedirectUris)
            descriptor.RedirectUris.Add(new Uri(uri));
        foreach (var uri in client.PostLogoutRedirectUris)
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

        return descriptor;
    }
}
