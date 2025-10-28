using DartWing.KeyCloak.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.KeyCloak;

public sealed class KeyCloakProvider
{
    private readonly ILogger<KeyCloakProvider> _logger;
    private readonly KeyCloakSettings _settings;
    private readonly KeyCloakClient _keyCloakClient;
    private readonly IMemoryCache _memoryCache;

    public KeyCloakProvider(ILogger<KeyCloakProvider> logger, KeyCloakSettings settings, KeyCloakClient keyCloakClient, IMemoryCache memoryCache)
    {
        _logger = logger;
        _settings = settings;
        _keyCloakClient = keyCloakClient;
        _memoryCache = memoryCache;
    }

    public async Task<string> CreateOrganizationWithPermissions(KeyCloakOrganization organization, string userId,
        CancellationToken ct)
    {
        var orgId = await CreateOrganization(organization, ct);
        if (string.IsNullOrEmpty(orgId)) return orgId;
        var org = await GetOrganization(orgId, ct);
        await AddUserToOrganization(org!, userId, ct);
        return orgId;
    }

    public async Task<string> CreateOrganization(KeyCloakOrganization organization, CancellationToken ct)
    {
        var (_, orgId) = await _keyCloakClient.Post(_settings.GetOrganizationsUrl(), organization,
            [_settings.GetOrganizationsUrl()], ct);
        if (!string.IsNullOrEmpty(orgId)) organization.Id = orgId;
        return orgId;
    }
    
    public Task<bool> UpdateOrganization(KeyCloakOrganization organization, CancellationToken ct)
    {
        return _keyCloakClient.Put(_settings.GetOrganizationUrl(organization.Id), organization,
            [_settings.GetOrganizationUrl(organization.Id)], ct);
    }
    
    public Task DeleteOrganization(KeyCloakOrganization org, CancellationToken ct)
    {
        return _keyCloakClient.Delete(_settings.GetOrganizationUrl(org.Id), [_settings.GetOrganizationsUrl()], ct);
    }

    private async ValueTask<string?> GetOrganizationId(string orgName, CancellationToken ct)
    {
        var allOrgs = await GetAllOrganizations(ct);
        return allOrgs.FirstOrDefault(o => o.Name == orgName)?.Id;
    }

    public async ValueTask<KeyCloakOrganization?> GetOrganization(string aliasOrId, CancellationToken ct)
    {
        var id = Guid.TryParse(aliasOrId, out _)
            ? aliasOrId
            : (await GetAllOrganizations(ct)).FirstOrDefault(o => o.Alias == aliasOrId)?.Id;
        if (id == null) return null;
        return await _keyCloakClient.Get<KeyCloakOrganization>(_settings.GetOrganizationUrl(id), ct);
    }
    
    public async ValueTask<KeyCloakOrganization?> GetOrganization(string site, string orgIdOrName, CancellationToken ct)
    {
        var orgId = string.IsNullOrEmpty(site)
            ? orgIdOrName
            : await GetOrganizationId(KeyCloakExtension.CompanyName(site, orgIdOrName), ct);
        return orgId == null
            ? null
            : await _keyCloakClient.Get<KeyCloakOrganization>(_settings.GetOrganizationUrl(orgId!), ct);
    }

    public ValueTask<IReadOnlyList<KeyCloakOrganization>> GetAllOrganizations(CancellationToken ct)
    {
        return _keyCloakClient.GetAll<KeyCloakOrganization>(_settings.GetOrganizationsUrl(), ct);
    }

    public async ValueTask<IReadOnlyList<KeyCloakUser>> GetOrganizationUsers(KeyCloakOrganization org, CancellationToken ct)
    {
        return await _keyCloakClient.GetAll<KeyCloakUser>(_settings.GetOrganizationMembersUrl(org.Id), ct);
    }
    
    public ValueTask<KeyCloakOrganization[]?> GetUserOrganizations(string userId, CancellationToken ct)
    {
        return _keyCloakClient.Get<KeyCloakOrganization[]>(_settings.GetUserOrganizationsUrl(userId), ct);
    }
    
    public async ValueTask<KeyCloakOrganization?> GetUserOrganization(string userId, string alias, CancellationToken ct)
    {
        var orgs = await _keyCloakClient.Get<KeyCloakOrganization[]>(_settings.GetUserOrganizationsUrl(userId), ct);
        var org = orgs?.FirstOrDefault(x => x.Alias == alias);
        return org != null ? await GetOrganization(org.Id, ct) : null;
    }
    
