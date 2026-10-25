using Cooking.Auth.Identity;
using Cooking.Auth.Options;
using Cooking.Auth.Seeding;
using Cooking.Auth.Services;
using System.Security.Cryptography.X509Certificates;
using Cooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ClientsCorsPolicy = "Clients";

    /// <summary>Вход через бота и через виджет Telegram, поиск пользователя для токенов, синхронизация клиентов.</summary>
    public static IServiceCollection AddLogin(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));

        services.AddHttpClient();
        services.AddSingleton<TelegramBotInfo>();
        services.AddScoped<BotLoginService>();
        services.AddScoped<TelegramWidgetLoginService>();
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

    /// <summary>
    /// Ключи Data Protection, которыми ASP.NET шифрует cookie сессии (и cookie входа через бота), — в Postgres.
    /// По умолчанию они лежат в файловой системе контейнера и пропадают при каждом деплое — вместе со всеми сессиями.
    /// </summary>
    public static IServiceCollection AddPersistentDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            // Имя приложения отделяет наши ключи: cookie, зашифрованную другим приложением с той же базой, Auth не примет.
            .SetApplicationName("Cooking.Auth")
            .PersistKeysToDbContext<CookingDbContext>();

        return services;
    }

    /// <summary>
    /// В проде Auth стоит за Caddy: тот принимает HTTPS и передаёт запрос по обычному HTTP, а настоящие схему и IP клиента
    /// кладёт в заголовки X-Forwarded-Proto / X-Forwarded-For. Без их чтения Auth считает запрос http-запросом —
    /// OpenIddict отвергает его (требует HTTPS), а адреса в discovery-документе получаются http://.
    /// </summary>
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // По умолчанию заголовкам верят только от localhost, а Caddy — другой контейнер с заранее неизвестным IP.
            // Верить всем безопасно, пока порт Auth не опубликован наружу и достучаться до него может только Caddy.
            forwarded.KnownNetworks.Clear();
            forwarded.KnownProxies.Clear();
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

                var certificates = options.Certificates;
                if (certificates.IsConfigured)
                {
                    // Прод: .pfx на docker-volume — ключи те же после перезапуска и деплоя.
                    server
                        .AddSigningCertificate(CertificateFile.LoadOrCreate(certificates.SigningPath!, certificates.Password,
                            "Cooking.Auth signing", X509KeyUsageFlags.DigitalSignature))
                        .AddEncryptionCertificate(CertificateFile.LoadOrCreate(certificates.EncryptionPath!, certificates.Password,
                            "Cooking.Auth encryption", X509KeyUsageFlags.KeyEncipherment));
                }
                else if (environment.IsDevelopment())
                {
                    // Локально — самоподписанные сертификаты в хранилище пользователя Windows.
                    server
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Auth:Certificates:SigningPath and Auth:Certificates:EncryptionPath must be set outside Development.");
                }

                // Passthrough: после проверок OpenIddict запрос доходит до AuthorizationController.
                server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            });

        return services;
    }

    /// <summary>
    /// Клиент ходит в Auth запросами из браузера — с другого origin, поэтому нужен CORS. GET — настройки и ключи
    /// (/.well-known/openid-configuration и jwks, их читает oidc-client-ts в PWA), POST — обмен code и refresh-токена (/connect/token).
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
            .WithMethods("GET", "POST")));

        return services;
    }
}
