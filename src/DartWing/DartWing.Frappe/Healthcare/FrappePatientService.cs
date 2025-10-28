using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.Frappe.Erp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Healthcare;

public class FrappePatientService : FrappeBaseService<FrappePatientCreateRequest>
{
    public FrappePatientService(ILogger<FrappePatientService> logger, HttpClient httpClient,
        IMemoryCache memoryCache) : base(logger, httpClient, memoryCache)
    {
    }
}

public sealed class FrappePatientModel : IFrappeBaseDto
{
    public string NamingSeries { get; set; } = "HMS-PAT-.YYYY.-";
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string Sex { get; set; }
    public string DateOfBirth { get; set; } // Format: YYYY-MM-DD
    public string Image { get; set; }
    public EFrappePatientStatus? Status { get; set; }
    public string IdentificationNumber { get; set; }
    public string InpatientRecord { get; set; }
    public EFrappeInpatientStatus? InpatientStatus { get; set; }
    public EFrappePatientReportPreference? ReportPreference { get; set; }
    public string Mobile { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string UserId { get; set; }
    public string Customer { get; set; }
    public string CustomerGroup { get; set; }
    public string Territory { get; set; }
    public string DefaultCurrency { get; set; }
    public string DefaultPriceList { get; set; }
    public string Language { get; set; }
    public string PatientDetails { get; set; }
    public EFrappePatientBloodGroup? BloodGroup { get; set; }
    
    public static FrappePatientModel FromRequest(FrappePatientCreateRequest request)
    {
        request.FixEnums();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request);
        return JsonSerializer.Deserialize<FrappePatientModel>(bytes)!;
    }
    
    public static FrappePatientCreateRequest ToRequest(FrappePatientModel request)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request);
        return JsonSerializer.Deserialize<FrappePatientCreateRequest>(bytes)!;
    }
}

public sealed class FrappePatientCreateRequest : IFrappeBaseDto
{
    public string NamingSeries { get; set; } = "HMS-PAT-.YYYY.-";
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string Sex { get; set; }
    public string? DateOfBirth { get; set; } // Format: YYYY-MM-DD
    public string? Image { get; set; }
    public string? Status { get; set; }
    public string IdentificationNumber { get; set; }
    public string? InpatientRecord { get; set; }
    public string? InpatientStatus { get; set; }
    public string? ReportPreference { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; }
    public string UserId { get; set; }
    public string? Customer { get; set; }
    public string? CustomerGroup { get; set; }
    public string? Territory { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? DefaultPriceList { get; set; }
    public string? Language { get; set; }
    public string? PatientDetails { get; set; }
    public string? BloodGroup { get; set; }

    public void FixEnums()
    {
        if (string.IsNullOrWhiteSpace(Status)) Status = null;
        if (string.IsNullOrWhiteSpace(InpatientStatus)) InpatientStatus = null;
        if (string.IsNullOrWhiteSpace(ReportPreference)) ReportPreference = null;
        if (string.IsNullOrWhiteSpace(BloodGroup)) BloodGroup = null;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EFrappePatientBloodGroup
{
    None,
    [JsonStringEnumMemberName("A Positive")]
    APositive,
    [JsonStringEnumMemberName("A Negative")]
    ANegative,
    [JsonStringEnumMemberName("AB Positive")]
    ABPositive,
    [JsonStringEnumMemberName("AB Negative")]
    ABNegative,
    [JsonStringEnumMemberName("B Positive")]
    BPositive,
    [JsonStringEnumMemberName("B Negative")]
    BNegative,
    [JsonStringEnumMemberName("O Positive")]
    OPositive,
    [JsonStringEnumMemberName("O Negative")]
    ONegative
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EFrappePatientStatus
{
    Active,
    Disabled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EFrappeInpatientStatus
{
    None,
    [JsonStringEnumMemberName("Admission Scheduled")]
    AdmissionScheduled,
    [JsonStringEnumMemberName("Admitted")]
    Admitted,
    [JsonStringEnumMemberName("Discharge Scheduled")]
    DischargeScheduled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EFrappePatientReportPreference
{
    None,
    [JsonStringEnumMemberName("Email")]
    Email,
    [JsonStringEnumMemberName("Print")]
    Print
}