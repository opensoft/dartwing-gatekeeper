using DartWing.Frappe.Models;

namespace DartWing.Web.Frappe;

public sealed class FrappeSiteHostedService : BackgroundService
{
    private readonly ILogger<FrappeSiteHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFrappeSiteStorage _storage;

    public FrappeSiteHostedService(ILogger<FrappeSiteHostedService> logger, IServiceProvider serviceProvider, IFrappeSiteStorage storage)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _storage = storage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FrappeSiteHostedService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(16000, stoppingToken);
                var siteJobs = _storage.GetSiteJobs();
                if (siteJobs.Count == 0) continue;

                var siteService = _serviceProvider.GetService<FrappeSiteService>()!;
                foreach (var siteJob in siteJobs)
                {
                    var status = await GetSiteJobStatus(siteService, siteJob, stoppingToken);
                    var sw = DateTime.UtcNow - siteJob.StartTime;
                    switch (status)
                    {
                        case EFrappeJobSiteStatus.Succeeded:
                        {
                            _logger.LogInformation("FrappeSiteHostedService site created {s} {j} {sw}",
                                siteJob.SiteHost, siteJob.SiteJobId, sw);
                            await siteService.OnSiteCreated(siteJob, stoppingToken);
                            _storage.RemoveSiteJob(siteJob.SiteHost);
                        } break;
                        case EFrappeJobSiteStatus.Failed:
                        {
                            _logger.LogWarning("FrappeSiteHostedService failed to create site {s} {j} {sw}",
                                siteJob.SiteHost, siteJob.SiteJobId, sw);
                            siteJob.Status = EFrappeSiteStatus.Failed;
                            _storage.RemoveSiteJob(siteJob.SiteHost);
                        } break;
                        default:
                        {
                            if (sw.TotalMinutes > 30)
                            {
                                _logger.LogWarning(
                                    "FrappeSiteHostedService failed to create site - timeout {s} {j} {sw}",
                                    siteJob.SiteHost, siteJob.SiteJobId, sw);
                                _storage.RemoveSiteJob(siteJob.SiteHost);
                            }
                        } break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "FrappeSiteHostedService error");
                await Task.Delay(60000, stoppingToken);
            }
        }

        _logger.LogInformation("FrappeSiteHostedService stopped");
    }

    private static async Task<EFrappeJobSiteStatus> GetSiteJobStatus(FrappeSiteService siteService,
        FrappeSiteJob siteJob, CancellationToken stoppingToken)
    {
        var status = await siteService.GetSiteStatus(siteJob, stoppingToken);

        return Enum.TryParse(status, true, out EFrappeJobSiteStatus siteStatus)
            ? siteStatus
            : EFrappeJobSiteStatus.Unknown;
    }
}