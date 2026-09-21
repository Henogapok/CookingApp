using FluentResults;

namespace Cooking.Application.Common.Errors;

public class ValidationError(string message) : Error(message);
