using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

/// <summary>Пользователь нажал «Войти» в боте.</summary>
public record ConfirmLoginRequestCommand(string Code, Guid UserId) : IRequest<Result>;
