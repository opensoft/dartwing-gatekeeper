using System.Text.Json.Serialization;

namespace DartWing.KeyCloak.Dto;

public sealed class KeyCloakClientDetail
{
    public string Protocol { get; set; } = "openid-connect";
    public string ClientId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool PublicClient { get; set; }
    public bool AuthorizationServicesEnabled { get; set; }
    public bool ServiceAccountsEnabled { get; set; }
    public bool ImplicitFlowEnabled { get; set; }
    public bool DirectAccessGrantsEnabled { get; set; }
    public bool StandardFlowEnabled { get; set; } = true;
    public bool FrontchannelLogout { get; set; }
    public KeyCloakAttributes Attributes { get; set; }
    public bool AlwaysDisplayInConsole { get; set; }
    public string RootUrl { get; set; }
    public string BaseUrl { get; set; }
    public List<string> RedirectUris { get; set; }
    public string Id { get; set; }
    [JsonIgnore]
    public string? Secret { get; set; }
}

public sealed class KeyCloakAttributes
{
    public string SamlIdpInitiatedSsoUrlName { get; set; }
    public bool StandardTokenExchangeEnabled { get; set; }
    public bool OAuth2DeviceAuthorizationGrantEnabled { get; set; }
    public bool OidcCibaGrantEnabled { get; set; }
}

public sealed class KeyCloakClientSecret
{
    public string Type { get; set; }
    public string Value { get; set; }
}