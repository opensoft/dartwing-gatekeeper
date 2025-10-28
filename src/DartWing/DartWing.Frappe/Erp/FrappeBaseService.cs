using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public interface IFrappeBaseDto
{
}
    

public abstract class FrappeBaseService<T> : FrappeBaseReadonlyService<T>
    where T : class, IFrappeBaseDto
{
    public FrappeBaseService(ILogger logger, HttpClient httpClient, IMemoryCache memoryCache)
        : base(logger, httpClient, memoryCache)
    {
    }

    public virtual async Task<T?> Create(T request, CancellationToken ct)
    {
        var response = await Client.Send<T, ErpResponseDto<T>>(HttpMethod.Post, RelativeUrl(), "", [], request, Logger, ct);
        if (response?.Data != null) MemoryCache.Remove(CacheKey("all"));
        return response?.Data;
    }
    
    public virtual async Task<T?> Update(string name, T request, CancellationToken ct)
    {
        var response = await Client.Send<T, ErpResponseDto<T>>(HttpMethod.Put, RelativeUrl(), Uri.EscapeDataString(name), [], request, Logger, ct);
        if (response == null) return response?.Data;
        MemoryCache.Remove(CacheKey("all"));
        MemoryCache.Remove(CacheKey(name));
        return response?.Data;
    }

    public async Task<bool> Delete(string name, CancellationToken ct)
    {
        var response = await Client.Send<bool>(HttpMethod.Delete, RelativeUrl(), Uri.EscapeDataString(name), [], Logger, ct);
        if (!response) return response;
        MemoryCache.Remove(CacheKey("all"));
        MemoryCache.Remove(CacheKey(name));
        return response;
    }
}

public abstract class FrappeBaseReadonlyService<T> : IFrappeBaseService where T : class, IFrappeBaseDto
{
    protected readonly ILogger Logger;
    protected readonly HttpClient Client;
    protected readonly IMemoryCache MemoryCache;

    public FrappeBaseReadonlyService(ILogger logger, HttpClient httpClient, IMemoryCache memoryCache)
    {
        Logger = logger;
        Client = httpClient;
        MemoryCache = memoryCache;
    }

    public virtual Task<T[]?> GetList(int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        string[] query = [];
        if (page != null && pageSize != null)
        {
            query =
            [
                GetStartQuery(page, pageSize),
                GetPageSizeQuery(pageSize)
            ];
        }
        return GetListProtected(query, ct);
    }

    public async ValueTask<List<T>?> GetAll(CancellationToken ct)
    {
        var key = CacheKey("all");
        if (MemoryCache.TryGetValue(key, out List<T>? list) && list != null) return list;
        var pos = 0;
        var pageSize = GetMaxPageSize();
        List<T> result = [];
        var batch = await GetList(0, pageSize, ct);
        result.AddRange(batch);
        if (batch.Length == pageSize)
        {
            for (var i = 0; i < 100; i++)
            {
                batch = await GetList(result.Count, pageSize, ct);
                result.AddRange(batch);
                if (batch.Length != pageSize) break;
            }
        }

        MemoryCache.Set(key, result, TimeSpan.FromSeconds(CacheLifetimeSeconds));
        return result;
    }
    
    protected async Task<T[]?> GetListProtected(string[] queryParms, CancellationToken ct = default)
    {
        var response = await Client.Send<ErpResponsesDto<T>>(HttpMethod.Get, RelativeUrl(), "", queryParms, Logger, ct);
        return response?.Data;
    }

    public async ValueTask<T?> Get(string name, CancellationToken ct)
    {
        if (MemoryCache.TryGetValue(CacheKey(name), out T? result) && result != null) return result;
        var response = await Client.Send<ErpResponseDto<T>>(HttpMethod.Get, RelativeUrl(), Uri.EscapeDataString(name), [], Logger, ct);
        if (response?.Data != null) MemoryCache.Set(CacheKey(name), response.Data, TimeSpan.FromSeconds(CacheLifetimeSeconds));
        return response?.Data;
    }

    protected virtual string RelativeUrl()
    {
        var name = GetType().Name;
        List<int> indexes = new();
        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] >= 'A' && name[i] <= 'Z') indexes.Add(i);
        }

        if (indexes.Count < 3) return "";

        if (indexes.Count == 3)
            return string.Concat("/api/resource/", name.AsSpan(indexes[1], indexes[2] - indexes[1]));
        if (indexes.Count == 4)
            return string.Concat("/api/resource/", name.AsSpan(indexes[1], indexes[2] - indexes[1]), " ",
                name.AsSpan(indexes[2], indexes[3] - indexes[2]));
        if (indexes.Count == 5)
            return
                $"/api/resource/{name.AsSpan(indexes[1], indexes[2] - indexes[1])} {name.AsSpan(indexes[2], indexes[3] - indexes[2])} {name.AsSpan(indexes[3], indexes[4] - indexes[3])}";
        return "";
    }

    protected string GetPageSizeQuery(int? pageSize = null) => $"limit_page_length={pageSize.GetValueOrDefault(GetMaxPageSize())}";
    protected string GetStartQuery(int? page, int? pageSize = null) => $"limit_start={page.GetValueOrDefault() * pageSize.GetValueOrDefault(GetMaxPageSize())}";
    
    protected virtual int GetMaxPageSize() => 100;
    protected string CacheKey(string name) => $"erp:{Client.BaseAddress?.Host}:{GetType().Name}:{name}";
    protected virtual int CacheLifetimeSeconds => 12;
}

public interface IFrappeBaseService
{
}

public class ErpResponseDto<T> where T : IFrappeBaseDto
{
    public bool Success { get; set; }
    public object Message { get; set; }
    public T? Data { get; set; }
}

public class ErpResponsesDto<T> where T : IFrappeBaseDto
{
    public bool Success { get; set; }
    public object Message { get; set; }
    public T[]? Data { get; set; }
}
