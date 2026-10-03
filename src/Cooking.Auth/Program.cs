using Cooking.Application;
using Cooking.Auth.Extensions;
using Cooking.Auth.Options;
using Cooking.Infrastructure;
using Serilog;

// Сервер авторизации (OAuth 2.0 / OpenID Connect): вход через Telegram, выдаёт клиентам (PWA, Swagger) JWT для Cooking.Api.
var builder = WebApplication.CreateBuilder(args);
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

// Та же база и те же пользователи, что у Api; разбор рецептов (AddRecipeParsing) здесь не нужен.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddControllers();
builder.Services.AddLogin(builder.Configuration);
builder.Services.AddSessionCookie(authOptions);
builder.Services.AddOpenIddictServer(authOptions, builder.Environment);
builder.Services.AddClientsCors(authOptions);

var app = builder.Build();

await app.PrepareDatabaseAsync();

app.UseSerilogRequestLogging();
app.UseCors(ServiceCollectionExtensions.ClientsCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
