using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>Создаёт черновик и ставит его в очередь на разбор. Результат придёт через IRecipeDraftNotifier.</summary>
public record CreateRecipeDraftCommand(Guid UserId, string Text) : IRequest<Result<Guid>>;
