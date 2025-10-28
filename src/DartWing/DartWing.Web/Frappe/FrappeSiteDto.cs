namespace DartWing.Web.Frappe;

public sealed class FrappeSiteRequest
{
    public string SiteName { get; set; }
    public string CompanyName { get; set; }
}

public sealed class FrappeSiteResponse
{
    public string JobName { get; set; }
}

public sealed class FrappeSiteStatusResponse
{
    public string Status { get; set; }
}