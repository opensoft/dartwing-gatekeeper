using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using DartWing.DomainModel.Extensions;
using DartWing.DomainModel.Helpers;
using DartWing.Frappe.Erp;
using DartWing.Frappe.Models;
using DartWing.KeyCloak;
using DartWing.KeyCloak.Dto;
using DartWing.Microsoft;
using DartWing.Web.Auth;
using DartWing.Web.Users.Dto;

namespace DartWing.Web.Frappe;

public sealed class FrappeSiteService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    private readonly ILogger<FrappeSiteService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly KeyCloakProvider _keyCloakProvider;
    private readonly KeyCloakSettings _kyeCloakSettings;
    private readonly HttpClient _httpClient;
    private readonly FrappeSiteSettings _frappeSiteSettings;
    private readonly IFrappeSiteStorage _frappeSiteStorage;
    private readonly AzureStorageAdapter _azureStorageAdapter;

    public FrappeSiteService(ILogger<FrappeSiteService> logger,
        IServiceProvider serviceProvider,
        KeyCloakProvider keyCloakProvider,
        KeyCloakSettings kyeCloakSettings,
        HttpClient httpClient,
        FrappeSiteSettings frappeSiteSettings,
        IFrappeSiteStorage frappeSiteStorage,
        AzureStorageAdapter azureStorageAdapter)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _keyCloakProvider = keyCloakProvider;
        _kyeCloakSettings = kyeCloakSettings;
        _httpClient = httpClient;
        _frappeSiteSettings = frappeSiteSettings;
        _frappeSiteStorage = frappeSiteStorage;
        _azureStorageAdapter = azureStorageAdapter;
    }

    public async Task<string> CreateSite(FrappeSiteJob job, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        _logger.LogInformation("Creating site {s}", job.SiteHost);
        var user = await _keyCloakProvider.GetDefaultUser(ct);
        await _keyCloakProvider.AddFrappeKeys(user!.Id, GenerateFrappeSecret(job.SiteHost), ct);
        if (_frappeSiteSettings.IsActive)
        {
            using var message = await _httpClient.PostAsJsonAsync("",
                new FrappeSiteRequest { SiteName = job.SiteHost, CompanyName = job.CompanyName }, Options, ct);
            if (!message.IsSuccessStatusCode)
            {
                var respString = await message.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Frappe site creation failed {site} response={r} {sw}", job.SiteHost, respString,
                    sw.Sw());
                return "";
            }

            var response = await message.Content.ReadFromJsonAsync<FrappeSiteResponse>(Options, ct);
            if (string.IsNullOrEmpty(response?.JobName))
            {
                _logger.LogWarning("Frappe site creation failed {site} job name is empty {sw}", job.SiteHost, sw.Sw());
                return "";
            }

            job.SiteJobId = response.JobName;
        }

        job.IsActive = _frappeSiteSettings.IsActive;
        _frappeSiteStorage.AddSiteJob(job);

        _logger.LogInformation("Frappe site creation OK {site} jobName={j} company={comp} by {user} {sw}", job.SiteHost,
            job.SiteJobId, job.CompanyName, job.UserEmail, sw.Sw());
        return job.SiteHost;
    }

    public async Task<bool> DeleteSite(string siteName, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var host = _frappeSiteSettings.GetSiteHost(siteName);
        _logger.LogInformation("Deleting site {s}", host);
        using HttpRequestMessage m = new()
        {
            Method = HttpMethod.Delete,
            Content = JsonContent.Create(new FrappeSiteRequest { SiteName = host }, options: Options),
        };
        using var message = await _httpClient.SendAsync(m, ct);
        if (!message.IsSuccessStatusCode)
        {
            var respString = await message.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Frappe site delete failed {site} response={r} {sw}", host, respString, sw.Sw());
            return false;
        }

        var user = await _keyCloakProvider.GetDefaultUser(ct);
        var fullSecret = user!.GetFrappeFullSecret(host);
        if (!string.IsNullOrEmpty(fullSecret)) await _keyCloakProvider.RemoveFrappeKeys(user!.Id, fullSecret, ct);
        var orgs = await _keyCloakProvider.GetAllOrganizations(ct);
        foreach (var org in orgs.Where(x => x.Name.StartsWith(host)))
        {
            await _keyCloakProvider.DeleteOrganization(org, ct);
        }

        var clients = await _keyCloakProvider.GetClients(ct);
        var client = clients.FirstOrDefault(x => x.ClientId == host);
        if (client != null) await _keyCloakProvider.DeleteClient(client.Id, ct);
        _logger.LogInformation("Frappe site delete OK {site} {sw}", host, sw.Sw());
        return true;
    }

    public FrappeSiteJob? GetLocalSiteStatus(string siteHost)
    {
        return _frappeSiteStorage.GetFinishedJob(siteHost);
    }

    private const string JobIdPrefix = "create-frappe-site-";
    public async Task<string?> GetSiteStatus(FrappeSiteJob siteJob, CancellationToken ct)
    {
        if (!siteJob.IsActive) return nameof(EFrappeJobSiteStatus.Succeeded);
        var siteJobId = siteJob.SiteJobId;
        if (siteJob.SiteJobId.StartsWith(JobIdPrefix)) siteJobId = siteJobId[JobIdPrefix.Length..];
        var uri = _httpClient.BaseAddress!.ToString().TrimEnd('/') + '/' + siteJobId;
        using var message = await _httpClient.GetAsync(uri, ct);
        if (!message.IsSuccessStatusCode)
        {
            var respString = await message.Content.ReadAsStringAsync(ct);

            _logger.LogWarning("Get status site job id failed {uri} jobId={j} code={c} {body}",
                message.RequestMessage?.RequestUri, siteJobId, message.StatusCode, respString);
            return null;
        }

        var response = await message.Content.ReadFromJsonAsync<FrappeSiteStatusResponse>(Options, ct);
        return response?.Status;
    }

    public async Task OnSiteCreated(FrappeSiteJob siteJob, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        _logger.LogInformation("OnSiteCreated site: {siteUrl}", siteJob.SiteHost);
        var siteUrl =  $"https://{siteJob.SiteHost.TrimEnd('/')}";
        var user = await _keyCloakProvider.GetUserById(siteJob.UserId, ct);
        if (user == null) return;
        var allCompanies = await _keyCloakProvider.GetAllOrganizations(ct);
        var hs = allCompanies.Select(x => x.Alias).ToImmutableHashSet();
        var company = siteJob.Request.Company;
        var alias = string.IsNullOrWhiteSpace(company.Alias)
            ? AbbreviationHelper.GenerateUniqueAbbreviation(siteJob.CompanyName, hs)
            : company.Alias;
        
        KeyCloakOrganization keyCloakCompany = new()
        {
            Name = KeyCloakExtension.CompanyName(siteJob.SiteHost, siteJob.CompanyName),
            Enabled = true,
            Alias = alias,
            Domains = [new KeyCloakOrganizationDomain { Name = alias + ".com" }],
        };

        var addr = company.Address;
        keyCloakCompany.AddSite(siteUrl)
            .AddCompanyOwner(siteJob.UserEmail)
            .AddMsTenantId(siteJob.MsTenantId)
            .AddCreatedDate(DateTimeOffset.UtcNow)
            .AddUserPermissions(siteJob.UserEmail, AuthConstants.LedgerCompanyAllPermissions)
            .AddStorageType(siteJob.StorageType.ToString())
            .AddAddress(new KeyCloakOrganizationAddress
            {
                City = addr.City, Country = addr.Country, Street = addr.Street, State = addr.State,
                Zip = addr.PostalCode, Name = addr.BusinessName
            });

        var orgId = await _keyCloakProvider.CreateOrganizationWithPermissions(keyCloakCompany, user.Id, ct);
        
        if (siteJob.IsActive)
        {
            var userService = await _serviceProvider.GetFrappeServiceForBackground<FrappeUserService>(siteUrl, ct);
            var uDto = new FrappeUserData
            {
                Email = siteJob.UserEmail,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = _frappeSiteSettings.OwnerRoles.Select(x => new ErpUserRoleDto(x)).ToList()
            };
            var userDto = await userService!.CreateUser(uDto, ct);
        }

        if (siteJob.StorageType == EDocumentUploadMethod.LedgerInternal)
        {
            await _azureStorageAdapter.CreateContainer(alias, ct);
        }

        siteJob.CompanyAlias = keyCloakCompany.Alias;
        siteJob.Status = EFrappeSiteStatus.Finished;

        _logger.LogInformation("OnSiteCreated site: {siteUrl} OK {sw} {totalSw}", siteUrl, sw.Sw(),
            DateTime.UtcNow - siteJob.StartTime);
    }

    private static string GenerateFrappeSecret(string host)
    {
        return $"{host}={GlobalExtensions.RandomString()}:{GlobalExtensions.RandomString()}";
    }
}