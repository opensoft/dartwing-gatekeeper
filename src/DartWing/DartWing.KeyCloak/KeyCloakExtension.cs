using DartWing.KeyCloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.Web.KeyCloak;

public static class KeyCloakExtension
{
    public static IServiceCollection AddKeyCloak(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddHttpClient("KeyCloak");
        services.AddHttpClient("KeyCloakKeysClient");

        var settings = new KeyCloakSettings();
        configuration.Bind("KeyCloak", settings);
        services.AddSingleton(settings);

        services.AddSingleton<AuthServerSecurityKeysHelper>();
        services.AddMemoryCache();
        services.AddSingleton<KeyCloakHelper>();
        return services;
    }
}