using System.Text.Json.Serialization;

namespace DartWing.Web.Invitations;

internal sealed class CreateInvitationRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
}

internal sealed class RevokeInvitationRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("verificationCode")]
    public string VerificationCode { get; set; } = string.Empty;
}

internal sealed class InvitationRequest
{
    [JsonPropertyName("verificationCode")]
    public string VerificationCode { get; set; } = string.Empty;
    
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }
}