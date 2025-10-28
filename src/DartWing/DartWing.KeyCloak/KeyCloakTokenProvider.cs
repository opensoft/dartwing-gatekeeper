using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Json;
using DartWing.KeyCloak.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.KeyCloak;

public class KeyCloakTokenProvider
{
    private static readonly EmailAddressAttribute EmailAttribute = new();
    
    private readonly ILogger<KeyCloakTokenProvider> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly KeyCloakSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public KeyCloakTokenProvider(ILogger<KeyCloakTokenProvider> logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, KeyCloakSettings settings)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public string BuildProviderRedirectUrl(string provider)
    {
        return _settings.GetProviderAuthUrl(provider);
    }

    public async ValueTask<AuthTokenResponse> GetClientAccessToken(string? scope = null, bool force = false, CancellationToken ct = default)
    {
        var key = $"KeyCloak:Token:{_settings.Domain}:{_settings.RealmName}:{_settings.ClientId}:scope={scope}";
        if (!force && _memoryCache.TryGetValue(key, out AuthTokenResponse? token) && token is not null)
            return token;

        var sw = Stopwatch.GetTimestamp();
        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret }
        };
        if (!string.IsNullOrEmpty(scope)) tokenRequest.Add("scope", scope);
        var tokenResponse = await GetTokenPrivate(tokenRequest, null, ct);
        if (tokenResponse!.ExpiresIn > 100)
            _memoryCache.Set(key, tokenResponse, TimeSpan.FromSeconds(32));

        _logger.LogInformation("KeyCloak get {type} token for client={cl} expIn={ex}sec {sw}", tokenResponse.TokenType,
            _settings.ClientId, tokenResponse.ExpiresIn, Stopwatch.GetElapsedTime(sw));

        return tokenResponse;
    }

    public ValueTask<AuthTokenResponse> TokenExchangeProviderAccessToken(string accessTokenOrEmail,
        string identityProvider,
        bool force = false, CancellationToken ct = default)
    {
        return TokenExchangePrivate(accessTokenOrEmail, identityProvider, null, force, ct);
    }

    public ValueTask<AuthTokenResponse> TokenExchangeUserAccessToken(string accessTokenOrEmail,
        string scope, bool force = false, CancellationToken ct = default)
    {
        return TokenExchangePrivate(accessTokenOrEmail, null, scope, force, ct);
    }

    private async ValueTask<AuthTokenResponse> TokenExchangePrivate(string accessTokenOrEmail, string? identityProvider,
        string? scope, bool force, CancellationToken ct)
    {
        var isEmail = EmailAttribute.IsValid(accessTokenOrEmail);

        var email = isEmail
            ? accessTokenOrEmail
            : KeyCloakHelpers.GetEmailFromJwtToken(accessTokenOrEmail);
        if (string.IsNullOrEmpty(email)) return new AuthTokenResponse();

        var key = string.IsNullOrEmpty(scope)
            ? $"KeyCloak:Token:{_settings.Domain}:{_settings.RealmName}:{_settings.ClientId}:{email}:{identityProvider}"
            : $"KeyCloak:Token:{_settings.Domain}:{_settings.RealmName}:{scope}:{email}";
        if (!force && _memoryCache.TryGetValue(key, out AuthTokenResponse? token) && token is not null)
            return token;

        var sw = Stopwatch.GetTimestamp();
        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:token-exchange" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret },
            { "requested_token_type ", "urn:ietf:params:oauth:token-type:access_token" },
        };
        
        if (!string.IsNullOrEmpty(identityProvider)) tokenRequest.Add("requested_issuer", identityProvider);
        if (!string.IsNullOrEmpty(scope)) tokenRequest.Add("audience", scope);

        tokenRequest.Add(isEmail ? "requested_subject" : "subject_token", accessTokenOrEmail);

        var tokenResponse = await GetTokenPrivate(tokenRequest, email, ct);
        
        if (tokenResponse!.ExpiresIn > 100 && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            _memoryCache.Set(key, tokenResponse, TimeSpan.FromSeconds(12));

        if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
            _logger.LogInformation("KeyCloak token exchange client={cl} user={usr} target={targ} expIn={ex} {sw}",
                _settings.ClientId, email, identityProvider ?? scope, tokenResponse.ExpiresIn,
                Stopwatch.GetElapsedTime(sw));

        return tokenResponse;
    }
    
    public async Task<AuthTokenResponse?> TokenByCode(string code, string redirectUri, CancellationToken ct = default)
    {
        var sw = Stopwatch.GetTimestamp();
        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret },
            { "code", code },
            { "redirect_uri", redirectUri }
        };
        
        var tokenResponse = await GetTokenPrivate(tokenRequest, null, ct);

        return tokenResponse;
    }

    private async Task<AuthTokenResponse?> GetTokenPrivate(Dictionary<string, string> request, string? email, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = _settings.GetTokenUrl();

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new FormUrlEncodedContent(request);

        var client = _httpClientFactory.CreateClient("KeyCloak");

        using var responseMessage = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        if (responseMessage.IsSuccessStatusCode)
            return await responseMessage.Content.ReadFromJsonAsync<AuthTokenResponse>(ct);
        var response = await responseMessage.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("KeyCloak token exchange error client={cl} {em} response={resp} {sw}",
            _settings.ClientId, email, response, Stopwatch.GetElapsedTime(sw));
        return new AuthTokenResponse();
    }
}