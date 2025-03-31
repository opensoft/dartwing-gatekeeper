using Microsoft.Extensions.Caching.Memory;

namespace DartWing.Microsoft;

public class GraphApiManager : IDisposable
{
    private GraphApiAdapter _userAdapter;
    private GraphApiAdapter? _clientAdapter;
    
    public GraphApiManager(GraphApiAdapter userAdapter, GraphApiAdapter? clientAdapter)
    {
        _userAdapter = userAdapter;
        _clientAdapter = clientAdapter;
    }
    
    public async Task<(string driveId, string? folderId)> GetDriveByClient(string path, CancellationToken ct)
    {
        if (_clientAdapter == null) return ("", "");
        var paths = string.IsNullOrEmpty(path) || path == "/"
            ? []
            : path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length < 2) return ("", "");

        var allSites = await _clientAdapter.GetAllSites(ct);
        
        var siteId = allSites.FirstOrDefault(x => x.Name == paths[0])?.Id;
        if (siteId == null) return ("", "");
        var siteDrives = await _clientAdapter.GetAllDrives(siteId, ct);
        if (siteDrives.Length == 0) return ("", "");
        var driveId = siteDrives.FirstOrDefault(x => x.Name == paths[1])?.Id;
        if (driveId == null) return ("", "");
        var folderId = "root";
        for (var i = 2; i < paths.Length; i++)
        {
            var allFolders = await _userAdapter.GetAllFolders(driveId, folderId, ct: ct);
            folderId = allFolders.FirstOrDefault(x => x.Name == paths[i])?.Id;
            if (folderId == null) return ("", "");
        }
        
        return (driveId, folderId);
    }

    public void Dispose()
    {
        _userAdapter.Dispose();
        _clientAdapter?.Dispose();
    }
}