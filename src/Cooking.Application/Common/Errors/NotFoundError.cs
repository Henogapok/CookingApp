using FluentResults;

namespace Cooking.Application.Common.Errors;

public class NotFoundError(string message) : Error(message);
