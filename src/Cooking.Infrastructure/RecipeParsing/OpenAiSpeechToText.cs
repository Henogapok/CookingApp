using System.Net.Http.Headers;
using System.Text.Json;
using Cooking.Application.Common.Errors;
using Cooking.Application.RecipeDrafts.Sources;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cooking.Infrastructure.RecipeParsing;

/// <summary>Настройки OpenAI. Ключ — только в User Secrets / переменных окружения (OpenAI__ApiKey).</summary>
public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }

    public string TranscriptionModel { get; set; } = "whisper-1";
}

/// <summary>
/// Расшифровка через OpenAI Audio API (Whisper). Язык не задаём: модель определяет его сама —
/// так англоязычные Reels распознаются не хуже русских.
/// </summary>
public class OpenAiSpeechToText(HttpClient http, IOptions<OpenAiOptions> options, ILogger<OpenAiSpeechToText> logger) : ISpeechToText
{
    public async Task<Result<string>> TranscribeAsync(string mediaFilePath, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return Result.Fail(new AppError("Speech-to-text is not configured: OpenAI:ApiKey is empty.", ErrorCode.Unavailable));

        try
        {
            await using var file = File.OpenRead(mediaFilePath);
            var fileContent = new StreamContent(file);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var form = new MultipartFormDataContent
            {
                { new StringContent(settings.TranscriptionModel), "model" },
                { new StringContent("json"), "response_format" },
                // Формат аудио сервис определяет по расширению имени файла (m4a, mp4, …).
                { fileContent, "file", Path.GetFileName(mediaFilePath) },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/transcriptions") { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var response = await http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Transcription failed: HTTP {Status} {Body}", (int)response.StatusCode, body);
                return Result.Fail(new AppError($"Transcription failed with HTTP {(int)response.StatusCode}.", ErrorCode.ExternalService));
            }

            using var json = JsonDocument.Parse(body);
            var text = json.RootElement.TryGetProperty("text", out var value) ? value.GetString() : null;

            logger.LogInformation("Transcribed {File}: {Length} chars", Path.GetFileName(mediaFilePath), text?.Length ?? 0);
            return Result.Ok(text?.Trim() ?? "");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException
                                   || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Transcription request failed");
            return Result.Fail(new AppError($"Transcription request failed: {ex.Message}", ErrorCode.ExternalService));
        }
    }
}
