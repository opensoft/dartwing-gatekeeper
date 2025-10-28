using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;


public sealed class ErpUserRoleDto
{
    public ErpUserRoleDto()
    {
    }

    public ErpUserRoleDto(string role)
    {
        Role = role;
    }

    public string Role { get; set; } = string.Empty;
}

public sealed class FrappeRoleDto : IFrappeBaseDto
{
    public string Name { get; set; } = string.Empty;
}


public sealed class FrappeRoleService : FrappeBaseReadonlyService<FrappeRoleDto>
{
    public FrappeRoleService(ILogger<FrappeRoleService> logger, HttpClient httpClient, IMemoryCache memoryCache)
        : base(logger, httpClient, memoryCache)
    {
    }
}