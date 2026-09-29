namespace Cooking.Application.Common.Errors;

public enum ErrorCode
{
    Validation,
    NotFound,
    LogicConflict,
    Forbidden,

    /// <summary>Внешний сервис (LLM) не настроен — функция недоступна.</summary>
    Unavailable,

    /// <summary>Внешний сервис (LLM) ответил ошибкой или не ответил.</summary>
    ExternalService
}
