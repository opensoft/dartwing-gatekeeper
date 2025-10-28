using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DartWing.DomainModel.Extensions;
using DartWing.DomainModel.Helpers;
using DartWing.Frappe.Models;
using DartWing.KeyCloak;
using DartWing.Web.Auth;
using DartWing.Web.Frappe;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static partial class SiteApiEndpoints
{
    public static void RegisterSiteApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/site/").WithTags("Site").RequireAuthorization();

        group.MapPost("", async ([FromBody] SiteCreateRequest site,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] FrappeSiteService frappeSiteService,
            [FromServices] FrappeSiteSettings frappeSiteSettings,
            [FromServices] KeyCloakTokenProvider kcTokenProvider,
            [FromServices] KeyCloakProvider kcProvider,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(site.SiteName))
            {
                var allCompanies = await kcProvider.GetAllOrganizations(ct);
                var hs = allCompanies.Select(x => x.GetSite())
                    .Select(x => x.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToImmutableHashSet();
                site.SiteName = AbbreviationHelper.GenerateUniqueAbbreviation(site.Company.Name, hs);
            }

            if (string.IsNullOrEmpty(site.SiteName) || site.SiteName.Length > 32 || !MyRegex().IsMatch(site.SiteName))
                return Results.BadRequest("Invalid site name");
            
            var userEmail = httpContextAccessor.GetEmail()!;
            FrappeSiteJob job = new()
            {
                UserEmail = userEmail,
                UserId = httpContextAccessor.GetUserId()!,
                CompanyName = site.Company.Name,
                SiteHost = frappeSiteSettings.GetSiteHost(site.SiteName),
                Request = site,
                StorageType = site.DocumentUploadMethod
            };
            
            var existCompany = await kcProvider.GetOrganization(job.SiteHost, job.CompanyName, ct);
            if (existCompany != null) return Results.Ok(new SiteCreateResponse{Site = job.SiteHost});
            
            logger.LogInformation("Create site {id} company {c} by {u}", site.SiteName, site.Company.Name, userEmail);
            var msToken = await kcTokenProvider.TokenExchangeProviderAccessToken(userEmail, "microsoft", false, ct);
            job.Domain = "https://" + job.SiteHost;
            job.MsTenantId = AuthExtension.GetTenantId(msToken?.AccessToken) ?? "";
            var result = await frappeSiteService.CreateSite(job, CancellationToken.None);
            return string.IsNullOrEmpty(result)
                ? Results.Conflict("Error occured, try again later")
                : Results.Ok(new SiteCreateResponse{Site = job.SiteHost});
        }).WithName("CreateSite").WithSummary("Create new site").Produces<SiteCreateResponse>();
        
        group.MapDelete("{site}", async (
            string site,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] FrappeSiteService frappeSiteService,
            CancellationToken ct) =>
        {
            var userEmail = httpContextAccessor.GetEmail();
            logger.LogInformation("Delete site {id} by {u}", site, userEmail); 
            await frappeSiteService.DeleteSite(site, CancellationToken.None);
            return Results.Ok("Site will be deleted");
        }).WithName("DeleteSite").WithSummary("Delete site").RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapGet("{site}", async (
            string site,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] FrappeSiteService frappeSiteService,
            [FromServices] FrappeSiteSettings frappeSiteSettings,
            CancellationToken ct) =>
        {
            var userEmail = httpContextAccessor.GetEmail();
            logger.LogInformation("Get site status {id} by {u}", site, userEmail); 
            var siteJob = frappeSiteService.GetLocalSiteStatus(site);
            var status = siteJob?.Status ?? EFrappeSiteStatus.InProgress;
            return Results.Ok(new SiteStatus{Status = status, CompanyAlias = siteJob?.CompanyAlias});
        }).WithName("GetSiteStatus").WithSummary("Get site status").Produces<SiteStatus>().RequireAuthorization(AuthConstants.AdminPolicy);
    }

    [GeneratedRegex(@"^[A-Za-z0-9 \-]+$")]
    private static partial Regex MyRegex();
}