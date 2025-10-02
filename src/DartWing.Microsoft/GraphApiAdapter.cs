using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace DartWing.Microsoft;

public sealed class GraphApiAdapter : IDisposable
{
    private readonly string _tokenKey;
    private readonly IMemoryCache _memoryCache;
    private readonly GraphServiceClient _graphClient;

    public GraphApiAdapter(string token, IMemoryCache memoryCache)
    {
        _tokenKey = $"{token.Length}_{token[..8]}{token[8..]}";
        _memoryCache = memoryCache;
        _graphClient = new GraphServiceClient(new CustomAuthProvider(token));
    }

    
    
    public async Task<User?> Me(CancellationToken ct)
    {
        var me = await _graphClient.Me.GetAsync(cancellationToken: ct);
        return me;
    }
    
    public async Task<MicrosoftDriveInfo[]?> Drives(CancellationToken ct)
    {
        var drivesKey = "Microsoft:Drives:" + _tokenKey;
        if (_memoryCache.TryGetValue(drivesKey, out MicrosoftDriveInfo[]? drs) && drs != null) return drs;
        
        var drives = await _graphClient.Drives.GetAsync(cancellationToken: ct);
        var result = drives?.Value?.Select(s => new MicrosoftDriveInfo(s.Id, s.Name)).ToArray();
        if (result != null) _memoryCache.Set(drivesKey, result, TimeSpan.FromSeconds(60));
        return result;
    }

    public async Task<List<MicrosoftFolderInfo>> GetMyFolders(CancellationToken ct)
    {
        var meDrive = await _graphClient.Me.Drive.GetAsync(cancellationToken: ct);
        var allFolders = await GetAllFolders(meDrive!.Id!, ct: ct);
        return allFolders;
    }

    public async Task<MicrosoftSiteInfo[]?> GetAllSites(CancellationToken ct)
    {
        var sites = await _graphClient.Sites.GetAsync(cancellationToken: ct);
        return sites?.Value?.Select(s => new MicrosoftSiteInfo(s.Id, s.Name)).ToArray();
    }

    public async Task<List<MicrosoftSiteInfo>> GetAllSitesWithDrives(CancellationToken ct)
    {
        SiteCollectionResponse? sites = null;
        var sitesKey = "Microsoft:Sites:" + _tokenKey;
        if (_memoryCache.TryGetValue(sitesKey, out SiteCollectionResponse? sts) && sts != null)
        {
            sites = sts;
        }
        else
        {
            sites = await _graphClient.Sites.GetAsync(cancellationToken: ct);
            _memoryCache.Set(sitesKey, sites, TimeSpan.FromSeconds(300));
        }
        if (sites?.Value == null) return [];
        List<Task<MicrosoftSiteInfo?>> tasks = [];
        foreach (var s in sites.Value.Where(s => !s.Id.Contains("-my.sharepoint.com,")))
        {
            var driveTask = GetDrivesPrivate(s, ct);
            tasks.Add(driveTask);
            if (tasks.Count(x => !x.IsCompleted) > 6)
            {
                await Task.WhenAny(tasks.Where(x => !x.IsCompleted));
            }
        }
        
        await Task.WhenAll(tasks);

        return tasks.Select(x => x.Result).Where(x => x != null).ToList();
    }

    private async Task<MicrosoftSiteInfo?> GetDrivesPrivate(Site s, CancellationToken ct)
    {
        var siteDrives = await _graphClient.Sites[s.Id].Drives.GetAsync(cancellationToken: ct);
        if (siteDrives?.Value == null || siteDrives.Value.Count == 0) return null;
        var st = new MicrosoftSiteInfo(s.Id, s.Name);
        foreach (var d in siteDrives.Value)
        {
            if (d == null) continue;
            st.Drives.Add(new MicrosoftDriveInfo(d.Id, d.Name));
        }
        return st;
    }

    public async Task<MicrosoftDriveInfo[]?> GetAllDrives(string siteId, CancellationToken ct)
    {
        try
        {
            var drives = await _graphClient.Sites[siteId].Drives.GetAsync(cancellationToken: ct);
            return drives?.Value?.Select(s => new MicrosoftDriveInfo(s.Id, s.Name)).ToArray();
        }
        catch (ODataError)
        {
            return [];
        }
    }

    public async Task<MicrosoftDriveInfo[]?> GetAllDrives(CancellationToken ct)
    {
        var drives = await _graphClient.Drives.GetAsync(cancellationToken: ct);
        return drives?.Value?.Select(s => new MicrosoftDriveInfo(s.Id, s.Name)).ToArray();
    }

    public async Task<List<MicrosoftFolderInfo>> GetAllFolders(string driveId, string itemId = "root",
        bool recursive = false, CancellationToken ct = default)
    {
        List<MicrosoftFolderInfo> folders = [];

        await FetchFoldersRecursive(driveId, itemId, null, folders, recursive, ct);

        return folders;
    }

    private async Task FetchFoldersRecursive(string driveId, string itemId, string? parentFolderId,
        List<MicrosoftFolderInfo> folders, bool recursive, CancellationToken ct)
    {
        try
        {
            var drivesKey = $"Microsoft:Drives:{_tokenKey}:Folders:{driveId}:{itemId}";
            DriveItemCollectionResponse? children = null;
            if (_memoryCache.TryGetValue(drivesKey, out DriveItemCollectionResponse? ch) && ch != null)
            {
                children = ch;
            }
            else
            {
                children = await _graphClient.Drives[driveId].Items[itemId].Children.GetAsync(cancellationToken: ct);
                if (children?.Value != null) _memoryCache.Set(drivesKey, children, TimeSpan.FromSeconds(30));
            }

            if (children?.Value == null) return;
            foreach (var item in children.Value)
            {
                if (item.Folder == null) continue;
                var folderInfo = new MicrosoftFolderInfo
                {
                    Id = item.Id!,
                    Name = item.Name!,
                    ParentFolderId = parentFolderId,
                    DriveId = driveId
                };

                folders.Add(folderInfo);

                if (recursive) await FetchFoldersRecursive(driveId, item.Id!, item.Id!, folders, true, ct);
            }
        }
        catch (ODataError)
        {
        }
    }
    
    public async Task<bool> UploadFile(string driveId, string itemId, string fileName, string fileContentType, Stream stream, CancellationToken ct)
    {
        try
        {
            await _graphClient.Drives[driveId].Items[itemId].ItemWithPath(fileName).Content.PutAsync(stream, cancellationToken: ct);
        }
        catch (ODataError e)
        {
            return false;
        }

        return true;
    }

    public sealed class MicrosoftFolderInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentFolderId { get; set; }
        public string DriveId { get; set; }
    }

    public sealed class MicrosoftDriveInfo
    {
        public MicrosoftDriveInfo(string? id, string? name)
        {
            Id = id;
            Name = name;
        }

        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<MicrosoftFolderInfo> Folders { get; set; } = [];
        public MicrosoftSiteInfo Site { get; set; }
    }

    public sealed class MicrosoftSiteInfo
    {
        public MicrosoftSiteInfo(string? id, string? name)
        {
            Id = id;
            Name = name;
        }

        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<MicrosoftDriveInfo> Drives { get; set; } = [];
    }

    public void Dispose()
    {
        _graphClient.Dispose();
    }


}
