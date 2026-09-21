using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Queries;

public record GetIngredientCategoryByIdQuery(Guid Id) : IRequest<Result<ReferenceEntityDto>>;
