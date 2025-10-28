using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public interface IErpFileService : IFrappeBaseService 
{
    Task<FrappeFileDataDto?> Create(FrappeFileDataDto fileData, CancellationToken ct);
    Task<FrappeFileDataDto[]?> GetList(int? page = null, int? pageSize = null, CancellationToken ct = default);
    ValueTask<List<FrappeFileDataDto>?> GetAll(CancellationToken ct);
    ValueTask<FrappeFileDataDto?> Get(string name, CancellationToken ct);
    Task<FrappeFileDataDto?> Update(string name, FrappeFileDataDto request, CancellationToken ct);
    Task<bool> Delete(string name, CancellationToken ct);
}

public sealed class FrappeFileService : FrappeBaseService<FrappeFileDataDto>, IErpFileService
{
    public FrappeFileService(ILogger<FrappeFileService> logger, HttpClient httpClient, IMemoryCache memoryCache)
        : base(logger, httpClient, memoryCache)
    {
    }
    
    public override async Task<FrappeFileDataDto?> Create(FrappeFileDataDto fileData, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(fileData.Folder)) return await base.Create(fileData, ct);
        fileData.Folder = $"Home/{fileData.Folder}";
        if (!await CheckFolderExists(fileData.Folder, ct))
            await CreateFolder(fileData.Folder, ct);

        return await base.Create(fileData, ct);
    }
    
    private async Task<bool> CheckFolderExists(string folderPath, CancellationToken ct)
    {
        var response = await Get(folderPath, ct);
        //using var response = await _client.GetAsync($"/api/resource/File/{folderPath}", ct);
        return response != null;
    }
    
    private async Task<bool> CreateFolder(string folderPath, CancellationToken ct)
    {
        var indx = folderPath.LastIndexOf('/');
        var folderName = folderPath[(indx + 1)..];
        var parentFolder = folderPath[..indx];
        
        FrappeFileDataDto folderData = new()
        {
            FileName = folderName,
            IsFolder = 1,
            Folder = parentFolder
        };
        var response = await base.Create(folderData, ct);
        return response != null;
    }
}

public sealed class FrappeFileDataDto : IFrappeBaseDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string FileName { get; set; }
    public int IsPrivate { get; set; }
    public string FileType { get; set; }
    public int IsHomeFolder { get; set; }
    public int IsAttachmentsFolder { get; set; }
    public long FileSize { get; set; }
    public string FileUrl { get; set; }
    public string Folder { get; set; }
    public int IsFolder { get; set; }
    public string AttachedToName { get; set; }
    public string AttachedToField { get; set; }
    public int UploadedToDropbox { get; set; }
    public int UploadedToGoogleDrive { get; set; }
    public string Doctype { get; set; }
    public string? CustomAttachedToUser { get; set; }
    public string? CustomAttachedToCompany { get; set; }
    public string? AttachedToDoctype { get; set; }
}