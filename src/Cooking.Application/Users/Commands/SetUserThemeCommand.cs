using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Commands;

/// <summary>Пользователь выбрал тему в PWA — запоминаем, чтобы на других устройствах была та же.</summary>
public record SetUserThemeCommand(Guid UserId, string Theme) : IRequest<Result>;
