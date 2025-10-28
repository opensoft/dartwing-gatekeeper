using System.ComponentModel.DataAnnotations;
using DartWing.DomainModel.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public interface IErpCompanyService : IFrappeBaseService
{
    Task<FrappeCompanyDto[]?> GetByFullName(string name, CancellationToken ct);
    Task<FrappeCompanyDto?> CreateAndValidate(FrappeCompanyDto request, string email, CancellationToken ct);
    Task<FrappeCompanyDto?> Update(string name, FrappeCompanyDto request, CancellationToken ct);
    Task<FrappeCompanyDto[]?> GetList(int? page = null, int? pageSize = null, CancellationToken ct = default);
    ValueTask<List<FrappeCompanyDto>?> GetAll(CancellationToken ct);
    ValueTask<FrappeCompanyDto?> Get(string name, CancellationToken ct);
    Task<FrappeCompanyDto?> Create(FrappeCompanyDto request, CancellationToken ct);
    Task<bool> Delete(string name, CancellationToken ct);
}

public sealed class FrappeCompanyService : FrappeBaseService<FrappeCompanyDto>, IErpCompanyService
{
    public FrappeCompanyService(ILogger<FrappeCompanyService> logger, HttpClient httpClient, IMemoryCache memoryCache) : base(
        logger, httpClient, memoryCache)
    {
    }

    public async Task<FrappeCompanyDto[]?> GetByFullName(string name, CancellationToken ct)
    {
        string[] query =
        [
            $"""filters=[["custom_full_name", "=", "{name}"]]""",
        ];
        return await GetListProtected(query, ct);
    }

    public async Task<FrappeCompanyDto?> CreateAndValidate(FrappeCompanyDto request, string email, CancellationToken ct)
    {
        request.Name ??= Guid.CreateVersion7().GetShortId();
        request.Abbr ??= request.Name;
        CheckCompanyDto(request, ct);
        var company = await Create(request, ct);
        return company ?? null;
    }

    public override async Task<FrappeCompanyDto?> Update(string name, FrappeCompanyDto request, CancellationToken ct)
    {
        CheckCompanyDto(request, ct);
        return await base.Update(name, request, ct);
    }

    private void CheckCompanyDto(FrappeCompanyDto request, CancellationToken ct)
    {
        request.CustomType ??= "Company";
        request.Country = FrappeHelpers.GetCountryName(request.Country);
        if (string.IsNullOrEmpty(request.DefaultCurrency)) request.DefaultCurrency = "USD";
        if (!string.IsNullOrWhiteSpace(request.CustomInvoicesWhitelist))
        {
            var emails = request.CustomInvoicesWhitelist.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            EmailAddressAttribute att = new();
            if (emails.Any(x => !att.IsValid(x)))
                request.CustomInvoicesWhitelist = string.Join(",", emails.Where(x => att.IsValid(x)));
        }
    }
}

public interface IFrappeCompany
{
    string CustomFullName { get; }
    string CompanyName { get; }
    string Name { get; } // Company Name
    string Abbr { get; } // Abbreviation
    string DefaultCurrency { get; } // Default Currency
    string Domain { get; } // Business Domain
    bool IsEnabled { get; } // Status
    string Country { get; }
    string CustomType { get; }
    string? CustomMicrosoftTenantId { get; }
    string? CustomMicrosoftSharepointFolderPath { get; }
    string? CustomMicrosoftSharepointUserPath { get; }
    string? CustomMicrosoftTenantName { get; }
    string? CustomInvoicesWhitelist { get; }
}

public sealed class FrappeCompanyDto : IFrappeBaseDto, IFrappeCompany
{
    public FrappeCompanyDto()
    {
    }
    public FrappeCompanyDto(IFrappeCompany cData)
    {
        CustomMicrosoftTenantId = cData.CustomMicrosoftTenantId;
        CustomMicrosoftTenantName = cData.CustomMicrosoftTenantName;
        CustomMicrosoftSharepointFolderPath = cData.CustomMicrosoftSharepointFolderPath;
        CustomMicrosoftSharepointUserPath = cData.CustomMicrosoftSharepointUserPath;
        CustomType = cData.CustomType;
        Domain = cData.Domain;
        IsEnabled = cData.IsEnabled;
        DefaultCurrency = cData.DefaultCurrency;
        Country = cData.Country;
        CustomFullName = cData.CustomFullName;
        CustomInvoicesWhitelist = cData.CustomInvoicesWhitelist;
    }

    public string CustomFullName { get; set; }

    public string CompanyName => Name;
    public string Name { get; set; } // Company Name
    public string Abbr { get; set; } // Abbreviation
    public string DefaultCurrency { get; set; } // Default Currency
    public string Domain { get; set; } // Business Domain
    public bool IsEnabled { get; set; } = true; // Status
    public string Country { get; set; }
    public string ChartOfAccounts { get; set; } = "Standard";
    
    public string CustomType { get; set; }
    
    public string? CustomMicrosoftTenantId { get; set; }
    public string? CustomMicrosoftSharepointFolderPath { get; set; }
    public string? CustomMicrosoftSharepointUserPath { get; set; }
    public string? CustomMicrosoftTenantName { get; set; }
    
    public string? CustomInvoicesWhitelist { get; set; }
}
