using DartWing.Frappe.Erp;
using DartWing.KeyCloak;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Suppliers;

public static class SupplierApiEndpoints
{
    public static void RegisterSupplierApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/supplier/{company}").WithTags("Supplier").RequireAuthorization();

        group.MapPost("", async ([FromBody] SupplierCreateRequest request,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakHelper,
            [FromServices] FrappeSupplierService supplierService,
            CancellationToken ct) =>
        {
            var supplier = request.GetRequest();
            var response = await supplierService.Create(supplier, ct);

            if (response == null) return Results.Conflict();

            SupplierResponse s = new(response);
            
            return Results.Created($"api/supplier/{s.Name}", s);
        }).WithName("CreateSupplier").WithSummary("Create supplier").Produces<SupplierResponse>();
        
        group.MapGet("", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakHelper,
            [FromServices] FrappeSupplierService supplierService,
            CancellationToken ct) =>
        {
            var response = await supplierService.GetList(page, pageSize, ct);

            if (response == null) return Results.Conflict();

            var names = response.Select(x => x.Name).ToArray();
            
            return Results.Ok(new SuppliersResponse {Names = names});
        }).WithName("GetSuppliers").WithSummary("Get suppliers").Produces<SuppliersResponse>();
        
        group.MapGet("{name}", async (string name,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakHelper,
            [FromServices] FrappeSupplierService supplierService,
            CancellationToken ct) =>
        {
            var response = await supplierService.Get(name, ct);

            if (response == null) return Results.Conflict();

            SupplierResponse result = new (response);
            
            return Results.Ok(result);
        }).WithName("GetSupplier").WithSummary("Get supplier").Produces<SupplierResponse>();
        
        group.MapPost("{name}/approve", async (string name,
            [FromRoute] string company,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakHelper,
            [FromServices] FrappeSupplierService supplierService,
            [FromServices] FrappeCompanySupplierApprovalService supplierApprovalService,
            CancellationToken ct)=>
        {
            var supplier = await supplierService.Get(name, ct);
            if (supplier == null) return Results.Conflict();
            var alreadyApproved = await supplierApprovalService.GetByCompany(company, name, 1, ct);
            if (alreadyApproved == null) return Results.Conflict();
            if (alreadyApproved.Length > 0) return Results.Ok("Already approved");

            FrappeCompanySupplierApprovalData approval = new()
            {
                Approved = 1,
                ApprovedBy = httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value ?? "",
                ApprovedOn = DateTime.UtcNow,
                Company = company,
                Supplier = name
            };
            var result = await supplierApprovalService.Create(approval, ct);
            
            return result != null ? Results.Ok() : Results.Conflict("Can't approve supplier");
        }).WithName("ApproveSupplier").WithSummary("Approve supplier").Produces<SupplierResponse>();
    }
}