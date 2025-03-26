using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace DartWing.Web.Azure;

public sealed class GraphApiAdapter
{

    private readonly HttpClient _httpClient;
    private readonly GraphServiceClient _graphClient;

    public GraphApiAdapter(string token)
    {
        _graphClient = new GraphServiceClient(new CustomAuthProvider(token));
    }

    public async Task<List<FolderInfo>> GetMyFolders(CancellationToken ct)
    {
        var meDrive = await _graphClient.Me.Drive.GetAsync(cancellationToken: ct);
        var allFolders = await GetAllFolders(meDrive, ct);
        return allFolders;
    }
    
    public async Task<SiteInfo[]?> GetAllSites(CancellationToken ct)
    {
        var sites = await _graphClient.Sites.GetAsync(cancellationToken: ct);
        return sites?.Value?.Select(s => new SiteInfo(s.Id, s.Name)).ToArray();
    }
    
    public async Task<DriveInfo[]?> GetAllDrives(string siteId, CancellationToken ct)
    {
        var sites = await _graphClient.Drives.GetAsync(cancellationToken: ct);
        return sites?.Value?.Select(s => new DriveInfo(s.Id, s.Name)).ToArray();
    }
    
    private async Task<List<FolderInfo>> GetAllFolders(Drive drive, CancellationToken ct)
    {
        List<FolderInfo> folders = [];

        await FetchFoldersRecursive(drive.Id!, "root", null, folders, ct);

        return folders;
    }

    private async Task FetchFoldersRecursive(string driveId, string itemId, string? parentFolderId, List<FolderInfo> folders, CancellationToken ct)
    {
        var children = await _graphClient.Drives[driveId].Items[itemId].Children.GetAsync(cancellationToken: ct);

        if (children?.Value == null) return;
        foreach (var item in children.Value)
        {
            if (item.Folder == null) continue;
            var folderInfo = new FolderInfo
            {
                Id = item.Id!,
                Name = item.Name!,
                ParentFolderId = parentFolderId
            };

            folders.Add(folderInfo);

            await FetchFoldersRecursive(driveId, item.Id!, item.Id!, folders, ct);
        }
    }

    public sealed class FolderInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentFolderId { get; set; }
    }

    public sealed class DriveInfo
    {
        public DriveInfo(string? id, string? name) {Id = id; Name = name; }
        
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
    
    public sealed class SiteInfo
    {
        public SiteInfo(string? id, string? name) {Id = id; Name = name; }
        
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
