using System.Collections.Concurrent;

namespace DartWing.Web.Frappe;

public sealed class FrappeSiteMemoryStorage : IFrappeSiteStorage
{
    private readonly ConcurrentDictionary<string, FrappeSiteJob> _jobs = new();
    private readonly ConcurrentDictionary<string, FrappeSiteJob> _finishedJobs = new();
    
    public void AddSiteJob(FrappeSiteJob job)
    {
        _jobs.TryAdd(job.SiteHost, job);
    }

    public List<FrappeSiteJob> GetSiteJobs()
    {
        return _jobs.Values.ToList();
    }

    public FrappeSiteJob? GetFinishedJob(string siteHost)
    {
        return _finishedJobs.GetValueOrDefault(siteHost);
    }
    
    public void RemoveSiteJob(string siteHost)
    {
        if (_jobs.TryRemove(siteHost, out var job))
            _finishedJobs.TryAdd(siteHost, job);
    }
}