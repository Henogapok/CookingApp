using Cooking.Application;
using Cooking.Auth;
using Cooking.Auth.Login;
using Cooking.Auth.OpenIddict;
using Cooking.Infrastructure;
using Cooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using static OpenIddict.Abstractions.OpenIddictConstants;

// Сервер авторизации (OAuth 2.0 / OpenID Connect): вход через Telegram, выдаёт клиентам (PWA, Swagger) JWT для Cooking.Api.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

// Та же база и те же пользователи, что у Api; разбор рецептов (AddRecipeParsing) здесь не нужен.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TelegramBotInfo>();

builder.Services.AddControllers();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

// Клиент меняет code на токены запросом из браузера (fetch на /connect/token) — с другого origin, поэтому нужен CORS.
// Разрешаем ровно те origin'ы, куда зарегистрированы redirect URI клиентов.
const string ClientsCorsPolicy = "Clients";
var clientOrigins = authOptions.Clients
    .SelectMany(c => c.RedirectUris)
    .Select(uri => new Uri(uri).GetLeftPart(UriPartial.Authority))
    .Distinct()
    .ToArray();
builder.Services.AddCors(options => options.AddPolicy(ClientsCorsPolicy, policy => policy
    .WithOrigins(clientOrigins)
    .AllowAnyHeader()
    .WithMethods("POST")));

// Сессия на сервере авторизации: пока она жива, любой клиент получает токены без повторного входа (SSO).
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ReturnUrlParameter = "returnUrl";
        options.Cookie.Name = "cooking.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Lax, а не Strict: на /connect/authorize браузер приходит переходом с сайта клиента — cookie должна дойти.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(authOptions.SessionLifetimeDays);
        options.SlidingExpiration = true;
    });

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<CookingDbContext>())
    .AddServer(options =>
    {
        options
            .SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetEndSessionEndpointUris("connect/logout");

        // Authorization Code + PKCE — единственный правильный flow для SPA; refresh-токены — чтобы не входить каждые 15 минут.
        options
            .AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .RequireProofKeyForCodeExchange();

        options.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.OfflineAccess, AuthOptions.ApiScope);

        options
            .SetAccessTokenLifetime(TimeSpan.FromMinutes(authOptions.AccessTokenLifetimeMinutes))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(authOptions.RefreshTokenLifetimeDays));

        if (authOptions.Issuer is { Length: > 0 } issuer)
            options.SetIssuer(new Uri(issuer));

        // По умолчанию OpenIddict шифрует access token (JWE) — тогда прочитать его может только он сам.
        // Нам нужен обычный подписанный JWT: Cooking.Api проверяет подпись по публичным ключам из /.well-known/jwks.
        options.DisableAccessTokenEncryption();

        if (builder.Environment.IsDevelopment())
        {
            // Локально — самоподписанные сертификаты в хранилище пользователя Windows.
            options
                .AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            throw new InvalidOperationException("Signing/encryption certificates for production are not configured yet.");
        }

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    });

builder.Services.AddHostedService<OpenIddictClientSeeder>();

var app = builder.Build();

// До сидера клиентов (hosted service): ему нужны таблицы OpenIddict.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

app.UseSerilogRequestLogging();

app.UseCors(ClientsCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapLoginEndpoints();

app.Run();
