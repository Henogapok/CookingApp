namespace Cooking.Auth.Contracts;

/// <summary>Ответ страницы входа «вы уже вошли как …».</summary>
public record LoginSessionResponse(string Name);

public record BotLoginStartResponse(string Link, DateTime ExpiresAt);

/// <summary>ReturnUrl — куда вернуться после входа (обычно /connect/authorize?… от клиента).</summary>
public record BotLoginPollRequest(string? ReturnUrl);

/// <summary>Status: pending | confirmed | declined | expired; Redirect — только при confirmed.</summary>
public record BotLoginPollResponse(string Status, string? Redirect = null);
