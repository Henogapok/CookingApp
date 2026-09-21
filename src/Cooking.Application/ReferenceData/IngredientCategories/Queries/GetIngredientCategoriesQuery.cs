using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Queries;

public record GetIngredientCategoriesQuery : IRequest<Result<List<ReferenceEntityDto>>>;
