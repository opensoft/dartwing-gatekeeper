using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DartWing.DomainModel.Extensions;

namespace DartWing.DomainModel.Helpers;

public static partial class AbbreviationHelper
{
    private static readonly HashSet<string> LegalSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Inc", "Incorporated", "LLC", "Ltd", "Limited", "Corp", "Corporation", "GmbH", "PLC", "Co", "Company"
    };

    private static readonly Regex CamelCaseSplitter = MyRegex1();
    private static readonly Regex ValidCharRegex = MyRegex();

    public static string GenerateUniqueAbbreviation(string companyName, ImmutableHashSet<string> existingAbbreviations)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name cannot be empty");

        // Sanitize and filter
        var words = companyName
            .Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(x => CamelCaseSplitter.Split(x))
            .Select(word => ValidCharRegex.Replace(word, "")) // remove non-alphanumeric
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Where(word => !LegalSuffixes.Contains(word))
            .ToArray();

        if (words.Length == 0)
            return GlobalExtensions.RandomString(6);
        
        string baseAbbr;
        if (words.Sum(word => word.Length) < 21)
        {
            baseAbbr = string.Join("", words).ToLower();
        }
        else
        {
            if (words.Length < 3)
            {
                const int maxLength = 5;
                var cleaned = words[0];
                baseAbbr = cleaned.Length >= maxLength ? cleaned[..maxLength].ToLower() : cleaned.ToLower();
                if (words.Length > 1)
                {
                    cleaned = words[1];
                    baseAbbr += cleaned.Length >= maxLength ? cleaned[..maxLength].ToLower() : cleaned.ToLower();
                }
            }
            else
            {
                baseAbbr = string.Concat(words.Select(w => char.ToLower(w[0])));
            }
        }
        
        // Create base abbreviation
        var abbr = baseAbbr;
        var counter = 1;

        // Ensure uniqueness
        while (existingAbbreviations.Contains(abbr))
        {
            abbr = $"{baseAbbr}{counter++}";
            if (counter > 128) return GlobalExtensions.RandomString(6);;
        }
        return abbr;
    }
    
    [GeneratedRegex(@"[^a-zA-Z0-9]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"(?<!^)(?=[A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();
}