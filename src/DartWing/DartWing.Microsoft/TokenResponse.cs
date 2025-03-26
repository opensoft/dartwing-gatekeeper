namespace DartWing.Microsoft;

internal sealed class MicrosoftTokenResponse
{
    public string TokenType { get; set; }
    public int ExpiresIn { get; set; }
    public int ExtExpiresIn { get; set; }
    public string AccessToken { get; set; }
}