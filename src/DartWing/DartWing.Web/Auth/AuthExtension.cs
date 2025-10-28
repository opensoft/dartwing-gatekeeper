using System.IdentityModel.Tokens.Jwt;
using DartWing.KeyCloak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace DartWing.Web.Auth;

public static class AuthExtension
{
    public static void AddAuthenticationLogic(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddProblemDetails();
        services.AddHttpClient("KeyCloakClient");

        var keyCloakSettings = new KeyCloakSettings();
        configuration.Bind("KeyCloak", keyCloakSettings);
        services.AddSingleton(keyCloakSettings);
        services.AddMemoryCache();

        var collection = new ServiceCollection();
        collection.AddSingleton<KeyCloakSecurityKeysProvider>();
        collection.AddSingleton(keyCloakSettings);
        collection.AddHttpClient("Auth0SecurityKeysClient");
        collection.AddMemoryCache();

#pragma warning disable ASP0000
        var auth0Provider = collection.BuildServiceProvider();
#pragma warning restore ASP0000

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = keyCloakSettings.GetAuthorityUrl();
                options.Audience = keyCloakSettings.GetAudience();
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
#if DEBUG
                    ValidateLifetime = false,
#endif
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = ["RS256"],
                    IssuerSigningKeyResolver = IssuerSigningKeyResolver,
                    //NameClaimType = "name",
                    //RoleClaimType = "permission",
                };

                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenInvalidSignatureException)
                        {
                            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>()!;
                            if (logger.IsEnabled(LogLevel.Debug))
                            {
                                logger.LogDebug(context.Exception.Message);
                            }
                        }

                        return Task.CompletedTask;
                    }
                };

                IList<SecurityKey> IssuerSigningKeyResolver(string token, SecurityToken securitytoken, string kid,
                    TokenValidationParameters validationparameters)
                {
#pragma warning disable CA2012
                    return auth0Provider.GetService<KeyCloakSecurityKeysProvider>()!.GetSecurityKeys().GetAwaiter()
                        .GetResult();
#pragma warning restore CA2012
                }
            });
        
        services.AddScoped<IAuthorizationHandler, AuthPolicyHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.AdminPolicy, policy => policy.Requirements.Add(new AuthPolicyRequirement(true)))
            .AddPolicy(AuthConstants.UserPolicy, policy => policy.Requirements.Add(new AuthPolicyRequirement(false)));
    }

    public static string? GetTenantId(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return null;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        return token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
    }
}