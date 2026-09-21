using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public record DeleteIngredientCategoryCommand(Guid Id) : IRequest<Result>;
