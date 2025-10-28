using System.IdentityModel.Tokens.Jwt;
using Azure.Storage.Blobs;
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

        services.AddHttpClient<GraphApiHelper>();
        services.AddSingleton(x =>
            new BlobServiceClient(configuration.GetConnectionString("AzureStorage")));

        services.AddScoped<AzureStorageAdapter>();
        return services;
    }
    
    internal static (string? email, string? tenantId) GetEmailAndTenantId(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return (null, null);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var tenantId = token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
        var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        return (email, tenantId);
    }
}