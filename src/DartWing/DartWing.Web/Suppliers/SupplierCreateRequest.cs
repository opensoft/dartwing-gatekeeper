using DartWing.DomainModel.Extensions;
using DartWing.Frappe.Erp;

namespace DartWing.Web.Suppliers;

public sealed class SupplierCreateRequest
{
    public string Name { get; set; } = null!;
    public string? Group { get; set; }
    public string Country { get; set; } = "United States";
    public ESupplierType Type { get; set; } = ESupplierType.Company;
    public bool IsTransporter { get; set; }
    public string BillingCurrency { get; set; } = "USD";
    public string PrintLanguage { get; set; } = "English";
    public string? Website { get; set; }
    public string? Details { get; set; }

    public string? TaxId { get; set; }

    public bool? AllowPurchaseInvoiceCreationWithoutPurchaseOrder { get; set; }
    public bool? AllowPurchaseInvoiceCreationWithoutPurchaseReceipt { get; set; }
    public bool? IsFrozen { get; set; }
    public bool? Disabled { get; set; }
    public string[]? InvoiceEmailsWhitelist { get; set; }

    public FrappeSupplierData GetRequest()
    {
        FrappeSupplierData supplier = new()
        {
            Name = Name,
            SupplierGroup = Group,
            AllowPurchaseInvoiceCreationWithoutPurchaseOrder =
                AllowPurchaseInvoiceCreationWithoutPurchaseOrder.ToInt(),
            AllowPurchaseInvoiceCreationWithoutPurchaseReceipt =
                AllowPurchaseInvoiceCreationWithoutPurchaseReceipt.ToInt(),
            Disabled = Disabled.ToInt(),
            IsFrozen = IsFrozen.ToInt(),
            TaxId = TaxId,
            Country = Country,
            DefaultCurrency = BillingCurrency,
            Details = Details,
            IsTransporter = IsTransporter.ToInt(),
            SupplierType = Type.ToString(),
            Website = Website,
            Language = PrintLanguage,
            CustomInvoiceEmailsWhitelist = InvoiceEmailsWhitelist != null ? string.Join(',', InvoiceEmailsWhitelist) : "",
        };
        
        return supplier;
    }
}

public enum ESupplierType
{
    Company,
    Individual,
    Partnership
}