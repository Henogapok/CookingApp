using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public record CreateIngredientCategoryCommand(string Name) : IRequest<Result<Guid>>;
