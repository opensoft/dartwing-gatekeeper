namespace DartWing.Web.Auth;

internal static class AuthConstants
{
    public const string AdminPolicy = "Admin";
    public const string UserPolicy = "User";
    public const string LedgerCompanyAdminPermission = "admin";

    public static readonly string[] LedgerCompanyAllPermissions =
        [LedgerCompanyAdminPermission, "user", "manager"];
    
    public static readonly string[] LedgerCompanyMangePermissions =
        ["user", "manager"];
}