using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.ErpNext;

public static class ERPNextExtension
{
    public static IServiceCollection AddDartWing(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddHttpClient<ERPNextService>((x, y) =>
        {
            var settings = x.GetService<ERPNextSettings>()!;
            y.DefaultRequestHeaders.Add("Authorization", $"token {settings.ApiKey}:{settings.ApiSecret}");
            y.BaseAddress = new Uri(settings.Url);
        });

        var settings = new ERPNextSettings();
        configuration.Bind("ERPNext", settings);
        services.AddSingleton(settings);

        return services;
    }
}