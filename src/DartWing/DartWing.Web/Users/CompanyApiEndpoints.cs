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
        var group = endpoints.MapGroup("api/company/").WithTags("Company");

        group.MapPost("", async ([FromBody] CompanyCreateRequest company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Create company {name} {abb}", company.Name, company.Abbreviation);

            var c = await erpNextService.GetCompanyAsync(company.Name, ct);
            if (c == null)
            {
                CreateCompanyDto cDto = new()
                {
                    Abbr = company.Abbreviation,
                    CompanyName = company.Name,
                    DefaultCurrency = company.Currency,
                    Domain = company.Domain
                };
                var erpCompany = await erpNextService.CreateCompanyAsync(cDto, ct);

                return Results.Ok(erpCompany.Data);
            }
            UpdateCompanyDto updateDto = new()
            {
                DefaultCurrency = c.Data.DefaultCurrency,
                Domain = c.Data.Domain,
                IsEnabled = company.IsEnabled
            };
            
            var erpUpdCompany = await erpNextService.UpdateCompanyAsync(company.Name, updateDto, ct);

            return Results.Ok(erpUpdCompany.Data);
        }).WithName("CreateOrUpdateCompany").WithSummary("Create or Update company").Produces<CompanyDto>();
        
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
            if (c == null)
            {
                return Results.NotFound("Company not found");
            }
            logger.LogInformation("Get company {uId} {email}: OK {sw}", c.Data.Name, c.Data.Abbr, Stopwatch.GetElapsedTime(sw));
            return Results.Ok(c.Data);
        }).WithName("Company").WithSummary("Get company").Produces<CompanyDto>();
    }
}