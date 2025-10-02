using System.Text.Json.Serialization;

namespace DartWing.ErpNext.Dto;

public sealed class UserCreateRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int SendWelcomeEmail { get; set; } = 1;
    public string Phone { get; set; } = string.Empty;
    public string MobileNo { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<UserRoleDto> Roles { get; set; } = [];
}

public class UserCreateResponseDto
{
    public UserResponseDto Data { get; set; }
    
    [JsonPropertyName("_server_messages")]
    public string ServerMessages { get; set; }
}

public sealed class UserResponseDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public int Enabled { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Language { get; set; }
    public string TimeZone { get; set; }
    public int SendWelcomeEmail { get; set; }
    public int Unsubscribed { get; set; }
    public int MuteSounds { get; set; }
    public string DeskTheme { get; set; }
    public string NewPassword { get; set; }
    public int LogoutAllSessions { get; set; }
    public string ResetPasswordKey { get; set; }
    public DateTime LastResetPasswordKeyGeneratedOn { get; set; }
    public int DocumentFollowNotify { get; set; }
    public string DocumentFollowFrequency { get; set; }
    public int FollowCreatedDocuments { get; set; }
    public int FollowCommentedDocuments { get; set; }
    public int FollowLikedDocuments { get; set; }
    public int FollowAssignedDocuments { get; set; }
    public int FollowSharedDocuments { get; set; }
    public int ThreadNotify { get; set; }
    public int SendMeACopy { get; set; }
    public int AllowedInMentions { get; set; }
    public int SimultaneousSessions { get; set; }
    public int LoginAfter { get; set; }
    public string UserType { get; set; }
    public int LoginBefore { get; set; }
    public int BypassRestrictIpCheckIf2faEnabled { get; set; }
    public string OnboardingStatus { get; set; }
    public string Doctype { get; set; }
    public List<object> Roles { get; set; }
    public List<object> Defaults { get; set; }
    public List<object> UserEmails { get; set; }
    public List<SocialLogin> SocialLogins { get; set; }
    public List<object> BlockModules { get; set; }
}

public sealed class SocialLogin
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string Provider { get; set; }
    public string Userid { get; set; }
    public string Parent { get; set; }
    public string Parentfield { get; set; }
    public string Parenttype { get; set; }
    public string Doctype { get; set; }
}

public sealed class UserRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public sealed class RoleDto
{
    public string Name { get; set; } = string.Empty;
}

public sealed class RolesResponseDto
{
    public List<RoleDto> Data { get; set; } = [];
}
