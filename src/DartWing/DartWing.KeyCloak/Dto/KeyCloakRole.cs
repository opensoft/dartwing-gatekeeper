namespace DartWing.KeyCloak.Dto;

public sealed class KeyCloakRole
{
    public List<KeyCloakRoleMapping> RealmMappings { get; set; }
    public Dictionary<string, KeyCloakClientMapping> ClientMappings { get; set; }
}

public sealed class KeyCloakRoleMapping
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool Composite { get; set; }
    public bool ClientRole { get; set; }
    public string ContainerId { get; set; }
}

public sealed class KeyCloakMapping
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool Composite { get; set; }
    public bool ClientRole { get; set; }
    public string ContainerId { get; set; }
}

public sealed class KeyCloakClientMapping
{
    public string Id { get; set; }
    public string Client { get; set; }
    public List<KeyCloakMapping> Mappings { get; set; }
}


internal sealed class KeyCloakClientMappings
{
    public Dictionary<string, KeyCloakClientMappings> Clients { get; set; }
}