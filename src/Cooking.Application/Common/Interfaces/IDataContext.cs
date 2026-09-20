using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Common.Interfaces;

/// <summary>
/// Абстракция над DbContext: use case'ы в Application зависят от этого интерфейса,
/// а не от конкретного EF Core DbContext из Infrastructure (Dependency Inversion).
/// </summary>
public interface IDataContext
{
    DbSet<Family> Families { get; }
    DbSet<User> Users { get; }

    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<RecipeStep> RecipeSteps { get; }
    DbSet<SourceType> SourceTypes { get; }
    DbSet<Complexity> Complexities { get; }

    DbSet<IngredientCatalog> IngredientCatalog { get; }
    DbSet<IngredientCategory> IngredientCategories { get; }
    DbSet<MeasurementUnit> MeasurementUnits { get; }
    DbSet<DataSource> DataSources { get; }

    DbSet<Tag> Tags { get; }
    DbSet<TagType> TagTypes { get; }
    DbSet<RecipeTag> RecipeTags { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
