using DartWing.Frappe.Erp;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.Frappe;

public interface IFrappeService
{
    TService GetService<TService>(string baseUrl, string token) where TService : IFrappeBaseService;
}

public sealed class FrappeService : IFrappeService
{
    private readonly IServiceProvider _provider;
    private readonly IHttpClientFactory _clientFactory;

    public FrappeService(IServiceProvider provider)
    {
        _provider = provider;
        _clientFactory = provider.GetService<IHttpClientFactory>()!;
    }

    public TService GetService<TService>(string baseUrl, string token) where TService : IFrappeBaseService
    {
        var client = _clientFactory.CreateClient("frappe");
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Authorization", token);
        return ActivatorUtilities.CreateInstance<TService>(_provider, client);
    }
}