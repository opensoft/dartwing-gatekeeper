using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public class FrappeContactService : FrappeBaseService<FrappeContactRequest>
{
    public FrappeContactService(ILogger logger, HttpClient httpClient, IMemoryCache memoryCache) : base(logger, httpClient, memoryCache)
    {
    }
}

public sealed class FrappeContactRequest : IFrappeBaseDto
{
    public string FirstName { get; set; } = default!;
    public string? LastName { get; set; }
    public string? EmailId { get; set; }
    public string? Phone { get; set; }

    public List<ErpContactLink> Links { get; set; } = new();
}

public sealed class ErpContactLink
{
    public string LinkDoctype { get; set; } = "Supplier";
    public string LinkName { get; set; } = default!;
}