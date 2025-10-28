using System.Diagnostics;
using System.Security.Cryptography;

namespace DartWing.DomainModel.Extensions;

public static class GlobalExtensions
{
    public static bool? ToBool(this int? value)
    {
        if (!value.HasValue) return null;
        return value.GetValueOrDefault() == 1;
    }
    
    public static bool ToBool(this int value)
    {
        return value == 1;
    }
    
    public static int ToInt(this bool? value)
    {
        return value.GetValueOrDefault() ? 1 : 0;
    }
    
    public static int ToInt(this bool value)
    {
        return value ? 1 : 0;
    }
    
    private const char PlusValue = '+';
    private const char SlashValue = '/';

    private const char PlusRepl = '_';
    private const char SlashRepl = '-';

    public static string GetShortId(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())[..22].Replace(PlusValue, PlusRepl)
            .Replace(SlashValue, SlashRepl);
    }

    public static Guid GetGuid(string id)
    {
        return new(Convert.FromBase64String(id.Replace(PlusRepl, PlusValue).Replace(SlashRepl, SlashValue) + "=="));
    }

    public static TimeSpan Sw(this long sw)
    {
        return Stopwatch.GetElapsedTime(sw);
    }
    
    public static string RandomString(int length = 16)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return RandomNumberGenerator.GetString(chars, length);
    }
}