    public async ValueTask<List<KeyCloakOrganization>> GetUserOrganizationsWithPermissions(string userId, CancellationToken ct)
    {
        var orgs = await _keyCloakClient.Get<KeyCloakOrganization[]>(_settings.GetUserOrganizationsUrl(userId), ct);
        if (orgs == null || orgs.Length == 0) return [];
        List<KeyCloakOrganization> lst = new(orgs.Length);
        foreach (var org in orgs)
        {
            var orgWithPermissions = await GetOrganization(org.Id, ct);
            if (orgWithPermissions != null) lst.Add(orgWithPermissions);
        }

        return lst;
    }

    public async Task<bool> AddUserToOrganization(KeyCloakOrganization org, string userId, CancellationToken ct)
    {
        var response = await _keyCloakClient.Post(_settings.GetOrganizationMembersUrl(org.Id), userId,
            [_settings.GetUserOrganizationsUrl(userId), _settings.GetOrganizationMembersUrl(org.Id)], ct);
       
        return response.success;
    }

    public Task<bool> RemoveUserFromOrganization(KeyCloakOrganization org, string userId, CancellationToken ct)
    {
        return _keyCloakClient.Delete(_settings.GetOrganizationMembersUserUrl(org.Id, userId),
            [_settings.GetUserOrganizationsUrl(userId), _settings.GetOrganizationMembersUrl(org.Id)], ct);
    }
    
