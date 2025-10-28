using DartWing.DomainModel.Files;
using DartWing.Frappe.Drive.Dto;

namespace DartWing.Frappe.Models;

public sealed class FrappeFileModel : IFileInfo
{
    public FrappeFileModel() { }
    public FrappeFileModel(DriveFile driveFile)
    {
        Id = driveFile.Name;
        Name = driveFile.Title;
        FileType = driveFile.IsGroup > 0 ? EFileType.Folder : EFileType.File;
        Modified = driveFile.Modified;
        Creation = driveFile.Creation;
        FileSize = driveFile.FileSize;
        MimeType = driveFile.MimeType;
        IsPublic = driveFile.Public > 0;
        ParentId = driveFile.ParentDriveEntity;
        Owner = driveFile.Owner;
    }
    
    public FrappeFileModel(DriveEntityResponse driveFile)
    {
        Id = driveFile.Name;
        Name = driveFile.Title;
        FileType = driveFile.IsGroup > 0 ? EFileType.Folder : EFileType.File;
        Modified = driveFile.Modified;
        Creation = driveFile.Creation;
        FileSize = driveFile.FileSize;
        MimeType = driveFile.MimeType;
        ParentId = driveFile.ParentDriveEntity;
        Owner = driveFile.Owner;
        WebLink = "https://" + driveFile.Path;
    }

    public string Id { get; set; }
    public string ParentId { get; set; }
    public EFileType FileType { get; set; }
    public string Owner { get; set; }
    public string Name { get; set; }
    public DateTime Modified { get; set; }
    public DateTime Creation { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; }
    public bool IsPublic { get; set; }
    public string WebLink { get; set; }
}