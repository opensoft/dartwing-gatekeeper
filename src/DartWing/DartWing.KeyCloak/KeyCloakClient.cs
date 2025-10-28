using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.KeyCloak;

public sealed class KeyCloakClient
{
    private readonly ILogger<KeyCloakClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeyCloakTokenProvider _tokenProvider;
    private readonly IMemoryCache _memoryCache;

    public KeyCloakClient(ILogger<KeyCloakClient> logger, IHttpClientFactory httpClientFactory,
        KeyCloakTokenProvider tokenProvider, IMemoryCache memoryCache)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _memoryCache = memoryCache;
    }

    public async Task<(bool success, string location)> Post<T>(string url, T entity, string[]? cacheKeys,
        CancellationToken ct) where T : class
    {
        var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var response = await client.Post(url, accessToken.AccessToken, entity, _logger, ct);
        if (!response.success) return response;
        _memoryCache.Remove(url);
        foreach (var key in cacheKeys ?? []) _memoryCache.Remove(key);
        return response;
    }

    public async Task<bool> Put<T>(string url, T entity, string[]? cacheKeys, CancellationToken ct) where T : class
    {
        var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var response = await client.Put(url, accessToken.AccessToken, entity, _logger, ct);
        if (response) _memoryCache.Remove(url);
        if (!response || cacheKeys == null) return response;
        foreach (var key in cacheKeys) _memoryCache.Remove(key);
        return response;
    }

    public async ValueTask<T?> Get<T>(string url, CancellationToken ct, bool force = false) where T : class
    {
        if (!force && _memoryCache.TryGetValue(url, out T? entity) && entity != null) return entity;
        var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var response = await client.Get<T>(url, accessToken.AccessToken, _logger, ct);
        if (response != null) _memoryCache.Set(url, response, TimeSpan.FromSeconds(12));
        return response;
    }

    public async ValueTask<IReadOnlyList<T>> GetAll<T>(string url, CancellationToken ct) where T : class
    {
        if (_memoryCache.TryGetValue(url, out IReadOnlyList<T>? all) && all != null) return all;
        var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var response = await client.GetAll<T>(url, accessToken.AccessToken, _logger, ct);
        if (response.Count > 0) _memoryCache.Set(url, response, TimeSpan.FromSeconds(24));
        return response;
    }

    public async Task<bool> Delete(string url, string[]? cacheKeys, CancellationToken ct)
    {
        var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var response = await client.Delete(url, accessToken.AccessToken, _logger, ct);
        if (!response) return response;
        _memoryCache.Remove(url);
        foreach (var key in cacheKeys ?? []) _memoryCache.Remove(key);
        return response;
    }
}