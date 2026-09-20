using Cooking.Application.Common.Interfaces;
using Cooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<CookingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDataContext>(sp => sp.GetRequiredService<CookingDbContext>());

        return services;
    }
}
