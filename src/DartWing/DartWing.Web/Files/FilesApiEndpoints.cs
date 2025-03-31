using System.Text.Json;
using DartWing.ErpNext;
using DartWing.ErpNext.Dto;
using DartWing.KeyCloak;
using DartWing.Microsoft;
using DartWing.Web.Files.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DartWing.Web.Files;

public static class FilesApiEndpoints
{
    public static void RegisterFolderApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/files/{company}").WithTags("File").RequireAuthorization();;
        
        group.MapPost("userfolders", async ([FromBody]CdFolderRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Get folder {c} {p} {f}", company, request.Provider, request.FolderPath);
            var userEmail = httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var userCompanies = await erpNextService.GetUserCompaniesAsync(userEmail, ct);
            //var companyDto = await erpNextService.GetCompanyAsync(company, ct);
            //if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            
            var providerToken = await keyCloakHelper.GetProviderToken(userEmail, request.Provider, ct);
            if (string.IsNullOrEmpty(providerToken)) return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));

            using GraphApiAdapter adapter = new(providerToken, memoryCache);
            var drives = await adapter.Drives(ct);
            List<CdFolder> folders = [];
            var paths = string.IsNullOrEmpty(request.FolderPath) || request.FolderPath == "/"
                ? []
                : request.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (paths.Length == 0)
            {
                foreach (var drive in drives)
                {
                    folders.Add(new CdFolder { Id = drive.Id, Name = drive.Name, Description = "SharePoint drive"});
                }
            
                return Results.Ok(new CdFolderResponse { Folders = folders });
            }
            var driveId = drives.FirstOrDefault(x => x.Name == paths[0])?.Id;
            if (driveId == null) return Results.Conflict("Invalid drive name");
            var folderId = "root";
            for (var i = 1; i < paths.Length; i++)
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
                    Description = folder.ParentFolderId == null ? "SharePoint Root Folder" : "SharePoint Folder"
                });
            }

            CdFolderResponse response = new()
            {
                Folders = folders
            };
            
            return Results.Ok(response);
        }).WithName("GetUserFolders").WithSummary("Get folders by user").Produces<CdFolderResponse>();

        group.MapPost("folders", async ([FromBody] CdFolderRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Get folder by service {c} {p} {f}", company, request.Provider,
                request.FolderPath);
            var userEmail = httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var companyDtoTask = erpNextService.GetCompanyAsync(company, ct);
            var userTokenTask = keyCloakHelper.GetProviderToken(userEmail, request.Provider, ct);
            var userCompaniesTask = erpNextService.GetUserCompaniesAsync(userEmail, ct);
            //if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();

            var userToken = await userTokenTask;
            if (string.IsNullOrEmpty(userToken))
                return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));
            var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(userToken, ct);
            using GraphApiAdapter clientAdapter = new(clientToken, memoryCache);
            var allSites = await clientAdapter.GetAllSites(ct);
            using GraphApiAdapter adapter = new(userToken, memoryCache);
            GraphApiManager graphManager = new(adapter, clientAdapter);
            var (driveId, folderId) = await graphManager.GetDriveByClient(request.FolderPath, ct);
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
                    foreach (var site in allSites.Where(s => !s.Id.Contains("-my.sharepoint.com,")))
                    {
                        folders.Add(new CdFolder
                            { Id = site.Id, Name = site.Name, CanBeSelected = false, Description = "SharePoint site" });
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
        
        
        group.MapPost("folders/save", async ([FromBody]CdFolderRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Save folder {c} {p} {f}", company, request.Provider, request.FolderPath);
            var u = httpContextAccessor.HttpContext?.User;
            var userId = u?.FindFirst("sub")?.Value;
            var userEmail = u?.FindFirst("email")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            var userCompanies = await erpNextService.GetUserCompaniesAsync(userEmail, ct);
            if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            var companyDto = await erpNextService.GetCompanyAsync(company, ct);
            UpdateCompanyDto updateCompany = new(companyDto); 
            var providerToken = await keyCloakHelper.GetProviderToken(userEmail, request.Provider, ct);
            if (string.IsNullOrEmpty(providerToken)) return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));
            var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(providerToken, ct);
            var tenantId = GraphApiHelper.GetTenantIdFromAccessToken(providerToken);
            updateCompany.CustomMicrosoftTenantId = tenantId;
            updateCompany.CustomMicrosoftSharepointFolderPath = request.FolderPath;
            GraphApiManager graphManager = new(new GraphApiAdapter(providerToken, memoryCache), new GraphApiAdapter(clientToken, memoryCache));
            var (driveId, folderId) = await graphManager.GetDriveByClient(request.FolderPath, ct);
            updateCompany.CustomMicrosoftSharepointUserPath = JsonSerializer.Serialize(new MicrosoftGraphApiDriveIdFolderId{ DriveId = driveId, FolderId = folderId });
            await erpNextService.UpdateCompanyAsync(company, updateCompany, ct);

            return Results.Ok();
        }).WithName("SaveFolder").WithSummary("Save folder").Produces<CdFolderResponse>();
        
        
        group.MapPost("upload", async (IFormFile file,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            logger.LogInformation("API upload file for company {c}", company);
            var u = httpContextAccessor.HttpContext?.User;
            var userEmail = u?.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var userCompaniesTask = erpNextService.GetUserCompaniesAsync(userEmail, ct);
            //if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            var companyDtoTask = erpNextService.GetCompanyAsync(company, ct);
            var providerTokenTask = keyCloakHelper.GetProviderToken(userEmail, "microsoft2", ct);
            var companyDto = await companyDtoTask;
            if (string.IsNullOrEmpty(companyDto.CustomMicrosoftSharepointFolderPath)) return Results.Conflict("Folder path is empty");
            var providerToken = await providerTokenTask;
            if (string.IsNullOrEmpty(providerToken)) return Results.Conflict();
            
            MicrosoftGraphApiDriveIdFolderId driveIdFolderId;
            using GraphApiAdapter adapter = new(providerToken, memoryCache);
            if (!string.IsNullOrEmpty(companyDto.CustomMicrosoftSharepointUserPath))
            {
                driveIdFolderId = JsonSerializer.Deserialize<MicrosoftGraphApiDriveIdFolderId>(companyDto
                    .CustomMicrosoftSharepointUserPath)!;
            }
            else
            {
                var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(providerToken, ct);
                var graphManager = new GraphApiManager(adapter, new GraphApiAdapter(clientToken, memoryCache));
                var (driveId, folderId) = await graphManager.GetDriveByClient(companyDto.CustomMicrosoftSharepointFolderPath, ct);
                driveIdFolderId = new () { DriveId = driveId, FolderId = folderId };
                if (!string.IsNullOrEmpty(driveIdFolderId.DriveId))
                {
                    var cDto = await erpNextService.GetCompanyAsync(company, ct);
                    UpdateCompanyDto updateCompany = new(cDto);
                    updateCompany.CustomMicrosoftSharepointUserPath = JsonSerializer.Serialize(driveIdFolderId);
                    await erpNextService.UpdateCompanyAsync(company, updateCompany, ct);
                }
            }
            
            if (string.IsNullOrEmpty(driveIdFolderId.DriveId)) return Results.Conflict("Folder path is empty");
            
            bool success;
            await using (var stream = file.OpenReadStream())
            {
                success = await adapter.UploadFile(driveIdFolderId.DriveId, driveIdFolderId.FolderId, file.FileName, file.ContentType, stream, ct);
            }

            return success? Results.Ok() : Results.Conflict();
        }).WithName("UploadFile").WithSummary("Upload file").WithMetadata(new RequestSizeLimitAttribute(100_000_000)).DisableAntiforgery();
        
        group.MapPost("filelink", async ([FromBody] CdFileLinkRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            logger.LogInformation("API send file link for company {c}", company);
            var u = httpContextAccessor.HttpContext?.User;
            var userEmail = u?.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var userCompaniesTask = erpNextService.GetUserCompaniesAsync(userEmail, ct);
            //if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            var companyDtoTask = erpNextService.GetCompanyAsync(company, ct);
            var providerTokenTask = keyCloakHelper.GetProviderToken(userEmail, "microsoft2", ct);
            var companyDto = await companyDtoTask;
            if (string.IsNullOrEmpty(companyDto.CustomMicrosoftSharepointFolderPath)) return Results.Conflict("Folder path is empty");
            var providerToken = await providerTokenTask;
            if (string.IsNullOrEmpty(providerToken)) return Results.Conflict();
            
            MicrosoftGraphApiDriveIdFolderId driveIdFolderId;
            using GraphApiAdapter adapter = new(providerToken, memoryCache);
            if (!string.IsNullOrEmpty(companyDto.CustomMicrosoftSharepointUserPath))
            {
                driveIdFolderId = JsonSerializer.Deserialize<MicrosoftGraphApiDriveIdFolderId>(companyDto
                    .CustomMicrosoftSharepointUserPath)!;
            }
            else
            {
                var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(providerToken, ct);
                var graphManager = new GraphApiManager(adapter, new GraphApiAdapter(clientToken, memoryCache));
                var (driveId, folderId) = await graphManager.GetDriveByClient(companyDto.CustomMicrosoftSharepointFolderPath, ct);
                driveIdFolderId = new () { DriveId = driveId, FolderId = folderId };
                if (!string.IsNullOrEmpty(driveIdFolderId.DriveId))
                {
                    var cDto = await erpNextService.GetCompanyAsync(company, ct);
                    UpdateCompanyDto updateCompany = new(cDto);
                    updateCompany.CustomMicrosoftSharepointUserPath = JsonSerializer.Serialize(driveIdFolderId);
                    await erpNextService.UpdateCompanyAsync(company, updateCompany, ct);
                }
            }
            
            if (string.IsNullOrEmpty(driveIdFolderId.DriveId)) return Results.Conflict("Folder path is empty");

            var client = httpClientFactory.CreateClient();
            using var message = await client.GetAsync(request.Link, ct);
            
            var contentType = message.Content.Headers.ContentType?.MediaType;
            var fileName = message.Content.Headers.ContentDisposition?.FileName;
            await using var fileStream = await message.Content.ReadAsStreamAsync(ct);

            var success = await adapter.UploadFile(driveIdFolderId.DriveId, driveIdFolderId.FolderId,
                fileName ?? Guid.NewGuid().ToString(), contentType ?? "application/octet-stream", fileStream, ct);

            return success? Results.Ok() : Results.Conflict();
        }).WithName("UploadFile").WithSummary("Upload file").WithMetadata(new RequestSizeLimitAttribute(100_000_000)).DisableAntiforgery();
    }
}