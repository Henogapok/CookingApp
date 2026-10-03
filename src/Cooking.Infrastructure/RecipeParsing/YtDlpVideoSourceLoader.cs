using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Cooking.Application.Common.Errors;
using Cooking.Application.RecipeDrafts.Sources;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cooking.Infrastructure.RecipeParsing;

public class YtDlpOptions
{
    public const string SectionName = "YtDlp";

    /// <summary>Исполняемый файл yt-dlp: по умолчанию ищется в PATH; в Docker — например, /usr/local/bin/yt-dlp.</summary>
    public string Path { get; set; } = "yt-dlp";

    /// <summary>Файл cookies (формат Netscape) отдельного аккаунта — если Instagram начнёт требовать вход (VPS).</summary>
    public string? CookiesFile { get; set; }

    public int TimeoutSeconds { get; set; } = 90;
}

/// <summary>
/// Скачивает описание и звуковую дорожку Reels через yt-dlp (отдельный процесс). Singleton: скачиваем по одному —
/// Instagram болезненно относится к частым запросам с одного адреса, а очередь разбора всё равно параллельна.
/// </summary>
public class YtDlpVideoSourceLoader(IOptions<YtDlpOptions> options, ILogger<YtDlpVideoSourceLoader> logger) : IVideoSourceLoader
{
    private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "cooking-media");

    private readonly SemaphoreSlim _downloadGate = new(1, 1);

    public async Task<Result<VideoSource>> LoadAsync(string url, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        Directory.CreateDirectory(TempDirectory);
        var fileName = Guid.NewGuid().ToString("N");

        // Один вызов: -j печатает метаданные (там описание), --no-simulate при этом всё равно скачивает.
        // "ba/b" — только звук, а если отдельной дорожки нет — обычный файл (Whisper примет и видео).
        var arguments = new List<string>
        {
            "-f", "ba/b", "-j", "--no-simulate", "--no-playlist", "--no-warnings", "--encoding", "utf-8",
            "-o", Path.Combine(TempDirectory, fileName + ".%(ext)s"),
        };
        if (!string.IsNullOrWhiteSpace(settings.CookiesFile))
            arguments.AddRange(["--cookies", settings.CookiesFile]);
        arguments.Add(url);

        await _downloadGate.WaitAsync(cancellationToken);
        try
        {
            var started = Stopwatch.GetTimestamp();
            var run = await RunAsync(settings, arguments, cancellationToken);
            if (run.IsFailed)
                return run.ToResult<VideoSource>();

            var (exitCode, stdout, stderr) = run.Value;
            if (exitCode != 0)
            {
                logger.LogWarning("yt-dlp failed for {Url} (exit {ExitCode}): {Error}", url, exitCode, stderr.Trim());
                return Result.Fail(new AppError($"yt-dlp could not load the video: {stderr.Trim()}", ClassifyError(stderr)));
            }

            var audioFile = Directory.GetFiles(TempDirectory, fileName + ".*").FirstOrDefault();
            var description = ReadDescription(stdout);

            logger.LogInformation(
                "yt-dlp loaded {Url} in {ElapsedMs} ms: description {DescriptionLength} chars, audio {AudioFile} ({AudioKb} KB)",
                url, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds, description?.Length ?? 0,
                audioFile is null ? "none" : Path.GetFileName(audioFile),
                audioFile is null ? 0 : new FileInfo(audioFile).Length / 1024);

            return Result.Ok(new VideoSource(description, audioFile));
        }
        finally
        {
            _downloadGate.Release();
        }
    }

    /// <summary>
    /// Нет видео / закрыто — NotFound; остальное (просит вход, лимит запросов, сбой) — ExternalService.
    /// Для пользователя разница невелика (в обоих случаях — «пришли файлом»), но в логах полезна.
    /// </summary>
    public static ErrorCode ClassifyError(string stderr)
    {
        bool Has(string text) => stderr.Contains(text, StringComparison.OrdinalIgnoreCase);

        // Сначала признаки блокировки: у Instagram «not available» часто идёт вместе с «rate-limit … or login required».
        if (Has("login") || Has("rate-limit") || Has("rate limit") || Has("HTTP Error 429"))
            return ErrorCode.ExternalService;

        return Has("private") || Has("not available") || Has("does not exist") || Has("HTTP Error 404")
            ? ErrorCode.NotFound
            : ErrorCode.ExternalService;
    }

    /// <summary>Описание из JSON-метаданных (первая строка вывода -j); null — нет или не разобрать.</summary>
    public static string? ReadDescription(string stdout)
    {
        var json = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => line.StartsWith('{'));
        if (json is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String
                ? description.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<Result<(int ExitCode, string Stdout, string Stderr)>> RunAsync(
        YtDlpOptions settings, List<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(settings.Path)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // ArgumentList, а не строка: ссылка от пользователя не может «сломать» командную строку.
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            logger.LogError(ex, "yt-dlp is not installed or not found at '{Path}'", settings.Path);
            return Result.Fail(new AppError($"yt-dlp was not found at '{settings.Path}'.", ErrorCode.Unavailable));
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            return Result.Ok((process.ExitCode, await stdout, await stderr));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            return Result.Fail(new AppError($"yt-dlp timed out after {settings.TimeoutSeconds}s.", ErrorCode.ExternalService));
        }
    }
}
