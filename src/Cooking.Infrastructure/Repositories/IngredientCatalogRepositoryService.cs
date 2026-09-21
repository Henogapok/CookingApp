using System.Linq.Expressions;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Ingredients;
using Cooking.Domain.Entities.Ingredients;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class IngredientCatalogRepositoryService(IDataContext dataContext) : IIngredientCatalogRepositoryService
{
    private static readonly Expression<Func<IngredientCatalog, IngredientCatalogDto>> ProjectToDto = e => new IngredientCatalogDto(
        e.Id, e.Name,
        e.CategoryId, e.Category.Name,
        e.BaseUnitId, e.BaseUnit.Name, e.BaseUnit.Abbreviation,
        e.PricePer100g, e.CaloriesPer100g, e.ProteinPer100g, e.FatPer100g, e.CarbsPer100g,
        e.CreatedBySourceId, e.CreatedBySource.Name,
        e.NutritionSourceId, e.NutritionSource.Name);

    public async Task<Result<Guid>> CreateAsync(IngredientCatalogFields fields, CancellationToken cancellationToken)
    {
        var fkCheck = await ValidateForeignKeysAsync(fields, cancellationToken);
        if (fkCheck.IsFailed)
            return fkCheck.ToResult<Guid>();

        var entity = new IngredientCatalog
        {
            Name = fields.Name,
            CategoryId = fields.CategoryId,
            BaseUnitId = fields.BaseUnitId,
            PricePer100g = fields.PricePer100G,
            CaloriesPer100g = fields.CaloriesPer100G,
            ProteinPer100g = fields.ProteinPer100G,
            FatPer100g = fields.FatPer100G,
            CarbsPer100g = fields.CarbsPer100G,
            CreatedBySourceId = fields.CreatedBySourceId,
            NutritionSourceId = fields.NutritionSourceId,
        };

        dataContext.IngredientCatalog.Add(entity);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, IngredientCatalogFields fields, CancellationToken cancellationToken)
    {
        var entity = await dataContext.IngredientCatalog.FindAsync([id], cancellationToken);

        if (entity is null)
            return Result.Fail(new AppError($"IngredientCatalog with id '{id}' was not found.", ErrorCode.NotFound));

        var fkCheck = await ValidateForeignKeysAsync(fields, cancellationToken);
        if (fkCheck.IsFailed)
            return fkCheck;

        entity.Name = fields.Name;
        entity.CategoryId = fields.CategoryId;
        entity.BaseUnitId = fields.BaseUnitId;
        entity.PricePer100g = fields.PricePer100G;
        entity.CaloriesPer100g = fields.CaloriesPer100G;
        entity.ProteinPer100g = fields.ProteinPer100G;
        entity.FatPer100g = fields.FatPer100G;
        entity.CarbsPer100g = fields.CarbsPer100G;
        entity.CreatedBySourceId = fields.CreatedBySourceId;
        entity.NutritionSourceId = fields.NutritionSourceId;

        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dataContext.IngredientCatalog.FindAsync([id], cancellationToken);

        if (entity is null)
            return Result.Fail(new AppError($"IngredientCatalog with id '{id}' was not found.", ErrorCode.NotFound));

        dataContext.IngredientCatalog.Remove(entity);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IngredientCatalogDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await dataContext.IngredientCatalog
            .Where(e => e.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Fail(new AppError($"IngredientCatalog with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(dto);
    }

    public async Task<Result<List<IngredientCatalogDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await dataContext.IngredientCatalog
            .OrderBy(e => e.Name)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);

        return Result.Ok(items);
    }

    private async Task<Result> ValidateForeignKeysAsync(IngredientCatalogFields fields, CancellationToken cancellationToken)
    {
        if (!await dataContext.IngredientCategories.AnyAsync(x => x.Id == fields.CategoryId, cancellationToken))
            return Result.Fail(new AppError($"IngredientCategory with id '{fields.CategoryId}' was not found.", ErrorCode.Validation));

        if (!await dataContext.MeasurementUnits.AnyAsync(x => x.Id == fields.BaseUnitId, cancellationToken))
            return Result.Fail(new AppError($"MeasurementUnit with id '{fields.BaseUnitId}' was not found.", ErrorCode.Validation));

        if (!await dataContext.DataSources.AnyAsync(x => x.Id == fields.CreatedBySourceId, cancellationToken))
            return Result.Fail(new AppError($"DataSource with id '{fields.CreatedBySourceId}' was not found.", ErrorCode.Validation));

        if (!await dataContext.DataSources.AnyAsync(x => x.Id == fields.NutritionSourceId, cancellationToken))
            return Result.Fail(new AppError($"DataSource with id '{fields.NutritionSourceId}' was not found.", ErrorCode.Validation));

        return Result.Ok();
    }
}
