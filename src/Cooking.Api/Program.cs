using Cooking.Bot;
using Cooking.Api.ExceptionHandling;
using Cooking.Api.Middleware;
using Cooking.Application;
using Cooking.Infrastructure;
using Cooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Serilog;

const string PwaCorsPolicy = "Pwa";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRecipeParsing(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddTelegramBot(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Токены выдаёт Cooking.Auth (OpenID Connect). Api только проверяет JWT: подпись — по ключам из
// {Authority}/.well-known/jwks (скачиваются и кэшируются сами), aud = cooking-api, срок действия.
var authority = builder.Configuration["Auth:Authority"];
var audience = builder.Configuration["Auth:Audience"] ?? "cooking-api";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        // Оставляем claim'ы как в токене ("sub", "name"), без переименования в длинные URI из WS-Federation.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    });
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    if (authority is null)
        return;

    // Кнопка Authorize в Swagger UI: обычный вход через Cooking.Auth (Authorization Code + PKCE), как у PWA.
    var authorityUri = new Uri(authority);
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(authorityUri, "connect/authorize"),
                TokenUrl = new Uri(authorityUri, "connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    ["openid"] = "OpenID Connect",
                    ["profile"] = "Имя пользователя",
                    [audience] = "Cooking API",
                },
            },
        },
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("oauth2", document)] = ["openid", "profile", audience],
    });
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    // TODO: заменить на реальный origin PWA, когда она появится.
    options.AddPolicy(PwaCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// До запуска hosted-сервисов (бот, очередь разбора): им уже нужна актуальная схема.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(app.Configuration["Auth:SwaggerClientId"] ?? "cooking-swagger");
        options.OAuthUsePkce();
        options.OAuthScopes("openid", "profile", audience);
    });
}

app.UseHttpsRedirection();
app.UseCors(PwaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapTelegramWebhook();

app.Run();
