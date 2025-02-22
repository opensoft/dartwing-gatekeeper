using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.Web.KeyCloak.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace DartWing.Web.KeyCloak;

internal sealed class AuthServerSecurityKeysHelper
{
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeyCloakSettings _settings;

    public AuthServerSecurityKeysHelper(IMemoryCache memoryCache, IHttpClientFactory httpClientFactory, KeyCloakSettings settings)
    {
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async ValueTask<IList<SecurityKey>> GetSecurityKeys(CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue("Auth0:GetSigningKeys", out var obj) && obj != null)
            return (IList<SecurityKey>)obj;

        var client = _httpClientFactory.CreateClient("Auth0SecurityKeysClient");
        var response = await client.GetStringAsync(_settings.GetSigningKeysUrl(), ct).ConfigureAwait(false);
        var keys = new JsonWebKeySet(response).GetSigningKeys();
        _memoryCache.Set("Auth0:GetSigningKeys", keys, TimeSpan.FromHours(4));

        return keys;
    }
}

internal sealed class KeyCloakHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
        {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull};

    private readonly IMemoryCache _memoryCache;
    private readonly KeyCloakSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _lock = new(1);

    public KeyCloakHelper(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, KeyCloakSettings settings)
    {
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
        var client = _httpClientFactory.CreateClient("Auth0Client");
        var response = await Post(client, url, accessToken, org, ct);
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
        
        var client = _httpClientFactory.CreateClient("Auth0Client");
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

    public async Task<RoleRepresentation> GetRoleRepresentationForUser(string userId, CancellationToken ct)
    {
        var accessToken = await GetAccessToken(ct);
        var url = _settings.GetUserRolesByUserIdUrl(userId);
        var client = _httpClientFactory.CreateClient("Auth0Client");
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

    /*public async Task<Auth0Permission[]> GetPermissions(string userId, CancellationToken ct)
    {
        var accessToken = await GetAccessToken(ct);
        var url = _settings.GetUserPermissionsByIdUrl(userId);
        var client = _httpClientFactory.CreateClient("Auth0Client");
        return await Send<Auth0Permission[]>(url, accessToken, false, ct).ConfigureAwait(false);
    }*/
    
    public async Task<bool> AddRoles(string userId, string[] roles, CancellationToken ct)
    {
        var existRolesRepresentation = await GetRoleRepresentationForUser(userId, ct);
        ClientMapping? clientMapping = null;
        existRolesRepresentation?.ClientMappings?.TryGetValue(_settings.ClientId, out clientMapping);
        var existRoles = clientMapping?.Mappings.Select(x => x.Name).ToArray() ?? [];
        if (existRoles.Length >= roles.Length && existRoles.Intersect(roles).Count() == roles.Length) return true;
        
        var accessToken = await GetAccessToken(ct);
        var client = _httpClientFactory.CreateClient("Auth0Client");
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
        var result = await Post(client, url, accessToken, rolesRepresentations, ct);
        
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

    private async ValueTask<string> GetAccessToken(CancellationToken ct)
    {
        if (_memoryCache.TryGetValue("AuthServer:ApiToken", out var token) && token is not null)
            return (string)token;
        
        var url = _settings.GetTokenUrl();

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new FormUrlEncodedContent(tokenRequest);
        
        var client = _httpClientFactory.CreateClient("Auth0Client");
        
        using var responseMessage = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();

        var body = await responseMessage.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var tokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(body)!;
        
        if (tokenResponse.ExpiresIn > 200)
            _memoryCache.Set("AuthServer:ApiToken", tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 150));

        return tokenResponse.AccessToken;
    }

    private static async Task<T> Send<T>(HttpClient client, string url, string accessToken, bool delete, CancellationToken ct)
    {
        using var requestMessage = new HttpRequestMessage(delete ? HttpMethod.Delete : HttpMethod.Get, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        using var response = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);

        if (typeof(T) == typeof(bool)) return (T)(object)response.IsSuccessStatusCode;
        
        response.EnsureSuccessStatusCode();
        
        var jsonResponse = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<T>(jsonResponse)!;
        
        return data;
    }
    
    private static async Task<(bool, string)> Post<T>(HttpClient client, string url, string accessToken, T body, CancellationToken ct)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body, SerializerOptions))
            { Headers = { ContentType = MediaTypeHeaderValue.Parse("application/json") } };
        
        using var response = await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            if (response.Headers.TryGetValues("location", out var loc))
            {
                var location = loc.FirstOrDefault();
                if (!string.IsNullOrEmpty(location) && location.Contains('/'))
                {
                    return (true, location[(location.LastIndexOf('/') + 1)..]);
                }
            }
        }

        return (response.IsSuccessStatusCode, "");
    }
}