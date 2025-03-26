namespace DartWing.KeyCloak.Dto;

internal sealed class AvailableRolesRepresentation
{
    public string id { get; set; }
    public string role { get; set; }
    public string client { get; set; }
    public string clientId { get; set; }
    public string description { get; set; }
}