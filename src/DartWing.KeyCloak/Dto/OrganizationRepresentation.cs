namespace DartWing.KeyCloak.Dto;

internal sealed class OrganizationRepresentation
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string alias { get; set; }
    public bool enabled { get; set; }
    public string redirectUrl { get; set; }
    public OrganizationDomainRepresentation[] domains { get; set; }
}