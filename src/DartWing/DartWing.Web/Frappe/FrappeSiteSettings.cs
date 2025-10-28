namespace DartWing.Web.Frappe;

public sealed class FrappeSiteSettings
{
    public string SetupUrl { get; set; }
    public int TimeoutSec { get; set; }
    public string BaseSiteHost { get; set; }

    public string GetSiteHost(string siteName) => $"{siteName}.{BaseSiteHost}";

    public string[] OwnerRoles { get; set; } = [];
    public bool IsActive { get; set; }
}