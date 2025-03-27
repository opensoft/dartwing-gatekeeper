using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace DartWing.Microsoft;

public sealed class GraphApiAdapter : IDisposable
{
    private readonly GraphServiceClient _graphClient;

    public GraphApiAdapter(string token)
    {
        _graphClient = new GraphServiceClient(new CustomAuthProvider(token));
    }

    public async Task<User?> Me(CancellationToken ct)
    {
        var me = await _graphClient.Me.GetAsync(cancellationToken: ct);
        return me;
    }
    
    public async Task<DriveCollectionResponse?> MyDrives(CancellationToken ct)
    {
        var drives = await _graphClient.Me.Drives.GetAsync(cancellationToken: ct);
        return drives;
    }

    public async Task<List<MicrosoftFolderInfo>> GetMyFolders(CancellationToken ct)
    {
        var meDrive = await _graphClient.Me.Drive.GetAsync(cancellationToken: ct);
        var allFolders = await GetAllFolders(meDrive, false, ct);
        return allFolders;
    }

    public async Task<List<MicrosoftFolderInfo>> GetFolders(string driveId, bool recursive, CancellationToken ct)
    {
        try
        {
            var drive = await _graphClient.Drives[driveId].GetAsync(cancellationToken: ct);
            var allFolders = await GetAllFolders(drive, recursive, ct);
            return allFolders;
        }
        catch (ODataError e)
        {
            return [];
        }
    }

    public async Task<MicrosoftSiteInfo[]?> GetAllSites(CancellationToken ct)
    {
        var sites = await _graphClient.Sites.GetAsync(cancellationToken: ct);
        return sites?.Value?.Select(s => new MicrosoftSiteInfo(s.Id, s.Name)).ToArray();
    }

    public async Task<List<MicrosoftSiteInfo>?> GetAllSitesWithDrives(CancellationToken ct)
    {
        var sites = await _graphClient.Sites.GetAsync(cancellationToken: ct);
        if (sites?.Value == null) return [];
        var siteList = new List<MicrosoftSiteInfo>();
        foreach (var s in sites.Value)
        {
            if (s == null) continue;
            var siteDrives = await _graphClient.Sites[s.Id].Drives.GetAsync(cancellationToken: ct);
            if (siteDrives?.Value == null || siteDrives.Value.Count == 0) continue;
            var st = new MicrosoftSiteInfo(s.Id, s.Name);
            foreach (var d in siteDrives.Value)
            {
                if (d == null) continue;
                st.Drives.Add(new MicrosoftDriveInfo(d.Id, d.Name));
            }
            siteList.Add(st);
        }

        return siteList;
    }


    public async Task<MicrosoftDriveInfo[]?> GetAllDrives(string siteId, CancellationToken ct)
    {
        var drives = await _graphClient.Sites[siteId].Drives.GetAsync(cancellationToken: ct);
        return drives?.Value?.Select(s => new MicrosoftDriveInfo(s.Id, s.Name)).ToArray();
    }

    public async Task<MicrosoftDriveInfo[]?> GetAllDrives(CancellationToken ct)
    {
        var drives = await _graphClient.Drives.GetAsync(cancellationToken: ct);
        return drives?.Value?.Select(s => new MicrosoftDriveInfo(s.Id, s.Name)).ToArray();
    }

    private async Task<List<MicrosoftFolderInfo>> GetAllFolders(Drive drive, bool recursive, CancellationToken ct)
    {
        List<MicrosoftFolderInfo> folders = [];

        await FetchFoldersRecursive(drive.Id!, "root", null, folders, recursive, ct);

        return folders;
    }

    private async Task FetchFoldersRecursive(string driveId, string itemId, string? parentFolderId,
        List<MicrosoftFolderInfo> folders, bool recursive, CancellationToken ct)
    {
        var children = await _graphClient.Drives[driveId].Items[itemId].Children.GetAsync(cancellationToken: ct);

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
    
    public async Task<bool> UploadFile(string driveId, string[] paths, string fileName, string fileContentType, Stream stream, CancellationToken ct)
    {
        if (paths.Length < 2) return false;

        try
        {
            await _graphClient.Drives[driveId].GetAsync(cancellationToken: ct);
        }
        catch (ODataError e)
        {
            return false;
        }

        var itemId = "root";

        for (var i = 2; i < paths.Length; i++)
        {
            var children = await _graphClient.Drives[driveId].Items[itemId].Children.GetAsync(cancellationToken: ct);
            if (children?.Value == null) return false;
            var it = children.Value.FirstOrDefault(x => x.Name == paths[i]);
            if (it == null) return false;
            itemId = it.Id;
        }
        if (itemId == null) return false;
        await _graphClient.Drives[driveId].Items[itemId].ItemWithPath(fileName).Content.PutAsync(stream, cancellationToken: ct);
        
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
