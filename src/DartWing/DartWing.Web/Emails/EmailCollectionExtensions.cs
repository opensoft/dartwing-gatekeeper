namespace DartWing.Web.Emails;

public static class EmailCollectionExtensions
{
    public static IServiceCollection AddEmailSending(this IServiceCollection services, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(apiKey);
        
        services.AddHttpClient<LoopsAdapter>(client =>
        {
            client.BaseAddress = new Uri("https://app.loops.so/api/v1/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        });
        
        return services;
    }
}