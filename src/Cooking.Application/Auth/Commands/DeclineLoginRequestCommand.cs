using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

/// <summary>Пользователь нажал «Отмена» в боте: вход начинал не он.</summary>
public record DeclineLoginRequestCommand(string Code, Guid UserId) : IRequest<Result>;
