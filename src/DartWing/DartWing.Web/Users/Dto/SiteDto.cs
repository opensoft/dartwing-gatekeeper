using DartWing.Frappe.Models;

namespace DartWing.Web.Users.Dto;

public sealed class SiteCreateRequest
{
    public string SiteName { get; set; }
    
    public CompanyCreateRequest Company { get; set; }
    
    public string Abbreviation { get; set; }
    public string Currency { get; set; }
    public string Domain { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string CompanyType { get; set; } = "Company";
    public EDocumentUploadMethod DocumentUploadMethod { get; set; } = EDocumentUploadMethod.LedgerInternal;
    
}

public sealed class SiteCreateResponse
{
    public string Site { get; set; }
}

public sealed class SiteStatus
{
    public EFrappeSiteStatus Status { get; set; }
    public string CompanyAlias { get; set; }
}

public enum EDocumentUploadMethod
{ 
    /// <summary>
    /// Do not use the file upload option
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Upload files directly to MSBC. Requires active MSBC integrations
    /// </summary>
    MSBC = 1,
    
    /// <summary>
    /// Upload files to using Dartwing SharePoint integration. Requires active SharePoint and MSBC integrations
    /// </summary>
    Sharepoint = 2,
    
    /// <summary>
    /// Upload files to using Dartwing Azure Storage integration
    /// </summary>
    LedgerInternal = 3
}