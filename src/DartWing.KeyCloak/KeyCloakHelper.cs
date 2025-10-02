using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.KeyCloak.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DartWing.KeyCloak;

public sealed class AuthServerSecurityKeysHelper
{
    private readonly ILogger<AuthServerSecurityKeysHelper> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeyCloakSettings _settings;

    public AuthServerSecurityKeysHelper(ILogger<AuthServerSecurityKeysHelper> logger, IMemoryCache memoryCache, IHttpClientFactory httpClientFactory, KeyCloakSettings settings)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async ValueTask<IList<SecurityKey>> GetSecurityKeys(CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue("KeyCloak:GetSigningKeys", out var obj) && obj != null)
            return (IList<SecurityKey>)obj;

        var sw = Stopwatch.GetTimestamp();
        var client = _httpClientFactory.CreateClient("KeyCloakKeysClient");
        var url = _settings.GetSigningKeysUrl();
        var response = await client.GetStringAsync(url, ct).ConfigureAwait(false);
        var keys = new JsonWebKeySet(response).GetSigningKeys();
        _memoryCache.Set("KeyCloak:GetSigningKeys", keys, TimeSpan.FromHours(4));
        
        _logger.LogInformation("Get keycloak signing keys {k} qty={qt} {el}", url, keys.Count, Stopwatch.GetElapsedTime(sw));

        return keys;
    }
}

