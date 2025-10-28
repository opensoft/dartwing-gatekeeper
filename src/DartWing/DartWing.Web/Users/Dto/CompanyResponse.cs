using DartWing.Frappe.Erp;
using DartWing.KeyCloak;
using DartWing.KeyCloak.Dto;

namespace DartWing.Web.Users.Dto;

public sealed class CompanyResponse
{
    public CompanyResponse() {}

    public CompanyResponse(IFrappeCompany erpCompanyData)
    {
        Id = erpCompanyData.Name;
        Abbr = erpCompanyData.Abbr;
        DefaultCurrency = erpCompanyData.DefaultCurrency;
        Domain = erpCompanyData.Domain;
        Country = erpCompanyData.Country;
        CompanyType = erpCompanyData.CustomType;
        MicrosoftTenantId = erpCompanyData.CustomMicrosoftTenantId;
        MicrosoftSharepointUserPath = erpCompanyData.CustomMicrosoftSharepointUserPath;
        MicrosoftSharepointFolderPath = erpCompanyData.CustomMicrosoftSharepointFolderPath;
        IsEnabled = erpCompanyData.IsEnabled;
        Name = erpCompanyData.CustomFullName;
        InvoicesWhitelist = erpCompanyData.CustomInvoicesWhitelist?.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }


    public string Id { get; set; }
    public string Abbr { get; set; }
    public string Name { get; set; }
    public string DefaultCurrency { get; set; }
    public string Domain { get; set; }
    public bool IsEnabled { get; set; }
    public string Country { get; set; }
    
    public string CompanyType { get; set; }
    
    public string? MicrosoftTenantId { get; set; }
    public string? MicrosoftSharepointFolderPath { get; set; }
    public string? MicrosoftSharepointUserPath { get; set; }

    public string[]? InvoicesWhitelist { get; set; }
    public string CompanyAlias { get; set; }
}

public sealed class CompanyRoleResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Site { get; set; }
    public string Alias { get; set; }
    public string[] Permissions { get; set; }
    public string MsTenantId { get; set; }
    public string? Country { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? BusinessName { get; set; }
    public string? Created { get; set; }


    public CompanyRoleResponse(KeyCloakOrganization org, string[] permissions)
    {
        Id = org.Id;
        var ind = org.Name.IndexOf("__", StringComparison.OrdinalIgnoreCase);
        if (ind != -1)
        {
            Name = org.Name[(ind + 2)..];
            Site = org.Name[..ind];
        }
        else
        {
            Name = org.Name;
        }

        Permissions = permissions;
        MsTenantId = org.GetMsTenantId();
        var address = org.GetAddress();
        Country = address.Country;
        Street = address.Street;
        City = address.City;
        State = address.State;
        PostalCode = address.Zip;
        BusinessName = address.Name;
        Alias = org.Alias;
        Created = org.GetCreatedDate();
    }
}

public sealed class UserWithCompaniesResponse
{
    public UserWithCompaniesResponse(CompanyRoleResponse[] result)
    {
        Companies = result;
    }
    
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string? Company { get; set; }
    public string? Site { get; set; }
    public CompanyRoleResponse[] Companies { get; set; }
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

public sealed class CompanyPath
{
    public string DriveId { get; set; }
    public string FolderId { get; set; }
}
