using FluentResults;

namespace Cooking.Application.Common.Errors;

public class AppError(string message, ErrorCode code) : Error(message)
{
    public ErrorCode Code { get; } = code;
}