public sealed class KeyCloakHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
        {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

    private readonly ILogger<KeyCloakHelper> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly KeyCloakSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _lock = new(1);

    public KeyCloakHelper(ILogger<KeyCloakHelper> logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, KeyCloakSettings settings)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> CreateOrganization(string company, string fullName, CancellationToken ct)
    {
        var url =_settings.GetOrganizationUrl();
        var accessToken = await GetAccessToken(ct);
        
        var org = new OrganizationRepresentation
        {
            alias = company,
            name = fullName,
            domains = [new OrganizationDomainRepresentation {name = company + ".com"}],
            enabled = true
        };
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var response = await Send(client, HttpMethod.Post, url, accessToken, org, ct);
        return response.Item2;
    }

    public async Task<bool> AddUserToOrganization(string companyGuid, string userId, CancellationToken ct)
    {
        var membersUrl = _settings.GetOrganizationMembersUrl(companyGuid);
        var accessToken = await GetAccessToken(ct);
        //fix ofter update keycloak
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, membersUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = new StringContent(userId)
            { Headers = { ContentType = MediaTypeHeaderValue.Parse("application/json") } };
        
        var client = _httpClientFactory.CreateClient("KeyCloak");
        using var response = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public Task<bool> DeleteUser(string userId, CancellationToken ct)
    {
        return Task.FromResult(true);
        // var accessToken = await GetAccessToken(ct);
        //
        // var url = _settings.GetUserByIdUrl(userId);
        // return await Send<bool>(url, accessToken, true, ct);
    }

    public async Task<RoleRepresentation?> GetRoleRepresentationForUser(string userId, CancellationToken ct)
    {
        var accessToken = await GetAccessToken(ct);
        var url = _settings.GetUserRolesByUserIdUrl(userId);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var roleRepresentation = await Send<RoleRepresentation>(client, url, accessToken, false, ct);
        return roleRepresentation; //?.ClientMappings.Clients[_settings.ClientId].Mappings.Select(x => x.Name).ToArray() ?? [];
    }
    
    public async Task<string[]> GetRolesForUser(string userId, CancellationToken ct)
    {
        var representation = await GetRoleRepresentationForUser(userId, ct);
        
        ClientMapping? clientMapping = null;
        representation?.ClientMappings?.TryGetValue(_settings.ClientId, out clientMapping);
        var existRoles = clientMapping?.Mappings.Select(x => x.Name).ToArray() ?? [];
        return existRoles;
    }
    
    public async Task<bool> AddRoles(string userId, string[] roles, CancellationToken ct)
    {
        var existRolesRepresentation = await GetRoleRepresentationForUser(userId, ct);
        ClientMapping? clientMapping = null;
        existRolesRepresentation?.ClientMappings?.TryGetValue(_settings.ClientId, out clientMapping);
        var existRoles = clientMapping?.Mappings.Select(x => x.Name).ToArray() ?? [];
        if (existRoles.Length >= roles.Length && existRoles.Intersect(roles).Count() == roles.Length) return true;
        
        var accessToken = await GetAccessToken(ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");
        var newRoles = roles.Except(existRoles).ToArray();
        var availableRolesurl = _settings.GetAvailableRolesUrl(userId);
        var availableRoles = await Send<GetRoleRepresentation[]>(client, availableRolesurl, accessToken, false, ct);

        var rolesRepresentations = new List<AddRoleRepresentation>(newRoles.Length);
        foreach (var role in newRoles)
        {
            rolesRepresentations.Add(new AddRoleRepresentation
            {
                name = role,
                id = availableRoles.First(x => x.client == _settings.ClientId && x.role == role).id
            });
        }
        
        
        var url = _settings.GetUserRoleMappingUrl(userId);
        var result = await Send(client, HttpMethod.Post, url, accessToken, rolesRepresentations, ct);
        
        return result.Item1;
    }
    
    public async Task<bool> RemovePermissions(string userId, string company, string[] permissions, CancellationToken ct)
    {
        return false;
        /*var accessToken = await GetAccessToken(ct);
        var url = _settings.GetUserPermissionsByIdUrl(userId);

        var pem = new Auth0Permissions
        {
            Permissions = permissions.Select(x => new Auth0Permission
            {
                PermissionName = $"{x}:{company}",
                ResourceServerIdentifier = _settings.Audience
            }).ToArray()
        };
        
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(pem, SerializerOptions))
            {Headers = {ContentType = new MediaTypeHeaderValue("application/json")}};
        
        var client = _httpClientFactory.CreateClient("Auth0Client");
        
        using var responseMessage = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        return responseMessage.IsSuccessStatusCode;*/
    }
    
    public async Task<UserResponse?> GetUserById(string userId, CancellationToken ct)
    {
        var userIdKey = "KeyCloakUser:" + userId;
        if (_memoryCache.TryGetValue(userIdKey, out UserResponse? userResponse) && userResponse != null)
            return userResponse;
        
        var sw = Stopwatch.GetTimestamp();
        var accessToken = await GetAccessToken(ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");

        var url = _settings.GetUserByIdUrl(userId);

        var user = await Send<UserResponse>(client, url, accessToken, false, ct);
        
        _logger.LogInformation("KeyCloak get user by id {id} {u} {sw}", userId, user?.Email, Stopwatch.GetElapsedTime(sw));
        
        if (user?.Id != null) _memoryCache.Set(userIdKey, user, TimeSpan.FromMinutes(1));
    
        return user;
    }
    
    public async Task<bool> UpdateUserCrmId(string userId, string crmId, CancellationToken ct)
    {
        var accessToken = await GetAccessToken(ct);
        var client = _httpClientFactory.CreateClient("KeyCloak");

        var url = _settings.GetUserByIdUrl(userId);
        
        var user = await Send<UserResponse>(client, url, accessToken, false, ct);
        var userUpdateData = new
        {
            user.Email,
            user.FirstName,
            user.LastName,
            attributes = new
            {
                crmID = new[] {crmId}
            }
        };
        var resp = await Send(client, HttpMethod.Put, url, accessToken, userUpdateData, ct);
        var userIdKey = "KeyCloakUser:" + userId;
        _memoryCache.Remove(userIdKey);
        
        return resp.Item1;
    }

    public string BuildProviderRedirectUrl(string provider)
    {
        return _settings.GetProviderAuthUrl(provider);
    }

    public async ValueTask<string> GetProviderToken(string email, string identityProvider, CancellationToken ct)
    {
        if (_memoryCache.TryGetValue($"KeyCloak:ApiToken:{identityProvider}:{email}", out var token) && token is not null)
            return (string)token;
        
        var sw = Stopwatch.GetTimestamp();
        var url = _settings.GetTokenUrl();

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:token-exchange" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret },
            {"requested_token_type ", "urn:ietf:params:oauth:token-type:access_token"},
            {"requested_subject", email},
            {"requested_issuer", identityProvider}
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new FormUrlEncodedContent(tokenRequest);

        var client = _httpClientFactory.CreateClient("KeyCloak");

        using var responseMessage = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode) return "";

        var body = await responseMessage.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var tokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(body)!;

        if (tokenResponse.ExpiresIn > 100)
            _memoryCache.Set($"KeyCloak:ApiToken:{identityProvider}:{email}", tokenResponse.AccessToken,
                TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60));

        _logger.LogInformation(
            "KeyCloak get {pr} {type} token for client={cl} user={usr} expIn={ex}sec {sw}",
            identityProvider, tokenResponse.TokenType, _settings.ClientId, email, tokenResponse.ExpiresIn,
            Stopwatch.GetElapsedTime(sw));

        return tokenResponse.AccessToken;
    }

    private async ValueTask<string> GetAccessToken(CancellationToken ct)
    {
        if (_memoryCache.TryGetValue("KeyCloak:ApiToken", out var token) && token is not null)
            return (string)token;

        var sw = Stopwatch.GetTimestamp();
        var url = _settings.GetTokenUrl();

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new FormUrlEncodedContent(tokenRequest);

        var client = _httpClientFactory.CreateClient("KeyCloak");

        using var responseMessage = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();

        var body = await responseMessage.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var tokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(body)!;

        if (tokenResponse.ExpiresIn > 200)
            _memoryCache.Set("KeyCloak:ApiToken", tokenResponse.AccessToken,
                TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 150));

        _logger.LogInformation("KeyCloak get {type} token for client={cl} expIn={ex}sec {sw}", tokenResponse.TokenType,
            _settings.ClientId, tokenResponse.ExpiresIn, Stopwatch.GetElapsedTime(sw));

        return tokenResponse.AccessToken;
    }

    private async Task<T?> Send<T>(HttpClient client, string url, string accessToken, bool delete, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        using var requestMessage = new HttpRequestMessage(delete ? HttpMethod.Delete : HttpMethod.Get, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        using var response = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var resp = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("KeyCloak {r} {url} body={body} {sw}", requestMessage.Method.Method, url,
                resp, Stopwatch.GetElapsedTime(sw));
            
            return default;
        }
        
        var jsonResponse = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("KeyCloak {r} {url} response={body} {sw}", requestMessage.Method.Method, url,
                System.Text.Encoding.UTF8.GetString(jsonResponse), Stopwatch.GetElapsedTime(sw));
        } else if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("KeyCloak {r} {url} {sw}", requestMessage.Method.Method, url,
                Stopwatch.GetElapsedTime(sw));
        }

        if (typeof(T) == typeof(bool)) return (T)(object)response.IsSuccessStatusCode;
        return JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions)!;
    }
    
    private async Task<(bool, string)> Send<T>(HttpClient client, HttpMethod method, string url, string accessToken, T body, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        using var requestMessage = new HttpRequestMessage(method, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var bodyJson = JsonSerializer.SerializeToUtf8Bytes(body, SerializerOptions);
        requestMessage.Content = new ByteArrayContent(bodyJson)
            { Headers = { ContentType = MediaTypeHeaderValue.Parse("application/json") } };
        
        using var response = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var resp = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("KeyCloak {r} {url} response={body} request={req} {sw}", requestMessage.Method.Method, url,
                resp, System.Text.Encoding.UTF8.GetString(bodyJson), Stopwatch.GetElapsedTime(sw));
            
            return (response.IsSuccessStatusCode, resp);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var resp = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogDebug("KeyCloak {r} {url} response={body} request={req} {sw}", requestMessage.Method.Method, url,
                resp, System.Text.Encoding.UTF8.GetString(bodyJson), Stopwatch.GetElapsedTime(sw));
        }
        else if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("KeyCloak {r} {url} {sw}", requestMessage.Method.Method, url,
                Stopwatch.GetElapsedTime(sw));
        }

        if (!response.Headers.TryGetValues("location", out var loc)) return (response.IsSuccessStatusCode, "");
        var location = loc.FirstOrDefault();
        if (!string.IsNullOrEmpty(location) && location.Contains('/'))
        {
            return (true, location[(location.LastIndexOf('/') + 1)..]);
        }

        return (response.IsSuccessStatusCode, "");
    }
}