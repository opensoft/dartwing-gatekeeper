using DartWing.ErpNext;
using DartWing.ErpNext.Dto;
using DartWing.KeyCloak;
using DartWing.Microsoft;
using DartWing.Web.Files.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Files;

public static class FilesApiEndpoints
{
    public static void RegisterFolderApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/files/{company}").WithTags("File");
        
        group.MapPost("folders", async ([FromBody]CdFolderRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Get folder {c} {p} {f}", company, request.Provider, request.FolderPath);
            var u = httpContextAccessor.HttpContext?.User;
            var userId = u?.FindFirst("sub")?.Value;
            var userEmail = u?.FindFirst("email")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            
            var providerToken = await keyCloakHelper.GetProviderToken(userEmail, request.Provider, ct);
            if (string.IsNullOrEmpty(providerToken)) return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));

            var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(providerToken, ct);
            using GraphApiAdapter clientAdapter = new(clientToken);
            var allSites = await clientAdapter.GetAllSitesWithDrives(ct);
            
            using GraphApiAdapter adapter = new(providerToken);

            List<GraphApiAdapter.MicrosoftDriveInfo> acceptedDrives = []; 

            foreach (var site in allSites)
            {
                foreach (var dr in site.Drives)
                {
                    var allFolders = await adapter.GetFolders(dr.Id, true, ct);
                    if (allFolders.Count == 0) continue;
                    dr.Folders = allFolders;
                    dr.Site = site;
                    acceptedDrives.Add(dr);
                }
            }
            List<CdFolder> folders = [];
            foreach (var site in acceptedDrives.Select(x => x.Site).Distinct())
            {
                folders.Add(new CdFolder { Id = site.Id, Name = site.Name , Description = "SharePoint site", CanBeSelected = false });
            }
            
            foreach (var drive in acceptedDrives)
            {
                folders.Add(new CdFolder { Id = drive.Id, Name = drive.Name, ParentId = drive.Site.Id, Description = "SharePoint drive"});
            }
            
            foreach (var folder in acceptedDrives.SelectMany(x => x.Folders))
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
        }).WithName("GetFolders").WithSummary("Get folders").Produces<CdFolderResponse>();
        
        
        group.MapPost("folders/save", async ([FromBody]CdFolderRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
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
            UpdateCompanyDto updateCompany = new(companyDto.Data); 
            
            var providerToken = await keyCloakHelper.GetProviderToken(userEmail, request.Provider, ct);
            if (string.IsNullOrEmpty(providerToken)) return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));
            var tenantId = GraphApiHelper.GetTenantIdFromAccessToken(providerToken);
            updateCompany.CustomMicrosoftTenantId = tenantId;
            updateCompany.CustomMicrosoftSharepointFolderPath = request.FolderPath;
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
            CancellationToken ct) =>
        {
            logger.LogInformation("API upload file for company {c}", company);
            var u = httpContextAccessor.HttpContext?.User;
            var userId = u?.FindFirst("sub")?.Value;
            var userEmail = u?.FindFirst("email")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            var userCompanies = await erpNextService.GetUserCompaniesAsync(userEmail, ct);
            if (userCompanies.Data.All(x => x.User != userEmail)) return Results.Conflict();
            var companyDto = await erpNextService.GetCompanyAsync(company, ct);
            if (string.IsNullOrEmpty(companyDto.Data.CustomMicrosoftSharepointFolderPath)) return Results.Conflict("Folder path is empty");
            
            var providerToken = await keyCloakHelper.GetProviderToken(userEmail, "microsoft2", ct);
            if (string.IsNullOrEmpty(providerToken)) return Results.Conflict();
            
            var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(providerToken, ct);
            using GraphApiAdapter clientAdapter = new(clientToken);
            var allSites = await clientAdapter.GetAllSitesWithDrives(ct);
            var paths = companyDto.Data.CustomMicrosoftSharepointFolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var site = allSites.FirstOrDefault(x => x.Name == paths[0]);
            var driveId = site?.Drives.FirstOrDefault(x => x.Name == paths[1])?.Id;
            if (string.IsNullOrEmpty(driveId)) return Results.Conflict("Can't find drive");
            
            using GraphApiAdapter adapter = new(providerToken);
            bool success;
            await using (var stream = file.OpenReadStream())
            {
                success = await adapter.UploadFile(driveId, paths, file.FileName, file.ContentType, stream, ct);
            }

            return success? Results.Ok() : Results.Conflict();
        }).WithName("UploadFile").WithSummary("Upload file");
    }
}