using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Microsoft;

public sealed class GraphApiHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    private readonly ILogger<GraphApiHelper> _logger;
    private readonly HttpClient _client;
    private readonly MicrosoftSettings _settings;
    private readonly IMemoryCache _memoryCache;

    public GraphApiHelper(ILogger<GraphApiHelper> logger, HttpClient client, MicrosoftSettings settings, IMemoryCache memoryCache)
    {
        _logger = logger;
        _client = client;
        _settings = settings;
        _memoryCache = memoryCache;
    }

    public static string? GetTenantIdFromAccessToken(string accessToken)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        return jwtToken.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
    }

    public ValueTask<string> GetClientAccessTokenFromUserToken(string userToken, CancellationToken ct)
    {
        var tenantId = GetTenantIdFromAccessToken(userToken);
        if (string.IsNullOrEmpty(tenantId)) return ValueTask.FromResult("");
        
        return GetClientAccessToken(tenantId, ct);
    }

    public async ValueTask<string> GetClientAccessToken(string tenantId, CancellationToken ct)
    {
        if (_memoryCache.TryGetValue($"Microsoft:GraphApi:Client:AccessToken:{tenantId}", out var token) && token is not null)
            return (string)token;
        
        var sw = Stopwatch.GetTimestamp();
        var body = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
        ]);
        
        var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        using var response = await _client.PostAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var resp = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Microsoft: Failed to get client access token for tenant={t} body={r} {el}", tenantId,
                resp, Stopwatch.GetElapsedTime(sw));
            return "";
        }
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(SerializerOptions, ct);

        if (tokenResponse?.AccessToken == null)
        {
            _logger.LogError("Microsoft: Failed to get client access token for tenant={t} read from json {el}",
                tenantId, Stopwatch.GetElapsedTime(sw));
            return "";
        }
        
        if (tokenResponse.ExpiresIn > 200)
            _memoryCache.Set($"Microsoft:GraphApi:Client:AccessToken:{tenantId}", tokenResponse.AccessToken,
                TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 150));

        return tokenResponse.AccessToken;
    }
}