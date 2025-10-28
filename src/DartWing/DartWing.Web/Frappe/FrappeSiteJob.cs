using DartWing.Frappe.Models;
using DartWing.Web.Users.Dto;

namespace DartWing.Web.Frappe;

public sealed class FrappeSiteJob
{
    public string SiteHost { get; set; }
    public string SiteJobId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public string CompanyName { get; set; }
    public string UserEmail { get; set; }
    public string UserId { get; set; }
    public string Currency { get; set; }
    public string Domain { get; set; }
    public string Country { get; set; }
    public bool IsActive { get; set; }
    public string MsTenantId { get; set; }
    public SiteCreateRequest Request { get; set; }
    public string CompanyAlias { get; set; }

    public string GetUrl() => $"https://{SiteHost}";
    
    public EFrappeSiteStatus Status { get; set; } = EFrappeSiteStatus.InProgress;
    public EDocumentUploadMethod StorageType { get; set; }
}