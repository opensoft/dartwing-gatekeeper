using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public sealed class FrappeWarehouseTypeService : FrappeBaseService<FrappeWarehouseTypeData>
{
    public FrappeWarehouseTypeService(ILogger<FrappeWarehouseTypeService> logger, HttpClient httpClient,
        IMemoryCache memoryCache) : base(logger, httpClient, memoryCache)
    {
    }
}

public sealed class FrappeWarehouseTypeData : IFrappeBaseDto
{
    public string Name { get; set; } = "Transit";
}