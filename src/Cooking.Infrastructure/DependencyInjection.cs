using Cooking.Application.Common.Interfaces;
using Cooking.Application.Families;
using Cooking.Application.Ingredients;
using Cooking.Application.MeasurementUnits;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.RecipeDrafts.Parsing;
using Cooking.Application.Recipes;
using Cooking.Application.ReferenceData;
using Cooking.Application.Tags;
using Cooking.Application.Users;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.RecipeParsing;
using Cooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<CookingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDataContext>(sp => sp.GetRequiredService<CookingDbContext>());

        services.AddScoped<IReferenceDataRepositoryService, ReferenceDataRepositoryService>();
        services.AddScoped<IMeasurementUnitRepositoryService, MeasurementUnitRepositoryService>();
        services.AddScoped<IIngredientCatalogRepositoryService, IngredientCatalogRepositoryService>();
        services.AddScoped<IUserRepositoryService, UserRepositoryService>();
        services.AddScoped<IFamilyRepositoryService, FamilyRepositoryService>();
        services.AddScoped<ITagRepositoryService, TagRepositoryService>();
        services.AddScoped<IRecipeRepositoryService, RecipeRepositoryService>();
        services.AddScoped<IRecipeDraftRepositoryService, RecipeDraftRepositoryService>();

        // Разбор рецептов: LLM + очередь в памяти процесса с ограниченной параллельностью.
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        services.Configure<RecipeParsingOptions>(configuration.GetSection(RecipeParsingOptions.SectionName));
        services.AddSingleton<IRecipeTextParser, ClaudeRecipeTextParser>();
        services.AddSingleton<InProcessRecipeParsingQueue>();
        services.AddSingleton<IRecipeParsingQueue>(sp => sp.GetRequiredService<InProcessRecipeParsingQueue>());
        services.AddHostedService<RecipeParsingBackgroundService>();

        // Реализация по умолчанию; бот (если настроен) регистрирует свою — TryAdd не перетрёт её при любом порядке.
        services.TryAddScoped<IRecipeDraftNotifier, NullRecipeDraftNotifier>();

        return services;
    }
}
