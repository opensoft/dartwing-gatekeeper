namespace DartWing.DomainModel.Files;

public interface IFileSystemProvider
{
    Task<DartWingResponse<IFileInfo>> CreateFolder(string folderPath, string folderName, CancellationToken ct);
    Task<DartWingResponse<IFileInfo>> UploadFile(string folderPath, string fileName, Stream fileStream, CancellationToken ct);
    Task<bool> IsFileExists(string folderPath, string fileOrFolderName, EFileType fileType, CancellationToken ct);
    Task<DartWingResponse<IFileInfo[]>> GetFiles(string path, EFileType fileType, CancellationToken ct);
}