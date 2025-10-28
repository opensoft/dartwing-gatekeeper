namespace DartWing.DomainModel.Files;

public interface IFileInfo
{
    public string Id { get; }
    public string Name { get; }
    public string ParentId { get; set; }
    public EFileType FileType { get; set; }
    public string Owner { get; set; }
    public DateTime Modified { get; set; }
    public DateTime Creation { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; }
    public bool IsPublic { get; set; }
    public string WebLink { get; set; }
}