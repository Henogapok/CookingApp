using Cooking.Auth.Identity;
using Cooking.Auth.Options;
using Cooking.Auth.Seeding;
using Cooking.Auth.Services;
using Cooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ClientsCorsPolicy = "Clients";

    /// <summary>Вход через бота, поиск пользователя для токенов, синхронизация клиентов.</summary>
    public static IServiceCollection AddLogin(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));

        services.AddHttpClient();
        services.AddSingleton<TelegramBotInfo>();
        services.AddScoped<BotLoginService>();
        services.AddScoped<UserResolver>();
        services.AddScoped<OAuthClientSeeder>();

        return services;
    }

    /// <summary>Сессия на сервере авторизации: пока она жива, любой клиент получает токены без повторного входа (SSO).</summary>
    public static IServiceCollection AddSessionCookie(this IServiceCollection services, AuthOptions options)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookie =>
            {
                cookie.LoginPath = "/login";
                cookie.ReturnUrlParameter = "returnUrl";
                cookie.Cookie.Name = "cooking.session";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                // Lax, а не Strict: на /connect/authorize браузер приходит переходом с сайта клиента — cookie должна дойти.
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.ExpireTimeSpan = TimeSpan.FromDays(options.SessionLifetimeDays);
                cookie.SlidingExpiration = true;
            });

        return services;
    }

    /// <summary>Сервер OAuth 2.0 / OpenID Connect: эндпоинты, flow, scope'ы, время жизни и подпись токенов.</summary>
    public static IServiceCollection AddOpenIddictServer(this IServiceCollection services, AuthOptions options, IHostEnvironment environment)
    {
        services.AddOpenIddict()
            .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<CookingDbContext>())
            .AddServer(server =>
            {
                server
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetEndSessionEndpointUris("connect/logout");

                // Authorization Code + PKCE — правильный flow для SPA; refresh-токены — чтобы не входить каждые 15 минут.
                server
                    .AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .RequireProofKeyForCodeExchange();

                server.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.OfflineAccess, AuthConstants.ApiScope);

                server
                    .SetAccessTokenLifetime(TimeSpan.FromMinutes(options.AccessTokenLifetimeMinutes))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(options.RefreshTokenLifetimeDays));

                if (options.Issuer is { Length: > 0 } issuer)
                    server.SetIssuer(new Uri(issuer));

                // По умолчанию OpenIddict шифрует access token (JWE) — тогда прочитать его может только он сам.
                // Нам нужен обычный подписанный JWT: Cooking.Api проверяет подпись по публичным ключам из /.well-known/jwks.
                server.DisableAccessTokenEncryption();

                if (!environment.IsDevelopment())
                    throw new InvalidOperationException("Signing/encryption certificates for production are not configured yet.");

                // Локально — самоподписанные сертификаты в хранилище пользователя Windows.
                server
                    .AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Passthrough: после проверок OpenIddict запрос доходит до AuthorizationController.
                server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            });

        return services;
    }

    /// <summary>
    /// Клиент меняет code на токены запросом из браузера (fetch на /connect/token) — с другого origin, поэтому нужен CORS.
    /// Разрешаем ровно те origin'ы, куда зарегистрированы redirect URI клиентов.
    /// </summary>
    public static IServiceCollection AddClientsCors(this IServiceCollection services, AuthOptions options)
    {
        var origins = options.Clients
            .SelectMany(c => c.RedirectUris)
            .Select(uri => new Uri(uri).GetLeftPart(UriPartial.Authority))
            .Distinct()
            .ToArray();

        services.AddCors(cors => cors.AddPolicy(ClientsCorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .WithMethods("POST")));

        return services;
    }
}
