namespace DartWing.Frappe.Drive.Dto;

public sealed class DriveFile
{
    public string Name { get; set; }
    public string Title { get; set; }
    public int IsGroup { get; set; }
    public string Owner { get; set; }
    public string FullName { get; set; }
    public string UserImage { get; set; }
    public DateTime Modified { get; set; }
    public DateTime Creation { get; set; }
    public long FileSize { get; set; }
    public string FileKind { get; set; }
    public string FileExt { get; set; }
    public string Color { get; set; }
    public string Document { get; set; }
    public string MimeType { get; set; }
    public string ParentDriveEntity { get; set; }
    public int AllowDownload { get; set; }
    public int IsActive { get; set; }
    public int AllowComments { get; set; }
    public int Read { get; set; }
    public string UserName { get; set; }
    public string UserDoctype { get; set; }
    public int Write { get; set; }
    public int Public { get; set; }
    public int Everyone { get; set; }
    public int Share { get; set; }
    public string IsFavourite { get; set; }
}

public sealed class DriveEntityResponse
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int DocStatus { get; set; }
    public int Index { get; set; }
    public string Title { get; set; }
    public int Left { get; set; }
    public int Right { get; set; }
    public int IsGroup { get; set; }
    public string ParentDriveEntity { get; set; }
    public string Path { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; }
    public int Version { get; set; }
    public int IsActive { get; set; }
    public int AllowComments { get; set; }
    public int AllowDownload { get; set; }
    public string FileExtension { get; set; }
    public string FileKind { get; set; }
    public string DocType { get; set; }
    public List<string> Tags { get; set; }
}
