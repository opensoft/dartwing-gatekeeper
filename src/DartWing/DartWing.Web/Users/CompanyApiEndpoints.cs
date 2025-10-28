using System.Diagnostics;
using System.Text.Json;
using DartWing.DomainModel.Extensions;
using DartWing.Frappe.Erp;
using DartWing.KeyCloak;
using DartWing.KeyCloak.Dto;
using DartWing.Microsoft;
using DartWing.Web.Auth;
using DartWing.Web.Frappe;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static class CompanyApiEndpoints
{
    public static void RegisterCompanyApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/company/{alias}").WithTags("Company").RequireAuthorization();

        group.MapGet("address", async (
            string alias,
            [FromServices] ILogger<Program> logger,
            [FromServices] KeyCloakProvider keyCloakHelper,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken ct) =>
        {
            var isUser = !httpContextAccessor.IsClientToken();
            var org = isUser ? await keyCloakHelper.GetUserOrganization(httpContextAccessor.GetUserId()!, alias, ct)
                    : await keyCloakHelper.GetOrganization(alias, ct);
            if (org == null) return Results.NotFound();
            var address = org.GetAddress();
            return Results.Ok(new CompanyAddress
            {
                Country = address.Country, City = address.City, Street = address.Street, State = address.State,
                PostalCode = address.Zip, BusinessName = address.Name
            });
        }).WithName("GetCompanyAddress").WithSummary("Get company address").Produces<CompanyAddress>();
        
        group.MapPost("address", async (
                string alias,
                [FromBody] CompanyAddress address,
                [FromServices] ILogger<Program> logger,
                [FromServices] KeyCloakProvider keyCloakHelper,
                [FromServices] IHttpContextAccessor httpContextAccessor,
                CancellationToken ct) =>
            {
                var org = await keyCloakHelper.GetUserOrganization(httpContextAccessor.GetUserId()!, alias, ct);
                if (org == null) return Results.NotFound();
                org.AddAddress(new KeyCloakOrganizationAddress
                {
                    City = address.City, Zip = address.PostalCode, Country = address.Country, Street = address.Street,
                    State = address.State, Name = address.BusinessName
                });
                await keyCloakHelper.UpdateOrganization(org, ct);
                return Results.Ok();
            }).WithName("SetCompanyAddress").WithSummary("Set company address")
            .RequireAuthorization(AuthConstants.AdminPolicy);
        
        group.MapGet("path", async (
                string alias,
                [FromServices] ILogger<Program> logger,
                [FromServices] KeyCloakProvider keyCloakHelper,
                [FromServices] IHttpContextAccessor httpContextAccessor,
                CancellationToken ct) =>
            {
                var org = await keyCloakHelper.GetUserOrganization(httpContextAccessor.GetUserId()!, alias, ct);
                if (org == null) return Results.NotFound();
                var pathString = org.GetPath();
                if (string.IsNullOrEmpty(pathString)) return Results.Ok(new CompanyPath());
                var path = JsonSerializer.Deserialize<MicrosoftGraphApiDriveIdFolderId>(pathString);
                return Results.Ok(new CompanyPath {DriveId = path?.DriveId, FolderId = path?.FolderId});
            }).WithName("GetCompanyPath").WithSummary("Get company path").Produces<CompanyPath>()
            .RequireAuthorization(AuthConstants.UserPolicy);
        
        group.MapPost("path", async (
                string alias,
                [FromBody] CompanyPath path,
                [FromServices] ILogger<Program> logger,
                [FromServices] KeyCloakProvider keyCloakHelper,
                [FromServices] IHttpContextAccessor httpContextAccessor,
                CancellationToken ct) =>
            {
                var org = await keyCloakHelper.GetUserOrganization(httpContextAccessor.GetUserId()!, alias, ct);
                if (org == null) return Results.NotFound();
                org.AddPath(JsonSerializer.Serialize(new MicrosoftGraphApiDriveIdFolderId
                    { FolderId = path.FolderId, DriveId = path.DriveId }));
                await keyCloakHelper.UpdateOrganization(org, ct);
                return Results.Ok();
            }).WithName("SetCompanyPath").WithSummary("Set company path")
            .RequireAuthorization(AuthConstants.UserPolicy);
        
        endpoints.MapPost("api/company", async (
            [FromBody] CompanyCreateRequest company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakHelper,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            logger.LogInformation("Create/Update company {id} {name}", company.Id, company.Name);
            var userId = httpContextAccessor.GetUserId();
            var userEmail = httpContextAccessor.GetEmail();
            if (userId == null || userEmail == null) return Results.BadRequest("User email is null");
            var provider = httpContextAccessor.HttpContext!.RequestServices;
            var isCreate = string.IsNullOrWhiteSpace(company.Id);
            var keyCloakCompany = isCreate ? null : await keyCloakHelper.GetOrganization(company.SiteHost, company.Name, ct);
            var erpCompanyService = isCreate
                ? null
                : await provider.GetFrappeCompanyService(company.Id);
            var c = isCreate || erpCompanyService == null ? null : await erpCompanyService.Get(company.Id, ct);
            if (isCreate)
            {
                var siteUrl = "https://" + company.SiteHost;
                var siteHost = company.SiteHost;

                var alias = string.IsNullOrWhiteSpace(company.Alias) ? GlobalExtensions.RandomString(4) : company.Alias;
                keyCloakCompany = new()
                {
                    Name = KeyCloakExtension.CompanyName(siteHost, company.Name),
                    Enabled = true,
                    Alias = alias,
                    Domains = [new KeyCloakOrganizationDomain { Name = alias + ".com" }],
                };

                keyCloakCompany.AddSite(siteUrl).AddCompanyOwner(userEmail)
                    .AddUserPermissions(userEmail, AuthConstants.LedgerCompanyAllPermissions)
                    .AddAddress(new KeyCloakOrganizationAddress
                    {
                        City = company.Address.City, Country = company.Address.Country, Street = company.Address.Street, State = company.Address.State,
                        Zip = company.Address.PostalCode, Name = company.Address.BusinessName
                    });

                var keyCloakOrgId = await keyCloakHelper.CreateOrganizationWithPermissions(keyCloakCompany, userId, ct);
                if (string.IsNullOrEmpty(keyCloakOrgId))
                    return Results.BadRequest("KeyCloak organization creation failed");
                keyCloakCompany.Id = keyCloakOrgId;

                FrappeCompanyDto cDto = new()
                {
                    Name = company.Name,
                    Abbr = keyCloakCompany.Id,
                    DefaultCurrency = company.Currency,
                    Domain = company.Domain,
                    Country = company.Address.Country,
                    CustomType = company.CompanyType,
                    CustomMicrosoftSharepointFolderPath = company.MicrosoftSharepointFolderPath,
                    CustomMicrosoftTenantName = "farheap",
                    CustomMicrosoftTenantId = "96d3fa6b-5547-49ca-9af1-dba9bec50c2b",
                    CustomInvoicesWhitelist = string.Join(',', company.InvoicesWhitelist)
                };

                erpCompanyService = await provider.GetFrappeCompanyService(site: siteUrl);
                var erpCompany = await erpCompanyService!.CreateAndValidate(cDto, userEmail, ct);
                if (erpCompany == null) return Results.BadRequest("Frappe organization creation failed");
                CompanyResponse crResponse = new(erpCompany);
                
                logger.LogInformation("Create company OK {id} {s} {name} {sw}", company.Id, company.SiteHost, company.Name, sw.Sw());
                return Results.Ok(crResponse);
            }

            if (c == null || keyCloakCompany == null) return Results.BadRequest("Company update failed");

            var admin = keyCloakCompany.IsAdmin(userEmail, AuthConstants.LedgerCompanyAdminPermission);
            if (!admin) return Results.Forbid();
            erpCompanyService = await provider.GetFrappeCompanyService(keyCloakCompany.Id);
            FrappeCompanyDto updateDto = new(c)
            {
                IsEnabled = company.IsEnabled,
                CustomMicrosoftSharepointFolderPath = company.MicrosoftSharepointFolderPath,
                Domain = company.Domain,
                DefaultCurrency = company.Currency,
                CustomType = company.CompanyType,
                Country = company.Address.Country,
                CustomFullName = company.Name,
                CustomInvoicesWhitelist = string.Join(',', company.InvoicesWhitelist)
            };
            if (c.CustomMicrosoftSharepointFolderPath != company.MicrosoftSharepointFolderPath)
                updateDto.CustomMicrosoftSharepointUserPath = null;

            var erpUpdCompany = await erpCompanyService.Update(c.Name, updateDto, ct);
            CompanyResponse response = new(erpUpdCompany)
            {
                CompanyAlias = keyCloakCompany.Alias
            };

            logger.LogInformation("Update company OK {id} {name} {sw}", company.Id, company.Name, sw.Sw());
            
            return Results.Ok(response);
        }).WithName("CreateOrUpdateCompany").WithSummary("Create or Update company").Produces<CompanyResponse>().RequireAuthorization();


        group.MapGet("", async (
                string alias,
                [FromServices] ILogger<Program> logger,
                [FromServices] Task<IFrappeCompany?> frappeCompanyTask,
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                var c = await frappeCompanyTask;
                if (c == null)
                {
                    logger.LogWarning("Get company {companyName} {sw}", alias, sw.Sw());
                    return Results.NotFound("Company not found");
                }
                CompanyResponse crResponse = new(c);
                logger.LogInformation("Get company {uId} {email}: OK {sw}", c.Name, c.Abbr, sw.Sw());
                return Results.Ok(crResponse);
            }).WithName("Company").WithSummary("Get company").Produces<CompanyResponse>()
            .RequireAuthorization(AuthConstants.UserPolicy);

        
        group.MapGet("providers", async (
                string alias,
                [FromServices] ILogger<Program> logger,
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get company providers {companyName}", alias);
                CompanyProvidersResponse crResponse = new()
                {
                    Providers =
                    [
                        new CompanyProviderResponse { Name = "Microsoft SharePoint", Alias = "microsoft-sharepoint" },
                        //new CompanyProviderResponse { Name = "Google Drive", Alias = "google2" }
                    ]
                };
                logger.LogInformation("Get company providers {uId}: OK {sw}", alias, Stopwatch.GetElapsedTime(sw));
                return Results.Ok(crResponse);
            }).WithName("CompanyProviders").WithSummary("Get company providers").Produces<CompanyProvidersResponse>()
            .RequireAuthorization(AuthConstants.UserPolicy);

        group.MapGet("user", async (
                string alias,
                [FromServices] ILogger<Program> logger,
                [FromServices] IHttpContextAccessor httpContextAccessor,
                [FromServices] KeyCloakProvider keyCloakHelper,
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get users for company {c}", alias);
                var userEmail = httpContextAccessor.GetEmail();
                if (userEmail == null) return Results.BadRequest("User email is null");

                var org = await keyCloakHelper.GetOrganization(alias, ct);
                var keyCloakUsers = await keyCloakHelper.GetOrganizationUsers(org!, ct);

                if (keyCloakUsers.Count == 0)
                {
                    logger.LogWarning("Get users for company {uId} by {usr} failed {sw}", alias, userEmail, sw.Sw());
                    return Results.Conflict();
                }

                logger.LogInformation("Get users for company {uId} by {usr} OK {cnt} {sw}", alias, userEmail,
                    keyCloakUsers.Count, sw.Sw());
                UsersResponse response = new()
                {
                    Users = keyCloakUsers.Select(x => x.Email).Where(x => !string.IsNullOrEmpty(x)).ToArray()
                };

                logger.LogInformation("Get users for company {c} by {em} count={cnt} {sw}", alias, userEmail,
                    response.Users.Length, sw.Sw());
                
                return Results.Ok(response);
            }).WithName("CompanyUsers").WithSummary("Get users for company").Produces<UsersResponse>().RequireAuthorization();
    }
}