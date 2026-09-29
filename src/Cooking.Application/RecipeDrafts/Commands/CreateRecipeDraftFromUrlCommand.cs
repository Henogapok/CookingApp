using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// Черновик из ссылки на Reels: скачивание, расшифровка и разбор идут в фоне (обычная очередь разбора).
/// Не удалось скачать — IRecipeDraftNotifier сообщит VideoUnavailable, и клиент может попросить прислать само видео.
/// </summary>
public record CreateRecipeDraftFromUrlCommand(Guid UserId, string Url) : IRequest<Result<Guid>>;
