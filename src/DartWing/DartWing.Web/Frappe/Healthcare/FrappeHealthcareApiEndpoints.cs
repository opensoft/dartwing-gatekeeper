using System.Diagnostics;
using DartWing.DomainModel.Extensions;
using DartWing.Frappe.Healthcare;
using DartWing.Web.Auth;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Frappe.Healthcare;

public static class FrappeHealthcareApiEndpoints
{
    public static void RegisterFrappeHealthcareApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/healthcare/{site}/{company}/patient").WithTags("Frappe healthcare").RequireAuthorization();

        group.MapGet("", async (
            string site,
            string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] Task<FrappePatientService?> frappePatientService,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            var patientService = await frappePatientService;
            if (patientService == null)
            {
                logger.LogWarning("Get patients {site} {c} {sw}", site, company, sw.Sw());
                return Results.NotFound("Company not found");
            }

            var patients = await patientService.GetAll(ct);
            logger.LogInformation("Get patients {uId} {email}: OK {count} {sw}", site, company, patients?.Count,
                sw.Sw());
            return Results.Ok(patients?.Select(FrappePatientModel.FromRequest).ToArray());
        }).WithName("Patients").WithSummary("Get patients").Produces<FrappePatientModel[]>();
            //.RequireAuthorization(AuthConstants.AdminPolicy);
        
        group.MapGet("{patientId}", async (
                string site,
                string company,
                string patientId,
                [FromServices] ILogger<Program> logger,
                [FromServices] Task<FrappePatientService?> frappePatientService, 
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                var patientService = await frappePatientService;
                if (patientService == null)
                {
                    logger.LogWarning("Get patients {site} {c} {sw}", site, company, sw.Sw());
                    return Results.NotFound("Company not found");
                }

                var patient = await patientService.Get(patientId, ct);
                logger.LogInformation("Get patient {uId} {email}: OK id={pId} {sw}", site, company,patientId, sw.Sw());
                return Results.Ok(FrappePatientModel.FromRequest(patient!));
            }).WithName("Patient").WithSummary("Get patient").Produces<FrappePatientModel>()
            .RequireAuthorization(AuthConstants.AdminPolicy);
        
        group.MapPost("", async (
                string site,
                string company,
                [FromBody] FrappePatientModel body,
                [FromServices] ILogger<Program> logger,
                [FromServices] Task<FrappePatientService?> frappePatientService, 
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                var patientService = await frappePatientService;
                if (patientService == null)
                {
                    logger.LogWarning("Get patients {site} {c} {sw}", site, company, sw.Sw());
                    return Results.NotFound("Company not found");
                }

                var request = FrappePatientModel.ToRequest(body);
                var patient = await patientService.Create(request, ct);
                logger.LogInformation("Get patient {uId} {email}: OK id={pId} {sw}", site, company, patient.IdentificationNumber, sw.Sw());
                return Results.Ok(FrappePatientModel.FromRequest(patient!));
            }).WithName("CreatePatient").WithSummary("Create patient").Produces<FrappePatientModel>()
            .RequireAuthorization(AuthConstants.AdminPolicy);
        
        group.MapDelete("{patientId}", async (
                string site,
                string company,
                string patientId,
                [FromServices] ILogger<Program> logger,
                [FromServices] Task<FrappePatientService?> frappePatientService, 
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                var patientService = await frappePatientService;
                if (patientService == null)
                {
                    logger.LogWarning("Get patients {site} {c} {sw}", site, company, sw.Sw());
                    return Results.NotFound("Company not found");
                }

                var patient = await patientService.Delete(patientId, ct);
                logger.LogInformation("Get patient {uId} {email}: OK id={pId} {sw}", site, company,patientId, sw.Sw());
                return Results.Ok(patient);
            }).WithName("DeletePatient").WithSummary("Delete patient").RequireAuthorization(AuthConstants.AdminPolicy);
    }
}