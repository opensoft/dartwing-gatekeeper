using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public sealed class FrappeCountryDto : IFrappeBaseDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string CountryName { get; set; }
    public string DateFormat { get; set; }
    public string TimeFormat { get; set; }
    public string TimeZones { get; set; }
    public string Code { get; set; }
}

public sealed class FrappeCountryService : FrappeBaseReadonlyService<FrappeCountryDto>
{
    private const string MemoryKey = "Erp:Country:";

    private static readonly string[] FieldsQuery = ["""fields=["name","code"]""", "limit_page_length=300"];
    
    public FrappeCountryService(ILogger<FrappeCountryService> logger, HttpClient httpClient, IMemoryCache memoryCache)
        : base(logger, httpClient, memoryCache)
    {
    }

    public async ValueTask<Dictionary<string, string>> GetCompanyCodes(CancellationToken ct)
    {
        if (MemoryCache.TryGetValue(MemoryKey, out Dictionary<string, string>? value) && value != null) return value;
        
        var result = await GetListProtected(FieldsQuery, ct);
        if (result == null) return [];
        
        var response = result.ToDictionary(x => x.Code, x => x.Name);
        MemoryCache.Set(MemoryKey, response);

        return response;
    }

    public async ValueTask<string?> GetCountryByCode(string code, CancellationToken ct)
    {
        var codes = await GetCompanyCodes(ct);
        return codes.GetValueOrDefault(code.ToLowerInvariant());
    }
}