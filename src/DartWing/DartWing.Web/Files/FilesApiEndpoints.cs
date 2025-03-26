using DartWing.ErpNext;
using DartWing.KeyCloak;
using DartWing.Web.Azure;
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
            
            GraphApiAdapter adapter = new(providerToken);
            var fold = await adapter.GetMyFolders(ct);
            var sites = await adapter.GetAllSites(ct);
            //var drives = await adapter.GetAllDrives(ct);
            
            return Results.Ok(sites);
        }).WithName("GetFolders").WithSummary("Get folders").Produces<CdFolderResponse>();
    }
}