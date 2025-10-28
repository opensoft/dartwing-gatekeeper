namespace DartWing.KeyCloak.Dto;

public sealed class KeyCloakOrganization
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Alias { get; set; }
    public bool Enabled { get; set; } = true;
    public string RedirectUrl { get; set; }
    public KeyCloakOrganizationDomain[] Domains { get; set; } = [];
    public Dictionary<string, string[]> Attributes { get; set; } = [];
}

public sealed class KeyCloakOrganizationAddress
{
    public string? Zip { get; set; }
    public string? Country { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Name { get; set; }
}