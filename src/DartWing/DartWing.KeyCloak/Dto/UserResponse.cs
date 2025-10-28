namespace DartWing.KeyCloak.Dto;

public interface IKeyCloakUser
{
    string Id { get; }
    string Username { get; }
    string FirstName { get; }
    string LastName { get; }
    string Email { get; }
    Dictionary<string, string[]> Attributes { get; }
}

public sealed class UserResponse : IKeyCloakUser
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public Dictionary<string, string[]> Attributes { get; set; } = new();
}