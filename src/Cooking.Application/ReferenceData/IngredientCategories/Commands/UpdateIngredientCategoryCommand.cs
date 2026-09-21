using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public record UpdateIngredientCategoryCommand(Guid Id, string Name) : IRequest<Result>;
