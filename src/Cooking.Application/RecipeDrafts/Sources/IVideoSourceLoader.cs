using FluentResults;

namespace Cooking.Application.RecipeDrafts.Sources;

/// <summary>
/// Достаёт из ссылки на видео (Instagram Reels) описание и звук. Ошибки: ErrorCode.Unavailable — загрузчик
/// не установлен; NotFound — видео нет или оно закрыто; ExternalService — площадка не отдала (блокировка, сбой).
/// </summary>
public interface IVideoSourceLoader
{
    Task<Result<VideoSource>> LoadAsync(string url, CancellationToken cancellationToken);
}

/// <summary>
/// Описание и путь к временному аудиофайлу. Файл удаляется при Dispose — источник живёт, пока идёт расшифровка.
/// </summary>
public sealed record VideoSource(string? Caption, string? AudioFilePath) : IDisposable
{
    public void Dispose()
    {
        if (AudioFilePath is null)
            return;

        try
        {
            File.Delete(AudioFilePath);
        }
        catch (IOException)
        {
            // Временный файл — не страшно, если не удалился сразу.
        }
    }
}
