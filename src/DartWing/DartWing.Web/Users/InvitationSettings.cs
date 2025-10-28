namespace DartWing.Web.Users;

internal sealed class InvitationSettings
{
    public double TtlHours { get; set; } = 24;
    public int VerificationCodeLength { get; set; } = 6;
}