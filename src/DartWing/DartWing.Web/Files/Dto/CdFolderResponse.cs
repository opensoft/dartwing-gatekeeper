namespace DartWing.Web.Files.Dto;

public sealed class CdFolderResponse
{
    public CdFolderResponse() {}

    public CdFolderResponse(string redirectUrl) { RedirectUrl = redirectUrl; }

    public List<CdFolder> Folders { get; set; }
    public string RedirectUrl { get; set; }
}

public sealed class CdFolder
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ParentId { get; set; }
    public string Description { get; set; }
    public string FolderType { get; set; }
    public bool CanBeSelected { get; set; } = true;
    public string? DisplayName { get; set; }
    public DateTimeOffset? LastModifiedDateTime { get; set; }
}