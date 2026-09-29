using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Queries;

public record GetRecipeDraftQuery(Guid DraftId, Guid UserId) : IRequest<Result<RecipeDraftDto>>;
