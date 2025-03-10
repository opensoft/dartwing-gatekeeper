namespace DartWing.Web.KeyCloak.Dto;

public sealed class UserResponse
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public Dictionary<string, string[]> Attributes { get; set; } = new();

    public string? CrmId => Attributes.TryGetValue("crmID", out var value) ? value[0] : null;
}