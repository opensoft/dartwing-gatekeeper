using System.Diagnostics;
using System.Text.Json;
using DartWing.DomainModel.Extensions;
using DartWing.Frappe.Erp;
using DartWing.KeyCloak;
using DartWing.KeyCloak.Dto;
using DartWing.Microsoft;
using DartWing.Web.Auth;
using DartWing.Web.Files.Dto;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DartWing.Web.Files;

public static class FilesApiEndpoints
{
    private static string FileUrl(string host, string alias, string fileName) =>
        $"https://{host}/api/file/{alias}/link/{fileName}";
    private static string RedirectUrl(string host, string alias, string fileName) =>
        $"https://{host}/api/file/{alias}/authlink/{fileName}";
    
    public static void RegisterFolderApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/file/{alias}/link/{fileName}", (
            [FromRoute] string alias,
            [FromRoute] string fileName,
            [FromServices] KeyCloakSettings keyCloakSettings,
            [FromServices] IHttpContextAccessor httpContextAccessor) =>
        {
            var redirectUrl = RedirectUrl(httpContextAccessor.HttpContext!.Request.Host.Value!, alias, fileName);
            return Results.Redirect(keyCloakSettings.GetRegularAuthUrl(redirectUrl));
        });
        
        endpoints.MapGet("api/file/{alias}/authlink/{fileName}", async (
            [FromRoute] string alias,
            [FromRoute] string fileName,
            [FromQuery] string code,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakTokenProvider  keyCloakTokenProvider,
            [FromServices] KeyCloakProvider  keyCloakProvider,
            [FromServices] AzureStorageAdapter azureStorageAdapter,
            CancellationToken ct) =>
        
        {
            if (string.IsNullOrWhiteSpace(code)) return Results.Unauthorized();
            var sw = Stopwatch.GetTimestamp();
            var redirectUrl = RedirectUrl(httpContextAccessor.HttpContext!.Request.Host.Value!, alias, fileName);
            var tokenResponse = await keyCloakTokenProvider.TokenByCode(code, redirectUrl, ct);
            if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            {
                logger.LogWarning("Download file bad code {comp} {file} {c} {sw}", alias, fileName, code, sw.Sw());
                return Results.Unauthorized();
            }
            var (userId, email) = KeyCloakExtension.GetUserIdAndEmail(tokenResponse.AccessToken);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Download file empty id or email {comp} {usr} {file} {sw}", alias, email, fileName,
                    sw.Sw());
                return Results.Unauthorized();
            }
            var kcOrg = await keyCloakProvider.GetUserOrganization(userId, alias, ct);
            var perms = kcOrg?.GetUserPermissions(email) ?? [];
            if (perms.Length == 0 || (!perms.Contains("admin") && !perms.Contains("manager")))
            {
                logger.LogWarning("Download file no access {comp} {usr} {file} {sw}", alias, email, fileName,
                    sw.Sw());
                return Results.Unauthorized();
            }

            try
            {
                var stream = await azureStorageAdapter.GetStream(alias, fileName, ct);
                logger.LogInformation("Download file OK {comp} {usr} {file} {sw}", alias, email, fileName,
                    sw.Sw());
                return Results.File(stream);
            }
            catch (Exception e)
            {
                logger.LogError("Download file error {comp} {usr} {m} {url} {sw}", alias, email, e.Message,
                    redirectUrl, sw.Sw());
                return Results.NotFound();
            }
        });
        
        var group = endpoints.MapGroup("api/file/{alias}").WithTags("File").RequireAuthorization();
        
        group.MapPost("userfolders", async ([FromBody]CdFolderRequest request,
            [FromRoute] string alias,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakTokenProvider keyCloakHelper,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            var userEmail = httpContextAccessor.GetEmail();
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Get user folders {e} {c} {p} {f}", userEmail, alias, request.Provider, request.FolderPath);
            if (userEmail == null) return Results.BadRequest("User email is null");
            //var userCompanies = await keyCloakProvider.GetUserOrganizationsWithPermissions(userEmail, ct);
            //var companyDto = await erpNextService.GetCompanyAsync(company, ct);
            //if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            
            var providerToken = await keyCloakHelper.TokenExchangeProviderAccessToken(userEmail, "microsoft-sharepoint", ct: ct);
            if (string.IsNullOrEmpty(providerToken.AccessToken)) return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));
        
            var paths = string.IsNullOrEmpty(request.FolderPath) || request.FolderPath == "/"
                ? []
                : request.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            using GraphApiAdapter adapter = new(providerToken.AccessToken, httpClientFactory, memoryCache);
          
            List<CdFolder> folders = [];
       
            if (paths.Length == 0)
            {
                var sites = await adapter.GetAllSitesWithDrives(ct);
                foreach (var st in sites.OrderByDescending(x => x.Drives[0].Folders.Max(y => y.LastModifiedDateTime)))
                {
                    folders.Add(new CdFolder
                    {
                        Id = st.Id, Name = st.Name, Description = st.Description, FolderType = "Site",
                        DisplayName = st.DisplayName, LastModifiedDateTime = st.LastModifiedDateTime,
                        CanBeSelected = false
                    });
                }

                logger.LogInformation("Get user sites {e} {c} {p} count={cnt} {sw}", userEmail, alias,
                    request.FolderPath, folders.Count, sw.Sw());
                return Results.Ok(new CdFolderResponse { Folders = folders });
            }
            var tenantName = await adapter.GetTenantName(ct);

            var s = await adapter.GetSite(paths[0], tenantName, ct);
            var drives = await adapter.GetAllDrives(s.Id, ct);
            if (paths.Length == 1)
            {
                foreach (var d in drives)
                {
                    folders.Add(new CdFolder
                    {
                        Id = d.Id, Name = d.Name, FolderType = "Drive", ParentId = s.Id,
                        DisplayName = d.Name, LastModifiedDateTime = d.LastModifiedDateTime, CanBeSelected = false
                    });
                }

                logger.LogInformation("Get user drives {e} {c} {p} count={cnt} {sw}", userEmail, alias,
                    request.FolderPath, folders.Count, sw.Sw());
                return Results.Ok(new CdFolderResponse { Folders = folders });
            }
            
            var driveId = drives.FirstOrDefault(x => x.Name == paths[1])?.Id;
            if (driveId == null) return Results.Conflict("Invalid drive name");
            var folderId = "root";
            for (var i = 2; i < paths.Length; i++)
            {
                var allFolders = await adapter.GetAllFolders(driveId, folderId, ct: ct);
                folderId = allFolders.FirstOrDefault(x => x.Name == paths[i])?.Id;
                if (folderId == null) return Results.Conflict("Invalid folder name");
            }
            
            var flds = await adapter.GetAllFolders(driveId, folderId, ct: ct);
           
            foreach (var folder in flds)
            {
                folders.Add(new CdFolder
                {
                    Id = folder.Id, Name = folder.Name, ParentId = folder.ParentFolderId ?? folder.DriveId,
                    FolderType = "Folder",
                    DisplayName = folder.Name,
                    LastModifiedDateTime = folder.LastModifiedDateTime
                });
            }
        
            CdFolderResponse response = new()
            {
                Folders = folders.OrderByDescending(x => x.LastModifiedDateTime).ToList()
            };
            logger.LogInformation("Get user folders {e} {c} {p} count={cnt} {sw}", userEmail, alias,
                request.FolderPath, folders.Count, sw.Sw());
            return Results.Ok(response);
        }).WithName("GetUserFolders").WithSummary("Get folders by user").Produces<CdFolderResponse>();
        
        group.MapPost("folders", async ([FromBody] CdFolderRequest request,
            [FromRoute] string alias,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakTokenProvider keyCloakHelper,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] KeyCloakProvider keyCloakProvider,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Get folder by service {c} {p} {f}", alias, request.Provider,
                request.FolderPath);
            var userEmail = httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var userTokenTask = keyCloakHelper.TokenExchangeProviderAccessToken(userEmail, request.Provider, ct: ct);
            //if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            var kcOrg =
                await keyCloakProvider.GetOrganization(alias, ct);
            if (kcOrg == null) return Results.BadRequest("Company not found");
            var userToken = await userTokenTask;
            var tenantId = await GetMsTenantId(keyCloakHelper, keyCloakProvider, kcOrg, userEmail, ct);
            if (string.IsNullOrEmpty(userToken.AccessToken))
                return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));
            var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(userToken.AccessToken, ct);
            using GraphApiAdapter clientAdapter = new(clientToken, httpClientFactory, memoryCache);
            var allSites = await clientAdapter.GetAllSites(ct);
            using GraphApiAdapter adapter = new(userToken.AccessToken, httpClientFactory, memoryCache);
            GraphApiManager graphManager = new(adapter, clientAdapter);
            var (driveId, folderId) = await graphManager.GetDriveByClient(request.FolderPath, tenantId, ct);
            List<CdFolder> folders = [];
            if (!string.IsNullOrEmpty(folderId))
            {
                var flds = await adapter.GetAllFolders(driveId, folderId, ct: ct);
        
                foreach (var folder in flds)
                {
                    folders.Add(new CdFolder
                    {
                        Id = folder.Id, Name = folder.Name, ParentId = folder.ParentFolderId ?? folder.DriveId,
                        Description = folder.ParentFolderId == null ? "SharePoint Root Folder" : "SharePoint Folder"
                    });
                }
        
                return Results.Ok(new CdFolderResponse { Folders = folders });
            }
        
            var paths = string.IsNullOrEmpty(request.FolderPath) || request.FolderPath == "/"
                ? []
                : request.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
            switch (paths.Length)
            {
                case > 1:
                    return Results.Conflict("Invalid folder path");
                case 0:
                {
                    // List<Task<bool>> tasks = new();
                    foreach (var s in allSites.Where(s => !s.Id.Contains("-my.sharepoint.com,")))
                    {
                        folders.Add(new CdFolder
                            { Id = s.Id, Name = s.Name, CanBeSelected = false, Description = "SharePoint site" });
                    }
        
                    return Results.Ok(new CdFolderResponse { Folders = folders });
                }
            }
        
            var siteId = allSites.FirstOrDefault(x => x.Name == paths[0])?.Id;
            if (siteId == null) return Results.Conflict("Invalid site name");
            var siteDrives = await adapter.GetAllDrives(siteId, ct);
            if (siteDrives.Length == 0) return Results.Ok();
            if (paths.Length != 1) return Results.Conflict();
            foreach (var drive in siteDrives)
            {
                folders.Add(new CdFolder
                {
                    Id = drive.Id, Name = drive.Name, ParentId = drive.Site?.Id,
                    Description = "SharePoint drive"
                });
            }
        
            return Results.Ok(new CdFolderResponse { Folders = folders });
        
        }).WithName("GetFoldersByService").WithSummary("Get folders by service").Produces<CdFolderResponse>();
        
 
        group.MapPost("uploadbylink", async ([FromBody] CdFileLinkRequest request,
            [FromRoute] string alias,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakTokenProvider keyCloakHelper,
            [FromServices] KeyCloakProvider keyCloakProvider,
            [FromServices] Task<IErpFileService?> erpFileServiceTask,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            [FromServices] AzureStorageAdapter azureStorageAdapter,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            var kcOrg = await keyCloakProvider.GetOrganization(alias, ct);
            if (kcOrg == null) return Results.BadRequest("Company not found");

            if (!Enum.TryParse<EDocumentUploadMethod>(kcOrg.GetStorageType(), out var storageType))
            {
                storageType = EDocumentUploadMethod.Sharepoint;
            }

            switch (storageType)
            {
                case EDocumentUploadMethod.LedgerInternal:
                {
                    var fileResult = await azureStorageAdapter.Upload(request.FileLink, alias, false, ct);
                    var fileUrl = FileUrl(httpContextAccessor.HttpContext!.Request.Host.Value!, alias, fileResult.Filename);
                    return Results.Ok(new CdFileLinkResponse { Link = fileUrl});
                }
            }
            
            var isClientToken = httpContextAccessor.IsClientToken() &&
                                 bool.TryParse(kcOrg.GetUseServiceToken(), out var result) && result;
            var userEmail = isClientToken ? kcOrg.GetCompanyOwner() : httpContextAccessor.GetEmail();
            var tenantId = await GetMsTenantId(keyCloakHelper, keyCloakProvider, kcOrg, userEmail, ct);

            logger.LogInformation("API send file link for company={c} email={ow} asService={isc} tenant={t}",
                kcOrg.Name, userEmail, isClientToken, tenantId);

            var token = isClientToken
                ? await graphApiHelper.GetClientAccessToken(tenantId, ct)
                : (await keyCloakHelper.TokenExchangeProviderAccessToken(userEmail, "microsoft-sharepoint", false, ct))
                .AccessToken;
            if (string.IsNullOrEmpty(token)) return Results.Conflict("Token is null");
            
            using var adapter = httpContextAccessor.Create<GraphApiAdapter>(token);

            var driveIdFolderId = JsonSerializer.Deserialize<MicrosoftGraphApiDriveIdFolderId>(kcOrg.GetPath());
            if (string.IsNullOrEmpty(driveIdFolderId?.DriveId))
            {
                logger.LogWarning("File upload to SharePoint drive is empty {c} {userEmail} {sw}", alias,
                    userEmail, sw.Sw());
                return Results.Conflict("Folder path does not exist");
            }

            var driveItem = await adapter.UploadFileByLink(driveIdFolderId.DriveId, driveIdFolderId.FolderId, request.FileLink, ct);

            if (driveItem == null)
            {
                logger.LogWarning("File upload to SharePoint error {c} {userEmail} {folderPath} {sw}", alias,
                    userEmail, driveIdFolderId.FolderId, sw.Sw());
                return Results.Conflict();
            }
            
            logger.LogInformation("File has been uploaded to SharePoint {c} {userEmail} {folderPath} url={u} {sw}",
                alias, userEmail, driveIdFolderId.FolderId, driveItem.WebUrl, sw.Sw());

            return Results.Ok(new CdFileLinkResponse { Link = driveItem.WebUrl!});
        }).WithName("UploadFileLink").WithSummary("Upload file by link").RequireAuthorization(AuthConstants.UserPolicy);
    }

    private static async ValueTask<string?> GetMsTenantId(KeyCloakTokenProvider tokenProvider, KeyCloakProvider provider,
        KeyCloakOrganization kcOrg, string userEmail, CancellationToken ct)
    {
        var tenantId = kcOrg.GetMsTenantId();
        if (!string.IsNullOrEmpty(tenantId)) return tenantId;
        var ownerToken =
            await tokenProvider.TokenExchangeProviderAccessToken(userEmail, "microsoft", false, ct);
        tenantId = AuthExtension.GetTenantId(ownerToken.AccessToken);
        if (string.IsNullOrEmpty(tenantId)) return tenantId;
        kcOrg.AddMsTenantId(tenantId);
        await provider.UpdateOrganization(kcOrg, ct);

        return tenantId;
    }
}