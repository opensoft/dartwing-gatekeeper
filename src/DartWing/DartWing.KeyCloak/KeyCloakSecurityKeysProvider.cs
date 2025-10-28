using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DartWing.KeyCloak;

public sealed class KeyCloakSecurityKeysProvider
{
    private IList<SecurityKey>? _securityKeys;

    private readonly ILogger<KeyCloakSecurityKeysProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeyCloakSettings _settings;

    public KeyCloakSecurityKeysProvider(ILogger<KeyCloakSecurityKeysProvider> logger,
        IHttpClientFactory httpClientFactory, KeyCloakSettings settings)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async ValueTask<IList<SecurityKey>> GetSecurityKeys(CancellationToken ct = default)
    {
        if (_securityKeys != null)
            return _securityKeys;

        var sw = Stopwatch.GetTimestamp();
        var url = _settings.GetSigningKeysUrl();
        var response = await _httpClientFactory.CreateClient().GetStringAsync(url, ct).ConfigureAwait(false);
        var keys = new JsonWebKeySet(response).GetSigningKeys();
        _securityKeys = keys;

        _logger.LogInformation("Get keycloak signing keys {k} qty={qt} {el}", url, keys.Count,
            Stopwatch.GetElapsedTime(sw));

        return _securityKeys;
    }

    public async ValueTask<(ClaimsPrincipal? claims, string? errorMessage)> CheckToken(string token, string? issuer,
        string? audience, bool validateLifetime = true, CancellationToken ct = default)
    {
        var keys = await GetSecurityKeys(ct).ConfigureAwait(false);
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys,
            ValidateIssuer = !string.IsNullOrEmpty(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrEmpty(audience),
            ValidAudience = audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        try
        {
            return (tokenHandler.ValidateToken(token, validationParameters, out _), null);
        }
        catch (Exception ex)
        {
            return (new ClaimsPrincipal(), ex.Message);
        }
    }
}