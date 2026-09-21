using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.ReferenceData;
using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class ReferenceDataRepositoryService(IDataContext dataContext) : IReferenceDataRepositoryService
{
    public Task<Result<Guid>> CreateSourceTypeAsync(string name, CancellationToken cancellationToken) =>
        CreateAsync<SourceType>(name, cancellationToken);

    public Task<Result> UpdateSourceTypeAsync(Guid id, string name, CancellationToken cancellationToken) =>
        UpdateAsync<SourceType>(id, name, cancellationToken);

    public Task<Result> DeleteSourceTypeAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync<SourceType>(id, cancellationToken);

    public Task<Result<ReferenceEntityDto>> GetSourceTypeByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync<SourceType>(id, cancellationToken);

    public Task<Result<List<ReferenceEntityDto>>> GetSourceTypesAsync(CancellationToken cancellationToken) =>
        GetAllAsync<SourceType>(cancellationToken);

    public Task<Result<Guid>> CreateComplexityAsync(string name, CancellationToken cancellationToken) =>
        CreateAsync<Complexity>(name, cancellationToken);

    public Task<Result> UpdateComplexityAsync(Guid id, string name, CancellationToken cancellationToken) =>
        UpdateAsync<Complexity>(id, name, cancellationToken);

    public Task<Result> DeleteComplexityAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync<Complexity>(id, cancellationToken);

    public Task<Result<ReferenceEntityDto>> GetComplexityByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync<Complexity>(id, cancellationToken);

    public Task<Result<List<ReferenceEntityDto>>> GetComplexitiesAsync(CancellationToken cancellationToken) =>
        GetAllAsync<Complexity>(cancellationToken);

    public Task<Result<Guid>> CreateIngredientCategoryAsync(string name, CancellationToken cancellationToken) =>
        CreateAsync<IngredientCategory>(name, cancellationToken);

    public Task<Result> UpdateIngredientCategoryAsync(Guid id, string name, CancellationToken cancellationToken) =>
        UpdateAsync<IngredientCategory>(id, name, cancellationToken);

    public Task<Result> DeleteIngredientCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync<IngredientCategory>(id, cancellationToken);

    public Task<Result<ReferenceEntityDto>> GetIngredientCategoryByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync<IngredientCategory>(id, cancellationToken);

    public Task<Result<List<ReferenceEntityDto>>> GetIngredientCategoriesAsync(CancellationToken cancellationToken) =>
        GetAllAsync<IngredientCategory>(cancellationToken);

    public Task<Result<Guid>> CreateDataSourceAsync(string name, CancellationToken cancellationToken) =>
        CreateAsync<DataSource>(name, cancellationToken);

    public Task<Result> UpdateDataSourceAsync(Guid id, string name, CancellationToken cancellationToken) =>
        UpdateAsync<DataSource>(id, name, cancellationToken);

    public Task<Result> DeleteDataSourceAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync<DataSource>(id, cancellationToken);

    public Task<Result<ReferenceEntityDto>> GetDataSourceByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync<DataSource>(id, cancellationToken);

    public Task<Result<List<ReferenceEntityDto>>> GetDataSourcesAsync(CancellationToken cancellationToken) =>
        GetAllAsync<DataSource>(cancellationToken);

    public Task<Result<Guid>> CreateTagTypeAsync(string name, CancellationToken cancellationToken) =>
        CreateAsync<TagType>(name, cancellationToken);

    public Task<Result> UpdateTagTypeAsync(Guid id, string name, CancellationToken cancellationToken) =>
        UpdateAsync<TagType>(id, name, cancellationToken);

    public Task<Result> DeleteTagTypeAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync<TagType>(id, cancellationToken);

    public Task<Result<ReferenceEntityDto>> GetTagTypeByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync<TagType>(id, cancellationToken);

    public Task<Result<List<ReferenceEntityDto>>> GetTagTypesAsync(CancellationToken cancellationToken) =>
        GetAllAsync<TagType>(cancellationToken);

    // Generic helpers are an internal implementation detail only: they let the 5 identical-shape
    // reference tables share one query/update/delete body without exposing <TEntity> on the public
    // interface (and therefore without the open-generic MediatR/DI registration issues that caused
    // problems when TEntity was part of the public Command/Handler surface).
    private async Task<Result<Guid>> CreateAsync<TEntity>(string name, CancellationToken cancellationToken)
        where TEntity : ReferenceEntity, new()
    {
        var entity = new TEntity { Name = name };

        dataContext.Set<TEntity>().Add(entity);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(entity.Id);
    }

    private async Task<Result> UpdateAsync<TEntity>(Guid id, string name, CancellationToken cancellationToken)
        where TEntity : ReferenceEntity
    {
        var entity = await dataContext.Set<TEntity>().FindAsync([id], cancellationToken);

        if (entity is null)
            return Result.Fail(new AppError($"{typeof(TEntity).Name} with id '{id}' was not found.", ErrorCode.NotFound));

        entity.Name = name;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private async Task<Result> DeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken)
        where TEntity : ReferenceEntity
    {
        var entity = await dataContext.Set<TEntity>().FindAsync([id], cancellationToken);

        if (entity is null)
            return Result.Fail(new AppError($"{typeof(TEntity).Name} with id '{id}' was not found.", ErrorCode.NotFound));

        dataContext.Set<TEntity>().Remove(entity);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private async Task<Result<ReferenceEntityDto>> GetByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken)
        where TEntity : ReferenceEntity
    {
        var entity = await dataContext.Set<TEntity>().FindAsync([id], cancellationToken);

        return entity is null
            ? Result.Fail(new AppError($"{typeof(TEntity).Name} with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(new ReferenceEntityDto(entity.Id, entity.Name));
    }

    private async Task<Result<List<ReferenceEntityDto>>> GetAllAsync<TEntity>(CancellationToken cancellationToken)
        where TEntity : ReferenceEntity
    {
        var entities = await dataContext.Set<TEntity>()
            .OrderBy(e => e.Name)
            .Select(e => new ReferenceEntityDto(e.Id, e.Name))
            .ToListAsync(cancellationToken);

        return Result.Ok(entities);
    }
}
