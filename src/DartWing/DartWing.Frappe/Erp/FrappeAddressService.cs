using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public class FrappeAddressService : FrappeBaseService<FrappeAddressRequest>
{
    public FrappeAddressService(ILogger logger, HttpClient httpClient, IMemoryCache memoryCache) : base(logger, httpClient, memoryCache)
    {
    }
}

public sealed class FrappeAddressRequest : IFrappeBaseDto
{
    public string AddressTitle { get; set; } = default!;
    public string AddressLine1 { get; set; } = default!;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = default!;
    public string Country { get; set; } = "United States";
    public string? State { get; set; }
    public string? Pincode { get; set; }

    public int IsPrimaryAddress { get; set; } = 1;
    public int IsShippingAddress { get; set; } = 0;
    public int IsBillingAddress { get; set; } = 1;

    public List<ErpAddressLink> Links { get; set; } = new();
}

public sealed class ErpAddressLink
{
    public string LinkDoctype { get; set; } = "Supplier";
    public string LinkName { get; set; } = default!;
}
