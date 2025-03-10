using System.Text.Json.Serialization;

namespace DartWing.Web.KeyCloak;

public sealed class KeyCloakSettings
{
    [JsonPropertyName("Domain")]
    public string Domain { get; set; }

    [JsonPropertyName("ClientId")]
    public string ClientId { get; set; }

    [JsonPropertyName("ClientSecret")]
    public string ClientSecret { get; set; }

    [JsonPropertyName("RealmName")]
    public string RealmName { get; set; }
    
    [JsonPropertyName("ClientGuid")]
    public string ClientGuid { get; set; }

    public string GetAudienceUrl() => GetAudienceForTokenUrl();
    public string GetSigningKeysUrl() => $"https://{Domain}/realms/{RealmName}/protocol/openid-connect/certs";
    public string GetTokenUrl() => $"https://{Domain}/realms/{RealmName}/protocol/openid-connect/token";
    public string GetAudienceForTokenUrl() => $"{RealmName}-realm";
    public string? GetAuthorityUrl() => $"https://{Domain}/realms/{RealmName}";
    public string GetOrganizationUrl() => $"https://{Domain}/admin/realms/{RealmName}/organizations";
    public string GetOrganizationMembersUrl(string orgId) => $"https://{Domain}/admin/realms/{RealmName}/organizations/{orgId}/members";
    public string GetUserRolesByUserIdUrl(string userId)=> $"https://{Domain}/admin/realms/{RealmName}/users/{userId}/role-mappings";
    public string GetUserRoleMappingUrl(string userId)=> $"https://{Domain}/admin/realms/{RealmName}/users/{userId}/role-mappings/clients/{ClientGuid}";
    public string GetAvailableRolesUrl(string userId)=> $"https://{Domain}/admin/realms/{RealmName}/ui-ext/available-roles/users/{userId}?first=0&max=100";
    public string GetUserByIdUrl(string userId)=> $"https://{Domain}/admin/realms/{RealmName}/users/{userId}";
    
}