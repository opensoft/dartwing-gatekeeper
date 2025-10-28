using DartWing.DomainModel.Extensions;
using DartWing.Frappe.Erp;

namespace DartWing.Web.Suppliers;

public sealed class SupplierResponse
{
    public SupplierResponse(FrappeSupplierData response)
    {
        Name = response.Name;
        Group = response.SupplierGroup;
        Country = response.Country;
        TaxId = response.TaxId;
        AllowPurchaseInvoiceCreationWithoutPurchaseOrder = response.AllowPurchaseInvoiceCreationWithoutPurchaseOrder.ToBool();
        AllowPurchaseInvoiceCreationWithoutPurchaseReceipt = response.AllowPurchaseInvoiceCreationWithoutPurchaseReceipt.ToBool();
        Type = Enum.TryParse<ESupplierType>(response.SupplierType, out var t) ? t : ESupplierType.Company;
        Website = response.Website;
        IsTransporter = response.IsTransporter.ToBool();
        BillingCurrency = response.DefaultCurrency;
        PrintLanguage = response.Language;
        Details = response.SupplierDetails;
        IsFrozen = response.IsFrozen.ToBool();
        Disabled = response.Disabled.ToBool();
        InvoiceEmailsWhitelist = response.CustomInvoiceEmailsWhitelist?.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }


    public string Name { get; set; }
    public string? Group { get; set; }
    public string Country { get; set; }
    public ESupplierType Type { get; set; }
    public bool IsTransporter { get; set; }
    public string BillingCurrency { get; set; }
    public string PrintLanguage { get; set; }
    public string? Website { get; set; }
    public string? Details { get; set; }

    public string? TaxId { get; set; }

    public bool? AllowPurchaseInvoiceCreationWithoutPurchaseOrder { get; set; }
    public bool? AllowPurchaseInvoiceCreationWithoutPurchaseReceipt { get; set; }
    public bool? IsFrozen { get; set; }
    public bool? Disabled { get; set; }
    public string[] InvoiceEmailsWhitelist { get; set; }
}

public sealed class SuppliersResponse
{
    public string[] Names { get; set; }
} 