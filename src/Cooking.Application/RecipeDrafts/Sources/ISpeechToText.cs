using FluentResults;

namespace Cooking.Application.RecipeDrafts.Sources;

/// <summary>
/// Расшифровка речи из аудио- или видеофайла. Ошибки: ErrorCode.Unavailable — сервис не настроен (нет ключа),
/// ExternalService — не ответил. Пустая строка — речи нет (например, только музыка).
/// </summary>
public interface ISpeechToText
{
    Task<Result<string>> TranscribeAsync(string mediaFilePath, CancellationToken cancellationToken);
}
