using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public sealed class FrappeDyncmicLinkData : IFrappeBaseDto
{
    public string? Name { get; set; }
    public string? Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string? ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string Parent { get; set; }
    public string Parentfield { get; set; }
    public string Parenttype { get; set; }
    public string LinkDoctype { get; set; }
    public string LinkName { get; set; }
    public string? LinkTitle { get; set; }
}

public sealed class FrappeDynamicLinkService : FrappeBaseService<FrappeDyncmicLinkData>
{
    public FrappeDynamicLinkService(ILogger logger, HttpClient httpClient, IMemoryCache memoryCache) : base(logger, httpClient, memoryCache)
    {
    }
    
    public async Task<FrappeDyncmicLinkData[]?> GetList(string type, CancellationToken ct)
    {
        string[] query = ["fields=[\"*\"]", $"parent={type}"];
        var result = await GetListProtected(query, ct);
        if (result == null) return null;
        return result.All(x => x.Parenttype == type)
            ? result
            : result.Where(x => x.Parenttype == type).ToArray();
    }
}