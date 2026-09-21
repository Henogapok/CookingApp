using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.MeasurementUnits;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class MeasurementUnitRepositoryService(IDataContext dataContext) : IMeasurementUnitRepositoryService
{
    public async Task<Result<Guid>> CreateAsync(string name, string abbreviation, CancellationToken cancellationToken)
    {
        var unit = new Domain.Entities.Ingredients.MeasurementUnit { Name = name, Abbreviation = abbreviation };

        dataContext.MeasurementUnits.Add(unit);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(unit.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, string name, string abbreviation, CancellationToken cancellationToken)
    {
        var unit = await dataContext.MeasurementUnits.FindAsync([id], cancellationToken);

        if (unit is null)
            return Result.Fail(new AppError($"MeasurementUnit with id '{id}' was not found.", ErrorCode.NotFound));

        unit.Name = name;
        unit.Abbreviation = abbreviation;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var unit = await dataContext.MeasurementUnits.FindAsync([id], cancellationToken);

        if (unit is null)
            return Result.Fail(new AppError($"MeasurementUnit with id '{id}' was not found.", ErrorCode.NotFound));

        dataContext.MeasurementUnits.Remove(unit);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<MeasurementUnitDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var unit = await dataContext.MeasurementUnits.FindAsync([id], cancellationToken);

        return unit is null
            ? Result.Fail(new AppError($"MeasurementUnit with id '{id}' was not found.", ErrorCode.NotFound))
            : Result.Ok(new MeasurementUnitDto(unit.Id, unit.Name, unit.Abbreviation));
    }

    public async Task<Result<List<MeasurementUnitDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var units = await dataContext.MeasurementUnits
            .OrderBy(u => u.Name)
            .Select(u => new MeasurementUnitDto(u.Id, u.Name, u.Abbreviation))
            .ToListAsync(cancellationToken);

        return Result.Ok(units);
    }
}
