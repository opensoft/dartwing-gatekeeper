using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public sealed  class FrappeCompanySupplierApprovalService : FrappeBaseService<FrappeCompanySupplierApprovalData>
{
    public FrappeCompanySupplierApprovalService(ILogger<FrappeCompanySupplierApprovalService> logger, HttpClient httpClient, IMemoryCache memoryCache)
        : base(logger, httpClient, memoryCache)
    {
    }

    public Task<FrappeCompanySupplierApprovalData[]?> GetByCompany(string company, string? supplier, int? approved, CancellationToken ct)
    {
        var supplQuery = string.IsNullOrEmpty(supplier) ? "" : $""",["supplier","=","{supplier}"]""";
        var approvedQuery = !approved.HasValue ? "" : $""",["approved","=","{approved.Value}"]""";
        
        string[] query = [$"""filters=[["company","=","{company}"]{supplQuery}{approvedQuery}]""", GetPageSizeQuery()];
        return GetListProtected(query, ct);
    }
}

public sealed class FrappeCompanySupplierApprovalData : IFrappeBaseDto
{
    public string Company { get; set; }
    public string Supplier { get; set; }
    public int Approved { get; set; }
    public string ApprovedBy { get; set; }
    public DateTime ApprovedOn { get; set; }
}
