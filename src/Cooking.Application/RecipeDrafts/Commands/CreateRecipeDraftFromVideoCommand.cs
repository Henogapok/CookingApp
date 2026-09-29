using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// Черновик из видеофайла, который прислал пользователь (когда ссылку скачать не удалось): файл уже лежит локально,
/// расшифровка и разбор — в фоне. Файл удаляется после расшифровки.
/// </summary>
/// <param name="Caption">Описание, если пользователь вставил его подписью к видео.</param>
public record CreateRecipeDraftFromVideoCommand(Guid UserId, string VideoFilePath, string? Caption) : IRequest<Result<Guid>>;
