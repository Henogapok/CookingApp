using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.Entities.Users;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Persistence;

public class CookingDbContext(DbContextOptions<CookingDbContext> options)
    : DbContext(options), IDataContext, IDataProtectionKeyContext
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<User> Users => Set<User>();
    public DbSet<FamilyInvite> FamilyInvites => Set<FamilyInvite>();
    public DbSet<LoginRequest> LoginRequests => Set<LoginRequest>();

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeDraft> RecipeDrafts => Set<RecipeDraft>();
    public DbSet<SourceType> SourceTypes => Set<SourceType>();
    public DbSet<Complexity> Complexities => Set<Complexity>();

    public DbSet<IngredientCatalog> IngredientCatalog => Set<IngredientCatalog>();
    public DbSet<IngredientCategory> IngredientCategories => Set<IngredientCategory>();
    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();
    public DbSet<DataSource> DataSources => Set<DataSource>();

    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TagType> TagTypes => Set<TagType>();
    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();

    /// <summary>
    /// Ключи Data Protection сервера авторизации (Cooking.Auth): ими шифруется cookie сессии.
    /// Хранятся в базе, чтобы сессии переживали перезапуск/деплой контейнера и попадали в бэкап вместе с данными.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CookingDbContext).Assembly);

        // Таблицы сервера авторизации (Cooking.Auth): клиенты, scope'ы, выданные разрешения и токены.
        modelBuilder.UseOpenIddict();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        => Database.CanConnectAsync(cancellationToken);
}
