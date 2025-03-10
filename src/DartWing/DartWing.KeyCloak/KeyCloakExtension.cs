using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.Web.KeyCloak;

public static class KeyCloakExtension
{
    public static void AddKeyCloak(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddHttpClient("KeyCloak");

        var auth0Settings = new KeyCloakSettings();
        configuration.Bind("KeyCloak", auth0Settings);
        services.AddSingleton(auth0Settings);

        var auth0Services = new ServiceCollection();
        auth0Services.AddSingleton<AuthServerSecurityKeysHelper>();
        auth0Services.AddSingleton(auth0Settings);
        auth0Services.AddHttpClient("KeyCloakKeysClient");
        auth0Services.AddMemoryCache();
    }
}