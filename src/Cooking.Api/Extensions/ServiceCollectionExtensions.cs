using Cooking.Api.Authorization;
using Cooking.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;

namespace Cooking.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Токены выдаёт Cooking.Auth (OpenID Connect). Api только проверяет JWT: подпись — по ключам из
    /// {Authority}/.well-known/jwks (скачиваются и кэшируются сами), aud = cooking-api, срок действия.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, AuthOptions options)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                // Оставляем claim'ы как в токене ("sub", "name"), без переименования в длинные URI из WS-Federation.
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters.NameClaimType = "name";
                // Роли — claim "role" (его кладёт Cooking.Auth); без этого RequireRole искал бы длинный URI Microsoft и не находил.
                jwt.TokenValidationParameters.RoleClaimType = "role";
            });

        services.AddAuthorization(Policies.Add);
        return services;
    }

    /// <summary>Swagger с кнопкой Authorize: обычный вход через Cooking.Auth (Authorization Code + PKCE), как у PWA.</summary>
    public static IServiceCollection AddSwaggerWithOAuth(this IServiceCollection services, AuthOptions options)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(swagger =>
        {
            if (options.Authority is null)
                return;

            var authority = new Uri(options.Authority);
            swagger.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(authority, "connect/authorize"),
                        TokenUrl = new Uri(authority, "connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "OpenID Connect",
                            ["profile"] = "Имя пользователя",
                            [options.Audience] = "Cooking API",
                        },
                    },
                },
            });
            swagger.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("oauth2", document)] = ["openid", "profile", options.Audience],
            });
        });

        return services;
    }
}
