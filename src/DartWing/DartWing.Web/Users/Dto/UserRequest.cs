namespace DartWing.Web.Users.Dto;

internal sealed class UserRequest
{
    public string FirstName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string MobileNumber { get; set; }
    public string Location { get; set; }
    public string Gender { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public string TimeZone { get; set; }
    public string Language { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Interests { get; set; }
    public string? Bio { get; set; }
}