using Cooking.Api.Options;

namespace Cooking.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Swagger UI (только Development); кнопка Authorize входит через Cooking.Auth с PKCE.</summary>
    public static WebApplication UseSwaggerWithOAuth(this WebApplication app, AuthOptions options)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            ui.OAuthClientId(options.SwaggerClientId);
            ui.OAuthUsePkce();
            ui.OAuthScopes("openid", "profile", options.Audience);
        });

        return app;
    }
}
