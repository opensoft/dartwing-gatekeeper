using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public sealed class FrappeUserPermissionService : FrappeBaseService<FrappeUserPermission>
{
    public FrappeUserPermissionService(ILogger<FrappeUserPermissionService> logger, HttpClient httpClient,
        IMemoryCache memoryCache) : base(logger, httpClient, memoryCache)
    {
    }

    public async ValueTask<FrappeUserPermission[]?> GetUserCompanies(string email, CancellationToken ct)
    {
        var key = CacheKey(email);
        if (MemoryCache.TryGetValue(key, out FrappeUserPermission[]? userPermissions) && userPermissions != null) return userPermissions;
        string[] query =
        [
            $"""filters=[["user", "=", "{email}"], ["allow", "=", "Company"]]""",
            $"""fields=["for_value", "name", "user", "custom_companyrole"]"""
        ];
        var response = await GetListProtected(query, ct);
        if (response != null) MemoryCache.Set(key, response, TimeSpan.FromSeconds(30));
        return response;
    }
    
    public async ValueTask<FrappeUserPermission[]?> GetCompanyUsers(string company, CancellationToken ct)
    {
        var key = CacheKey(company);
        if (MemoryCache.TryGetValue(key, out FrappeUserPermission[]? userPermissions) && userPermissions != null) return userPermissions;
        string[] query =
        [
            $"""filters=[["for_value", "=", "{company}"], ["allow", "=", "Company"]]""",
            $"""fields=["for_value", "name", "user", "custom_companyrole"]"""
        ];
        var response = await GetListProtected(query, ct);
        if (response != null) MemoryCache.Set(key, response, TimeSpan.FromSeconds(30));
        return response;
    }

    public async Task<FrappeUserPermission?> AddToCompany(string email, string companyName, string role,
        CancellationToken ct)
    {
        FrappeUserPermission dto = new()
        {
            User = email,
            Allow = "Company",
            ForValue = companyName,
            ApplyToAllDoctypes = 1,
            CustomCompanyrole = role
        };
        var response = await Create(dto, ct);
        if (response != null)
        {
            MemoryCache.Remove(CacheKey(email));
            MemoryCache.Remove(CacheKey(companyName));
        }
        return response;
    }

    public async Task<bool> RemoveFromCompany(string email, string companyName, string role, CancellationToken ct)
    {
        var permissions = await GetUserCompanies(email, ct);
        if (permissions == null) return false;
        var permission = permissions.FirstOrDefault(x => x.ForValue == companyName && x.CustomCompanyrole == role);
        if (permission == null) return true;

        var result = await Delete(permission.Name, ct);
        if (result)
        {
            MemoryCache.Remove(CacheKey(email));
            MemoryCache.Remove(CacheKey(companyName));
        }
        return result;
    }
}

public sealed class FrappeUserPermission : IFrappeBaseDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string User { get; set; }
    public string Allow { get; set; }
    public string ForValue { get; set; }
    public int IsDefault { get; set; }
    public int ApplyToAllDoctypes { get; set; }
    public int HideDescendants { get; set; }
    public string Doctype { get; set; }
    public string CustomCompanyrole { get; set; }
}