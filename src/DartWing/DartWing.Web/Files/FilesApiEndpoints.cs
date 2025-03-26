using DartWing.ErpNext;
using DartWing.KeyCloak;
using DartWing.Microsoft;
using DartWing.Web.Files.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Files;

public static class FilesApiEndpoints
{
    public static void RegisterFolderApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/folder/").WithTags("Folder");
        
        group.MapPost("", async ([FromBody]CdFolderRequest request,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            [FromServices] GraphApiHelper graphApiHelper,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Get folder {c} {p} {f}", request.Company, request.Provider, request.FolderPath);
            var u = httpContextAccessor.HttpContext?.User;
            var userId = u?.FindFirst("sub")?.Value;
            var userEmail = u?.FindFirst("email")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            
            var providerToken = await keyCloakHelper.GetProviderToken(userEmail, request.Provider, ct);
            if (string.IsNullOrEmpty(providerToken))
            {
                return Results.Ok(new CdFolderResponse(keyCloakHelper.BuildProviderRedirectUrl(request.Provider)));
            }

            var clientToken = await graphApiHelper.GetClientAccessTokenFromUserToken(providerToken, ct);
            using GraphApiAdapter clientAdapter = new(clientToken);
            var allSites = await clientAdapter.GetAllSitesWithDrives(ct);
            
            using GraphApiAdapter adapter = new(providerToken);
            
            List<GraphApiAdapter.MicrosoftDriveInfo> acceptedDrives = []; 

            foreach (var site in allSites)
            {
                foreach (var dr in site.Drives)
                {
                    var allFolders = await adapter.GetFolders(dr.Id, false, ct);
                    if (allFolders.Count == 0) continue;
                    dr.Folders = allFolders;
                    dr.Site = site;
                    acceptedDrives.Add(dr);
                }
            }
            List<CdFolder> folders = [];
            foreach (var site in acceptedDrives.Select(x => x.Site).Distinct())
            {
                folders.Add(new CdFolder { Id = site.Id, Name = site.Name , Description = "SharePoint site"});
            }
            
            foreach (var drive in acceptedDrives)
            {
                folders.Add(new CdFolder { Id = drive.Id, Name = drive.Name, ParentId = drive.Site.Id, Description = "SharePoint Drive"});
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
    }
}