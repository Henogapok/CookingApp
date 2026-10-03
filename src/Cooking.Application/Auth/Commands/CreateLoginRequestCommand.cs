using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

/// <summary>Сайт начинает вход через бота.</summary>
public record CreateLoginRequestCommand : IRequest<Result<LoginRequestCreatedDto>>;
