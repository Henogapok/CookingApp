using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Tags;
using Cooking.Domain.Entities.Tags;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class TagRepositoryService(IDataContext dataContext) : ITagRepositoryService
{
    public async Task<Result<Guid>> CreateAsync(string name, Guid tagTypeId, CancellationToken cancellationToken)
    {
        var fkCheck = await ValidateTagTypeAsync(tagTypeId, cancellationToken);
        if (fkCheck.IsFailed)
            return fkCheck.ToResult<Guid>();

        var tag = new Tag { Name = name, TagTypeId = tagTypeId };

        dataContext.Tags.Add(tag);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(tag.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, string name, Guid tagTypeId, CancellationToken cancellationToken)
    {
        var tag = await dataContext.Tags.FindAsync([id], cancellationToken);

        if (tag is null)
            return Result.Fail(new AppError($"Tag with id '{id}' was not found.", ErrorCode.NotFound));

        var fkCheck = await ValidateTagTypeAsync(tagTypeId, cancellationToken);
        if (fkCheck.IsFailed)
            return fkCheck;

        tag.Name = name;
        tag.TagTypeId = tagTypeId;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tag = await dataContext.Tags.FindAsync([id], cancellationToken);

        if (tag is null)
            return Result.Fail(new AppError($"Tag with id '{id}' was not found.", ErrorCode.NotFound));

        // RecipeTag удаляется каскадом — тег просто снимается со всех рецептов.
        dataContext.Tags.Remove(tag);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<TagDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await dataContext.Tags
            .Where(t => t.Id == id)
            .Select(t => new TagDto(t.Id, t.Name, t.TagTypeId, t.TagType.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Fail(new AppError($"Tag with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(dto);
    }

    public async Task<Result<List<TagDto>>> GetAllAsync(Guid? tagTypeId, CancellationToken cancellationToken)
    {
        var query = dataContext.Tags.AsQueryable();

        if (tagTypeId is not null)
            query = query.Where(t => t.TagTypeId == tagTypeId);

        var tags = await query
            .OrderBy(t => t.TagType.Name)
            .ThenBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.TagTypeId, t.TagType.Name))
            .ToListAsync(cancellationToken);

        return Result.Ok(tags);
    }

    private async Task<Result> ValidateTagTypeAsync(Guid tagTypeId, CancellationToken cancellationToken) =>
        await dataContext.TagTypes.AnyAsync(x => x.Id == tagTypeId, cancellationToken)
            ? Result.Ok()
            : Result.Fail(new AppError($"TagType with id '{tagTypeId}' was not found.", ErrorCode.Validation));
}
