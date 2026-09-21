using FluentResults;

namespace Cooking.Application.ReferenceData;

public interface IReferenceDataRepositoryService
{
    Task<Result<Guid>> CreateSourceTypeAsync(string name, CancellationToken cancellationToken);
    Task<Result> UpdateSourceTypeAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<Result> DeleteSourceTypeAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<ReferenceEntityDto>> GetSourceTypeByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<ReferenceEntityDto>>> GetSourceTypesAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateComplexityAsync(string name, CancellationToken cancellationToken);
    Task<Result> UpdateComplexityAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<Result> DeleteComplexityAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<ReferenceEntityDto>> GetComplexityByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<ReferenceEntityDto>>> GetComplexitiesAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateIngredientCategoryAsync(string name, CancellationToken cancellationToken);
    Task<Result> UpdateIngredientCategoryAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<Result> DeleteIngredientCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<ReferenceEntityDto>> GetIngredientCategoryByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<ReferenceEntityDto>>> GetIngredientCategoriesAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateDataSourceAsync(string name, CancellationToken cancellationToken);
    Task<Result> UpdateDataSourceAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<Result> DeleteDataSourceAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<ReferenceEntityDto>> GetDataSourceByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<ReferenceEntityDto>>> GetDataSourcesAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateTagTypeAsync(string name, CancellationToken cancellationToken);
    Task<Result> UpdateTagTypeAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<Result> DeleteTagTypeAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<ReferenceEntityDto>> GetTagTypeByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<ReferenceEntityDto>>> GetTagTypesAsync(CancellationToken cancellationToken);
}
