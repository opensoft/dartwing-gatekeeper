namespace DartWing.Web.Users.Dto;

public sealed class CompanyCreateRequest
{
    public string Name { get; set; }
    public string Abbreviation { get; set; }
    public string Currency { get; set; }
    public string Country { get; set; }
    public string Domain { get; set; }
    public bool? IsEnabled { get; set; } = true;
    public string CompanyType { get; set; } = "Company";
    
    public string? MicrosoftSharepointFolderPath { get; set; }
}
