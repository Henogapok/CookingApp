using Cooking.Application.Common.Behaviors;
using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

/// <summary>Сайт опрашивает запрос на вход по секрету браузера; подтверждённый отдаёт пользователя один раз.
/// Браузер опрашивает её раз в пару секунд, пока ждёт подтверждения в боте.</summary>
public record CompleteLoginRequestCommand(string PollToken) : IRequest<Result<LoginRequestStatusDto>>, IPolledRequest;
