using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.OpenIddict;

/// <summary>
/// Клиенты описаны в настройках (Auth:Clients) — при старте создаём или обновляем их в базе,
/// чтобы адреса возврата для разработки и прода жили в конфиге, а не в миграциях.
/// </summary>
public class OpenIddictClientSeeder(IServiceScopeFactory scopeFactory, IOptions<AuthOptions> options, ILogger<OpenIddictClientSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in options.Value.Clients)
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
                    Permissions.Prefixes.Scope + AuthOptions.ApiScope,
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            };

            foreach (var uri in client.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));
            foreach (var uri in client.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

            if (await manager.FindByClientIdAsync(client.ClientId, cancellationToken) is { } existing)
                await manager.UpdateAsync(existing, descriptor, cancellationToken);
            else
                await manager.CreateAsync(descriptor, cancellationToken);

            logger.LogInformation("OAuth client {ClientId} registered", client.ClientId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
