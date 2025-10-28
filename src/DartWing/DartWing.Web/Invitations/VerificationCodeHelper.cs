namespace DartWing.Web.Invitations;

public static class VerificationCodeHelper
{
    private static readonly Random Random = new();
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateCode(int length)
    {
        Span<char> charSpan = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            var ch = Chars[Random.Next(0, Chars.Length)];
            if (i > length / 2)
            {
                var ind = 0;
                var sp = charSpan[..(i - 1)];
                while (sp.Count(ch) > length / 2)
                {
                    ch = Chars[Random.Next(0, Chars.Length)];
                    if (ind++ <= 16) continue;
                    ch = Chars[i * ind % Chars.Length];
                    break;
                }
            }
            charSpan[i] = ch;
        }

        return new string(charSpan);
    }
}