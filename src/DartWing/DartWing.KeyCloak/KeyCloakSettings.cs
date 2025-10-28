namespace DartWing.KeyCloak;

public sealed class KeyCloakSettings
{
    public string Domain { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RealmName { get; set; }
    public string DefaultUser { get; set; }

    public string GetAudience() => ClientId;
    public string GetAudienceUrl() => GetAudienceForTokenUrl();
    public string GetSigningKeysUrl() => $"{GetDomain()}/realms/{RealmName}/protocol/openid-connect/certs";
    public string GetTokenUrl() => $"{GetDomain()}/realms/{RealmName}/protocol/openid-connect/token";
    public string GetAudienceForTokenUrl() => $"{RealmName}-realm";
    public string? GetAuthorityUrl() => $"{GetDomain()}/realms/{RealmName}";
    public string GetUserOrganizationsUrl(string userId) => $"{GetDomain()}/admin/realms/{RealmName}/organizations/members/{userId}/organizations";
    public string GetOrganizationsUrl() => $"{GetDomain()}/admin/realms/{RealmName}/organizations";
    public string GetOrganizationUrl(string orgId) => $"{GetDomain()}/admin/realms/{RealmName}/organizations/{orgId}";
    public string GetOrganizationMembersUrl(string orgId) => $"{GetDomain()}/admin/realms/{RealmName}/organizations/{orgId}/members";
    public string GetOrganizationMembersUserUrl(string orgId, string userId) => $"{GetDomain()}/admin/realms/{RealmName}/organizations/{orgId}/members/{userId}";
    public string GetUserRolesByUserIdUrl(string userId)=> $"{GetDomain()}/admin/realms/{RealmName}/users/{userId}/role-mappings";
    public string GetUserRoleMappingUrl(string userId, string clientGuid)=> $"{GetDomain()}/admin/realms/{RealmName}/users/{userId}/role-mappings/clients/{clientGuid}";
    public string GetAvailableRolesUrl(string userId)=> $"{GetDomain()}/admin/realms/{RealmName}/ui-ext/available-roles/users/{userId}?first=0&max=100";
    public string GetUserByIdUrl(string userId)=> $"{GetDomain()}/admin/realms/{RealmName}/users/{userId}";
    public string GetUsersUrl()=> $"{GetDomain()}/admin/realms/{RealmName}/users";
    public string GetUsersByEmailUrl(string email)=> $"{GetDomain()}/admin/realms/{RealmName}/users?email={email}";
    public string GetProviderAuthUrl(string provider)  => $"{GetDomain()}/realms/{RealmName}/protocol/openid-connect/auth?client_id={ClientId}&response_type=code&scope=openid&kc_idp_hint={provider}&prompt=login";

    public string GetRegularAuthUrl(string redirectUri) =>
        $"{GetDomain()}/realms/{RealmName}/protocol/openid-connect/auth?client_id={ClientId}&response_type=code&scope=openid profile email&redirect_uri={Uri.EscapeDataString(redirectUri)}";
    public string GetClientsByClientIdUrl(string clientId)=> $"{GetDomain()}/admin/realms/{RealmName}/clients?clientId={clientId}";
    public string GetClientByIdUrl(string clientId)=> $"{GetDomain()}/admin/realms/{RealmName}/clients/{clientId}";
    public string GetClientsUrl() => $"{GetDomain()}/admin/realms/{RealmName}/clients";
    public string GetClientSecretUrl(string clientId) => $"{GetDomain()}/admin/realms/{RealmName}/clients/{clientId}/client-secret";
    
    private string? GetDomain()
    {
        if (string.IsNullOrEmpty(Domain) ||
            Domain.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
            Domain.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) return Domain;
        return "https://" + Domain;
    }
}