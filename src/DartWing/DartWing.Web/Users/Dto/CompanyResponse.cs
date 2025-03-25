using DartWing.ErpNext.Dto;

namespace DartWing.Web.Users.Dto;

public sealed class CompanyResponse
{
    public CompanyResponse() {}

    public CompanyResponse(CompanyDto erpCompanyData)
    {
        Name = erpCompanyData.Name;
        Abbr = erpCompanyData.Abbr;
        DefaultCurrency = erpCompanyData.DefaultCurrency;
        Domain = erpCompanyData.Domain;
        Country = erpCompanyData.Country;
        CompanyType = erpCompanyData.CustomType;
        MicrosoftTenantId = erpCompanyData.CustomMicrosoftTenantId;
        MicrosoftTenantName = erpCompanyData.CustomMicrosoftTenantName;
        MicrosoftSharepointFolderPath = erpCompanyData.CustomMicrosoftSharepointFolderPath;
        IsEnabled = erpCompanyData.IsEnabled;
    }
    
    public string Name { get; set; }
    public string Abbr { get; set; }
    public string DefaultCurrency { get; set; }
    public string Domain { get; set; }
    public bool IsEnabled { get; set; }
    public string Country { get; set; }
    
    public string CompanyType { get; set; }
    
    public string? MicrosoftTenantId { get; set; }
    public string? MicrosoftTenantName { get; set; }
    public string? MicrosoftSharepointFolderPath { get; set; }
}

public sealed class CompanyProvidersResponse
{
    public CompanyProviderResponse[] Providers { get; set; }
}

public sealed class CompanyProviderResponse
{
    public string Alias { get; set; }
    public string Name { get; set; }
}