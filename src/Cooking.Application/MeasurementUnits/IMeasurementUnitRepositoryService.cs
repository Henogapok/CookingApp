using FluentResults;

namespace Cooking.Application.MeasurementUnits;

public interface IMeasurementUnitRepositoryService
{
    Task<Result<Guid>> CreateAsync(string name, string abbreviation, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(Guid id, string name, string abbreviation, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<MeasurementUnitDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<MeasurementUnitDto>>> GetAllAsync(CancellationToken cancellationToken);
}