    public ValueTask<UserResponse?> GetUserById(string userId, CancellationToken ct)
    {
        return _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct);
    }
    
    public async ValueTask<UserResponse?> GetUserByEmail(string userEmail, CancellationToken ct)
    {
        var users = await _keyCloakClient.Get<UserResponse[]>(_settings.GetUsersByEmailUrl(userEmail), ct);
        if (users == null || users.Length == 0) return null;
        return await GetUserById(users[0].Id, ct);
    }
    
    public async ValueTask<UserResponse?> GetDefaultUser(CancellationToken ct)
    {
        if (_memoryCache.TryGetValue("keycloak:defaultuserid:", out string? userId) && userId != null) 
            return await GetUserById(userId, ct);
        var users = await _keyCloakClient.Get<UserResponse[]>(
            _settings.GetUsersUrl() + $"?email={_settings.DefaultUser}", ct);
        if (users == null || users.Length == 0) return null;
        _memoryCache.Set("keycloak:defaultuserid:", users[0].Id);
        return await GetUserById(users[0].Id, ct);
    }

    public async Task<bool> UpdateUserInvitations(string userId, string[] values, CancellationToken ct)
    {
        var user = await _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct, true);
        if (user == null) return false;
        user.SetInvitations(values);
        var result = await _keyCloakClient.Put(_settings.GetUserByIdUrl(userId), user, [], ct);
        if (result) await _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct, true);
        return result;
    }

    public async ValueTask<bool> AddFrappeKeys(string userId, string value, CancellationToken ct)
    {
        var user = await _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct, true);
        if (user == null) return false;
        user.AddFrappeSecret(value);
        var result = await _keyCloakClient.Put(_settings.GetUserByIdUrl(userId), user, [], ct);
        if (result) await _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct, true);
        return result;
    }
    
    public async ValueTask<bool> RemoveFrappeKeys(string userId, string value, CancellationToken ct)
    {
        var user = await _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct, true);
        if (user == null) return false;
        user.RemoveFrappeSecret(value);
        var result = await _keyCloakClient.Put(_settings.GetUserByIdUrl(userId), user, [], ct);
        if (result) await _keyCloakClient.Get<UserResponse>(_settings.GetUserByIdUrl(userId), ct, true);
        return result;
    }

    public async Task<bool> CreateClient(KeyCloakClientDetail client, CancellationToken ct)
    {
        var (_, orgId) = await _keyCloakClient.Post(_settings.GetClientsUrl(), client, [], ct);
        if (string.IsNullOrEmpty(orgId)) return false;
        client.Id = orgId;
        client.Secret = await GetClientSecret(orgId, ct);
        return !string.IsNullOrEmpty(client.Secret);
    }
    
    public Task<bool> DeleteClient(string clientId, CancellationToken ct)
    {
        return _keyCloakClient.Delete(_settings.GetClientByIdUrl(clientId), [_settings.GetClientsUrl()], ct);
    }
    
    public ValueTask<IReadOnlyList<KeyCloakClientDetail>> GetClients(CancellationToken ct)
    {
        return _keyCloakClient.GetAll<KeyCloakClientDetail>(_settings.GetClientsUrl(), ct);
    }
    
    public async ValueTask<string?> GetClientSecret(string clientId, CancellationToken ct)
    {
        var secret = await _keyCloakClient.Get<KeyCloakClientSecret>(_settings.GetClientSecretUrl(clientId), ct);
        return secret?.Value;
    }


    // public async Task<KeyCloakRole?> GetRoleRepresentationForUser(string userId, CancellationToken ct)
    // {
    //     var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
    //     var url = _settings.GetUserRolesByUserIdUrl(userId);
    //     var client = _httpClientFactory.CreateClient("KeyCloak");
    //     var roleRepresentation = await client.Get<KeyCloakRole>(url, accessToken.AccessToken, _logger, ct);
    //     return
    //         roleRepresentation; //?.ClientMappings.Clients[_settings.ClientId].Mappings.Select(x => x.Name).ToArray() ?? [];
    // }
    //
    // public async Task<string[]> GetRolesForUser(string userId, CancellationToken ct)
    // {
    //     var representation = await GetRoleRepresentationForUser(userId, ct);
    //
    //     KeyCloakClientMapping? clientMapping = null;
    //     representation?.ClientMappings?.TryGetValue(_settings.ClientId, out clientMapping);
    //     var existRoles = clientMapping?.Mappings.Select(x => x.Name).ToArray() ?? [];
    //     return existRoles;
    // }
    //
    // public async Task<bool> AddRoles(string userId, string[] roles, CancellationToken ct)
    // {
    //     var existRolesRepresentation = await GetRoleRepresentationForUser(userId, ct);
    //     KeyCloakClientMapping? clientMapping = null;
    //     existRolesRepresentation?.ClientMappings?.TryGetValue(_settings.ClientId, out clientMapping);
    //     var existRoles = clientMapping?.Mappings.Select(x => x.Name).ToArray() ?? [];
    //     if (existRoles.Length >= roles.Length && existRoles.Intersect(roles).Count() == roles.Length) return true;
    //
    //     var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
    //     var client = _httpClientFactory.CreateClient("KeyCloak");
    //     var newRoles = roles.Except(existRoles).ToArray();
    //     var availableRolesurl = _settings.GetAvailableRolesUrl(userId);
    //     var availableRoles =
    //         await client.Get<GetRoleRepresentation[]>(availableRolesurl, accessToken.AccessToken, _logger, ct);
    //     var clientsUrl = _settings.GetClientsByClientIdUrl(_settings.ClientId);
    //     var clients = await client.Get<ClientResponse[]>(clientsUrl, accessToken.AccessToken, _logger, ct);
    //
    //     var rolesRepresentations = new List<AddRoleRepresentation>(newRoles.Length);
    //     foreach (var role in newRoles)
    //     {
    //         rolesRepresentations.Add(new AddRoleRepresentation
    //         {
    //             name = role,
    //             id = availableRoles.First(x => x.client == _settings.ClientId && x.role == role).id
    //         });
    //     }
    //
    //
    //     var url = _settings.GetUserRoleMappingUrl(userId, clients[0].Id);
    //     var result = await client.Post(url, accessToken.AccessToken, rolesRepresentations, _logger, ct);
    //
    //     return result.success;
    // }
    //
    // public async Task<bool> UpdateUserCrmId(string userId, string crmId, CancellationToken ct)
    // {
    //     var accessToken = await _tokenProvider.GetClientAccessToken(ct: ct);
    //     var client = _httpClientFactory.CreateClient("KeyCloak");
    //
    //     var url = _settings.GetUserByIdUrl(userId);
    //
    //     var user = await client.Get<UserResponse>(url, accessToken.AccessToken, _logger, ct);
    //     var userUpdateData = new
    //     {
    //         user.Email,
    //         user.FirstName,
    //         user.LastName,
    //         attributes = new
    //         {
    //             crmID = new[] { crmId }
    //         }
    //     };
    //     var resp = await client.Put(url, accessToken.AccessToken, userUpdateData, _logger, ct);
    //     var userIdKey = "KeyCloakUser:" + userId;
    //     _memoryCache.Remove(userIdKey);
    //
    //     return resp;
    // }

    
}