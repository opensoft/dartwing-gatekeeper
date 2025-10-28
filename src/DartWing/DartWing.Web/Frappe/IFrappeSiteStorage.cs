namespace DartWing.Web.Frappe;

public interface IFrappeSiteStorage
{
    void AddSiteJob(FrappeSiteJob job);
    List<FrappeSiteJob> GetSiteJobs();
    void RemoveSiteJob(string siteHost);
    FrappeSiteJob? GetFinishedJob(string siteHost);
}