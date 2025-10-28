using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.Frappe;

public static class FrappeExtension
{
    public static IServiceCollection AddFrappe(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IFrappeService, FrappeService>();
        
        return services;
    }
}