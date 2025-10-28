namespace DartWing.Web.Users.Dto;

public sealed class CompanyCreateRequest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Abbreviation { get; set; }
    public string Currency { get; set; }
    public string Domain { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string CompanyType { get; set; } = "Company";
    
    public string? MicrosoftSharepointFolderPath { get; set; }

    public string[] InvoicesWhitelist { get; set; } = [];
    public string? FrappeSiteUrl { get; set; }
    public string Alias { get; set; }
    public string SiteHost { get; set; }
    
    public CompanyAddress Address { get; set; }
}

public sealed class CompanyAddress
{
    public string Country { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string BusinessName { get; set; }
}
