namespace DartWing.KeyCloak.Dto;

public sealed class KeyCloakUser
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }
    public long CreatedTimestamp { get; set; }
    public bool Enabled { get; set; }
    public bool Totp { get; set; }
    public List<string> DisableableCredentialTypes { get; set; }
    public List<string> RequiredActions { get; set; }
    public int NotBefore { get; set; }
    public string MembershipType { get; set; }
}