using System.Diagnostics;
using DartWing.ErpNext;
using DartWing.ErpNext.Dto;
using DartWing.KeyCloak;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static class CompanyApiEndpoints
{
    public static void RegisterCompanyApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/company/").WithTags("Company").RequireAuthorization();

        group.MapPost("", async ([FromBody] CompanyCreateRequest company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Create/Update company {name} {abb}", company.Name, company.Abbreviation);

            var c = await erpNextService.GetCompanyAsync(company.Name, ct);
            if (c == null)
            {
                CreateCompanyDto cDto = new()
                {
                    Abbr = company.Abbreviation,
                    CompanyName = company.Name,
                    DefaultCurrency = company.Currency,
                    Domain = company.Domain,
                    CustomType = company.CompanyType,
                    CustomMicrosoftSharepointFolderPath = company.MicrosoftSharepointFolderPath,
                    Country = company.Country
                };
                var erpCompany = await erpNextService.CreateCompanyAsync(cDto, ct);
                
                var userEmail = httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
                if (!string.IsNullOrEmpty(userEmail)) await erpNextService.AddUserInCompanyAsync(userEmail, erpCompany!.Name, "Administrator", ct);
                CompanyResponse crResponse = new(erpCompany);

                return Results.Ok(crResponse);
            }
            UpdateCompanyDto updateDto = new(c)
            {
                IsEnabled = company.IsEnabled,
                CustomMicrosoftSharepointFolderPath = company.MicrosoftSharepointFolderPath,
                Domain = company.Domain,
                DefaultCurrency = company.Currency,
                CustomType = company.CompanyType,
                Country = company.Country
            };
            
            var erpUpdCompany = await erpNextService.UpdateCompanyAsync(company.Name, updateDto, ct);
            CompanyResponse response = new(erpUpdCompany);
            
            return Results.Ok(response);
        }).WithName("CreateOrUpdateCompany").WithSummary("Create or Update company").Produces<CompanyResponse>();
        
        group.MapGet("{companyName}", async ([FromServices] IHttpClientFactory httpClientFactory,
            string companyName,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get company {companyName}", companyName);
            var c = await erpNextService.GetCompanyAsync(companyName, ct);
            if (c == null) return Results.NotFound("Company not found");
            CompanyResponse crResponse = new(c);
            logger.LogInformation("Get company {uId} {email}: OK {sw}", c.Name, c.Abbr, Stopwatch.GetElapsedTime(sw));
            return Results.Ok(crResponse);
        }).WithName("Company").WithSummary("Get company").Produces<CompanyResponse>();
        
        group.MapGet("{companyName}/providers", async ([FromServices] IHttpClientFactory httpClientFactory,
            string companyName,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get company providers {companyName}", companyName);
            var c = await erpNextService.GetCompanyAsync(companyName, ct);
            if (c == null) return Results.NotFound("Company not found");
            CompanyProvidersResponse crResponse = new()
            {
                Providers =
                [
                    new CompanyProviderResponse { Name = "Microsoft SharePoint", Alias = "microsoft2" },
                    new CompanyProviderResponse { Name = "Google Drive", Alias = "google2" }
                ]
            };
            logger.LogInformation("Get company providers {uId} {email}: OK {sw}", c.Name, c.Abbr, Stopwatch.GetElapsedTime(sw));
            return Results.Ok(crResponse);
        }).WithName("CompanyProviders").WithSummary("Get company providers").Produces<CompanyProvidersResponse>();
    }
}