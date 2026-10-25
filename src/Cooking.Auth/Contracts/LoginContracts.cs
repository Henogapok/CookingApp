namespace Cooking.Auth.Contracts;

/// <summary>Ответ страницы входа «вы уже вошли как …».</summary>
public record LoginSessionResponse(string Name);

public record BotLoginStartResponse(string Link, DateTime ExpiresAt);

/// <summary>ReturnUrl — куда вернуться после входа (обычно /connect/authorize?… от клиента).</summary>
public record BotLoginPollRequest(string? ReturnUrl);

/// <summary>Status: pending | confirmed | declined | expired; Redirect — только при confirmed.</summary>
public record BotLoginPollResponse(string Status, string? Redirect = null);

/// <summary>Username бота для кнопки виджета; null — виджет выключен (локально), показываем только вход через бота.</summary>
public record LoginOptionsResponse(string? WidgetBotUsername);

/// <summary>Data — поля, которые виджет передал странице (id, first_name, …, auth_date, hash), все значения строками.</summary>
public record TelegramWidgetLoginRequest(Dictionary<string, string> Data, string? ReturnUrl);

/// <summary>Куда перейти после входа.</summary>
public record LoginCompletedResponse(string Redirect);
