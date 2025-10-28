using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public sealed class FrappeSupplierService : FrappeBaseService<FrappeSupplierData>
{
    private readonly IServiceProvider _serviceProvider;

    public FrappeSupplierService(ILogger<FrappeSupplierService> logger, HttpClient httpClient, IMemoryCache memoryCache,
        IServiceProvider serviceProvider) : base(logger, httpClient, memoryCache)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<FrappeSupplierData?> Create(FrappeSupplierData request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.Country) && request.Country.Length == 2)
        {
            var cService = _serviceProvider.GetService<FrappeCountryService>()!;
            var c = await cService.GetCountryByCode(request.Country, ct) ?? await cService.GetCountryByCode("us", ct);
            request.Country = c!;
        }
        if (string.IsNullOrEmpty(request.DefaultCurrency)) request.DefaultCurrency = "USD";
        
        return await base.Create(request, ct);
    }
}

public sealed class ErpDefaultPayableAccount
{
    public string Company { get; set; } = default!;
    public string Account { get; set; } = default!;
}

public sealed class FrappeSupplierData : IFrappeBaseDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string NamingSeries { get; set; }
    public string SupplierName => Name;
    public string Country { get; set; }
    public string SupplierGroup { get; set; }
    public string SupplierType { get; set; }
    public int IsTransporter { get; set; }
    public string DefaultCurrency { get; set; }
    public int IsInternalSupplier { get; set; }
    public string RepresentsCompany { get; set; }
    public string Language { get; set; } = "en";
    public string TaxId { get; set; }
    public int Irs1099 { get; set; }
    public int AllowPurchaseInvoiceCreationWithoutPurchaseOrder { get; set; }
    public int AllowPurchaseInvoiceCreationWithoutPurchaseReceipt { get; set; }
    public int IsFrozen { get; set; }
    public int Disabled { get; set; }
    public int WarnRfqs { get; set; }
    public int WarnPos { get; set; }
    public int PreventRfqs { get; set; }
    public int PreventPos { get; set; }
    public int OnHold { get; set; }
    public string HoldType { get; set; }
    public string Doctype { get; set; }
    public List<object> Companies { get; set; }
    public List<object> Accounts { get; set; }
    public List<object> PortalUsers { get; set; }
    public string? SupplierDetails { get; set; }
    public string? Website { get; set; }
    
    public string? CustomInvoiceEmailsWhitelist { get; set; }
    
    public List<ErpDefaultPayableAccount>? DefaultPayableAccounts { get; set; }
    public string? Details { get; set; }
}