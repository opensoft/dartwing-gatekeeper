using System.Diagnostics;
using System.Net.Http.Json;
using DartWing.DomainModel;
using DartWing.DomainModel.Extensions;
using DartWing.DomainModel.Files;
using DartWing.Frappe.Drive.Dto;
using DartWing.Frappe.Erp;
using DartWing.Frappe.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Drive;

public sealed class FrappeDriveService : IFileSystemProvider
{
    private readonly ILogger<FrappeDriveService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;

    public FrappeDriveService(ILogger<FrappeDriveService> logger, HttpClient httpClient, IMemoryCache memoryCache)
    {
        _logger = logger;
        _httpClient = httpClient;
        _memoryCache = memoryCache;
    }

    private async Task<bool> UploadFile(string path, Stream fileStream, string fileName, CancellationToken ct)
    {
        const string url = "/api/method/drive.api.files.upload_file";
        using MultipartFormDataContent form = new();
        form.Add(new StreamContent(fileStream), "file", fileName);
        if (!string.IsNullOrEmpty(path) && path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length != 0)
            form.Add(new StringContent(await GetFolderId(path, ct)), "parent");

        using var response = await _httpClient.PostAsync(url, form, ct);
        return response.IsSuccessStatusCode;
    }
    
   
    private async Task<FrappeFileModel[]> GetFilesPrivate(string parentId, EFileType fileType, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var urlFiles =
            $"/api/method/drive.api.list.files?entity_name={parentId}&order_by=title&offset=0&limit=250&folders_first=true&is_active=1&recents_only=false&favourites_only=false&file_kind_list=%5B%5D&tag_list=%5B%5D";
        using var filesMessage =  await _httpClient.GetAsync(urlFiles, ct);
        var files = await filesMessage.HandleResponse<DriveResponseDto<DriveFile[]>>(sw, _logger, ct);

        if (files?.Data == null) return [];
        var includeFolders = fileType.HasFlag(EFileType.Folder) ? 1 : 0;
        return files.Data
            .WhereIf(fileType.HasFlag(EFileType.Folder) != fileType.HasFlag(EFileType.File),
                x => x.IsGroup == includeFolders).Select(x => new FrappeFileModel(x)
                { WebLink = $"{_httpClient.BaseAddress?.AbsoluteUri}/drive/file/{x.Name}" }).ToArray();
    }
    
    private async Task<string> GetFolderId(string path, CancellationToken ct)
    {
        var parentId = await GetHomeFolderId(ct);
        if (string.IsNullOrEmpty(path) || path == "/") return parentId;
        var paths = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in paths)
        {
            var folders = await GetFilesPrivate(parentId, EFileType.Folder, ct);
            var f = folders.FirstOrDefault(x => x.Name == p);
            if (f == null) return "";
            parentId = f.Id;
        }
        return parentId;
    }

    private async Task<string> GetHomeFolderId(CancellationToken ct)
    {
        const string url = "/api/method/drive.api.files.get_home_folder_id";
        var response = await _httpClient.GetFromJsonAsync<DriveMessageDto<string>>(url, ct);
        return response?.Message ?? "";
    }

    public async Task<DartWingResponse<IFileInfo>> CreateFolder(string folderPath, string folderName, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        const string url = "/api/method/drive.api.files.create_folder";
        var folderId = IsRootPath(folderPath) ? "" : await GetFolderId(folderPath, ct);
        CreateDriveFolderRequest request = new () {Parent = folderId, Title = folderName};
        using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
        var responseEntity = await response.HandleResponse<DriveMessageDto<DriveEntityResponse>>(sw, _logger, ct);
        return responseEntity?.Message == null
            ? new DartWingResponse<IFileInfo>("Error while creating folder")
            : new DartWingResponse<IFileInfo>(new FrappeFileModel(responseEntity.Message)); 
    }

    public async Task<DartWingResponse<IFileInfo>> UploadFile(string folderPath, string fileName, Stream fileStream, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        const string url = "/api/method/drive.api.files.upload_file";
        using MultipartFormDataContent form = new();
        form.Add(new StreamContent(fileStream), "file", fileName);
        if (!IsRootPath(folderPath))
            form.Add(new StringContent(await GetFolderId(folderPath, ct)), "parent");

        using var response = await _httpClient.PostAsync(url, form, ct);
        var responseEntity = await response.HandleResponse<DriveMessageDto<DriveEntityResponse>>(sw, _logger, ct);
        return responseEntity?.Message == null
            ? new DartWingResponse<IFileInfo>("Error while uploading file")
            : new DartWingResponse<IFileInfo>(new FrappeFileModel(responseEntity.Message));
    }

    public Task<bool> IsFileExists(string folderPath, string fileOrFolderName, EFileType fileType, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<DartWingResponse<IFileInfo[]>> GetFiles(string path, EFileType fileType, CancellationToken ct)
    {
        var folderId = await GetFolderId(path, ct);
        var files = await GetFilesPrivate(folderId, fileType, ct);
        return new DartWingResponse<IFileInfo[]>(files);
    }

    private static bool IsRootPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var span = path.AsSpan().Trim().Trim('/');
        return span.Length == 0;
    }
}