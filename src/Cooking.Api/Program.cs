using Cooking.Api.Extensions;
using Cooking.Api.ExceptionHandling;
using Cooking.Api.Middleware;
using Cooking.Api.Options;
using Cooking.Application;
using Cooking.Bot;
using Cooking.Infrastructure;
using Cooking.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRecipeParsing(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddTelegramBot(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(authOptions);
builder.Services.AddSwaggerWithOAuth(authOptions);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// CORS не нужен: PWA ходит в Api по относительному /api на свой же origin (dev — прокси Vite, прод — Caddy).

var app = builder.Build();

// До запуска hosted-сервисов (бот, очередь разбора): им уже нужна актуальная схема.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseSwaggerWithOAuth(authOptions);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapTelegramWebhook();

app.Run();
