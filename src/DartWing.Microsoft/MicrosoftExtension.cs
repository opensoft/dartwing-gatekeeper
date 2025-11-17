using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DartWing.Microsoft;

public static class MicrosoftExtension
{
    public static IServiceCollection AddMicrosoft(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddMemoryCache();

        var settings = new MicrosoftSettings();
        configuration.Bind("Microsoft", settings);
        services.AddSingleton(settings);

        var serviceSettings = new MicrosoftServiceSettings();
        configuration.Bind("MicrosoftService", serviceSettings);
        services.AddSingleton(serviceSettings);

        services.AddHttpClient<GraphApiHelper>();
        return services;
    }
}