using DartWing.Frappe.Erp;
using DartWing.KeyCloak.Dto;

namespace DartWing.Web.Users.Dto;

public sealed class UsersResponse
{
    public string[] Users { get; set; }
}

public sealed class UserResponse
{
    public UserResponse() { }

    public UserResponse(IKeyCloakUser user)
    {
        Name = user.Username;
        Email = user.Email;
        FirstName = user.FirstName;
        Enabled = 1;
    }

    public UserResponse(IFrappeUser erpUserData)
    {
        Name = erpUserData.Name;
        Email = erpUserData.Email;
        Creation = erpUserData.Creation;
        Modified = erpUserData.Modified;
        Enabled = erpUserData.Enabled;
        FirstName = erpUserData.FirstName;
        FullName = erpUserData.FullName;
        Language = erpUserData.Language;
        TimeZone = erpUserData.TimeZone;
        ModifiedBy = erpUserData.ModifiedBy;
        Owner = erpUserData.Owner;
    }

    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Enabled { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string FullName { get; set; }
    public string Language { get; set; }
    public string TimeZone { get; set; }
}

public sealed class UserCompanyResponse
{
    public string CompanyName { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
}