namespace DartWing.Web.Files.Dto;

public sealed class CdFolderResponse
{
    public CdFolderResponse() {}

    public CdFolderResponse(string redirectUrl) { RedirectUrl = redirectUrl; }

    public CdFolder[] Folders { get; set; }
    public string RedirectUrl { get; set; }
}

public sealed class CdFolder
{
    
}