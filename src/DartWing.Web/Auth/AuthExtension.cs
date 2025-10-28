using DartWing.KeyCloak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DartWing.Web.Auth;

public static class AuthExtension
{
    public static void AddAuthenticationLogic(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddProblemDetails();
        services.AddHttpClient("KeyCloakClient");

        var auth0Settings = new KeyCloakSettings();
        configuration.Bind("KeyCloak", auth0Settings);
        services.AddSingleton(auth0Settings);
        services.AddMemoryCache();

        var auth0Services = new ServiceCollection();
        auth0Services.AddSingleton<AuthServerSecurityKeysHelper>();
        auth0Services.AddSingleton(auth0Settings);
        auth0Services.AddHttpClient("Auth0SecurityKeysClient");
        auth0Services.AddMemoryCache();

#pragma warning disable ASP0000
        var auth0Provider = auth0Services.BuildServiceProvider();
#pragma warning restore ASP0000

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = auth0Settings.GetAuthorityUrl();
                options.Audience = auth0Settings.GetAudienceUrl();
                

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
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
                    return auth0Provider.GetService<AuthServerSecurityKeysHelper>()!.GetSecurityKeys().GetAwaiter()
                        .GetResult();
#pragma warning restore CA2012
                }
            });
        services.AddAuthorization();
    }
}