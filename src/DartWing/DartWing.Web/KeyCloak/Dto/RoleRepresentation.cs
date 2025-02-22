using System.Text.Json.Serialization;

namespace DartWing.Web.KeyCloak.Dto;

internal sealed class RoleRepresentation
{
    [JsonPropertyName("realmMappings")]
    public List<RoleMapping> RealmMappings { get; set; }

    [JsonPropertyName("clientMappings")]
    public Dictionary<string, ClientMapping> ClientMappings { get; set; }
}

public class RoleMapping
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("composite")]
    public bool Composite { get; set; }

    [JsonPropertyName("clientRole")]
    public bool ClientRole { get; set; }

    [JsonPropertyName("containerId")]
    public string ContainerId { get; set; }
}

public class Mapping
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("composite")]
    public bool Composite { get; set; }

    [JsonPropertyName("clientRole")]
    public bool ClientRole { get; set; }

    [JsonPropertyName("containerId")]
    public string ContainerId { get; set; }
}

public class ClientMapping
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("client")]
    public string Client { get; set; }

    [JsonPropertyName("mappings")]
    public List<Mapping> Mappings { get; set; }
}


internal sealed class ClientMappings
{
    [JsonPropertyName("client")]
    public Dictionary<string, ClientMapping> Clients { get; set; }
}