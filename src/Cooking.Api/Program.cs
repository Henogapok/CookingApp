using Cooking.Bot;
using Cooking.Api.ExceptionHandling;
using Cooking.Api.Middleware;
using Cooking.Application;
using Cooking.Infrastructure;
using Cooking.Infrastructure.Persistence;
using Serilog;

const string PwaCorsPolicy = "Pwa";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddTelegramBot(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(PwaCorsPolicy);

app.MapControllers();
app.MapTelegramWebhook();

app.Run();
