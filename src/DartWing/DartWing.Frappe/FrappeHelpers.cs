using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe;

internal static class FrappeHelpers
{
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters = { new CustomDateTimeConverter() }
        };

    public static async Task<T?> Send<T>(this HttpClient client, HttpMethod method, string relativeUrl, string path,
        string[] query,
        ILogger logger, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        using var message = new HttpRequestMessage();
        message.Method = method;
        message.RequestUri = BuildUri(client, relativeUrl, path, query);

        using var response = await client.SendAsync(message, ct);
        return await response.HandleResponse<T>(sw, logger, ct);
    }

    public static async Task<TOut?> Send<TIn, TOut>(this HttpClient client, HttpMethod method, string relativeUrl,
        string path,
        string[] query, TIn? request, ILogger logger, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        using var message = new HttpRequestMessage();
        message.Method = method;
        message.RequestUri = BuildUri(client, relativeUrl, path, query);
        if (request != null)
            message.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(request, JsonSerializerOptions));

        using var response = await client.SendAsync(message, ct);
        return await response.HandleResponse<TOut>(sw, logger, ct);
    }

    private static Uri BuildUri(HttpClient client, string relativeUrl, string path, string[] query)
    {
        var add = relativeUrl;
        if (!string.IsNullOrWhiteSpace(path)) add += '/' + path;
        if (query.Length > 0) add += '?' + string.Join('&', query);

        return string.IsNullOrEmpty(add)
            ? client.BaseAddress!
            : new Uri($"{client.BaseAddress!.ToString().TrimEnd('/')}{add}");
    }

    public static async Task<T?> HandleResponse<T>(this HttpResponseMessage response, long timestamp, ILogger logger,
        CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync(ct);
            var request = response.RequestMessage?.Content != null
                ? await response.RequestMessage!.Content.ReadAsStringAsync(ct)
                : "";
            var headers = response.RequestMessage?.Headers.Authorization?.ToString();
            logger.LogWarning("erpNext {type} {url} code={r} response={b}    request={req} secr={header} {sw}",
                response.RequestMessage?.Method.Method,
                response.RequestMessage?.RequestUri?.AbsoluteUri, response.StatusCode, contentString, request, headers,
                Stopwatch.GetElapsedTime(timestamp));

            return default;
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            var request = response.RequestMessage?.Content != null
                ? await response.RequestMessage!.Content.ReadAsStringAsync(ct)
                : "";
            logger.LogDebug("erpNext {type} {url} code={r} response={b} request={req} {sw}",
                response.RequestMessage?.Method.Method,
                response.RequestMessage?.RequestUri?.AbsoluteUri, response.StatusCode, Encoding.UTF8.GetString(content),
                request, Stopwatch.GetElapsedTime(timestamp));
        }
        else
        {
            logger.LogInformation("erpNext {type} {url} code={r} {sw}", response.RequestMessage?.Method.Method,
                response.RequestMessage?.RequestUri?.AbsoluteUri, response.StatusCode,
                Stopwatch.GetElapsedTime(timestamp));
        }

        if (typeof(T) == typeof(bool)) return (T)(object)response.IsSuccessStatusCode;

        var result = JsonSerializer.Deserialize<T>(content, JsonSerializerOptions)!;
        return result;
    }

    public static string GetCountryName(string? countryCode)
    {
        return CountryCodes.GetValueOrDefault(countryCode?.ToUpperInvariant() ?? "US", countryCode);
    }
    
    private static Dictionary<string, string> CountryCodes = new()
    {
        { "AD", "Andorra" },
        { "AE", "United Arab Emirates" },
        { "AF", "Afghanistan" },
        { "AG", "Antigua and Barbuda" },
        { "AI", "Anguilla" },
        { "AL", "Albania" },
        { "AM", "Armenia" },
        { "AO", "Angola" },
        { "AQ", "Antarctica" },
        { "AR", "Argentina" },
        { "AS", "American Samoa" },
        { "AT", "Austria" },
        { "AU", "Australia" },
        { "AW", "Aruba" },
        { "AX", "Åland Islands" },
        { "AZ", "Azerbaijan" },
        { "BA", "Bosnia and Herzegovina" },
        { "BB", "Barbados" },
        { "BD", "Bangladesh" },
        { "BE", "Belgium" },
        { "BF", "Burkina Faso" },
        { "BG", "Bulgaria" },
        { "BH", "Bahrain" },
        { "BI", "Burundi" },
        { "BJ", "Benin" },
        { "BL", "Saint Barthélemy" },
        { "BM", "Bermuda" },
        { "BN", "Brunei Darussalam" },
        { "BO", "Bolivia, Plurinational State of" },
        { "BQ", "Bonaire, Sint Eustatius and Saba" },
        { "BR", "Brazil" },
        { "BS", "Bahamas" },
        { "BT", "Bhutan" },
        { "BV", "Bouvet Island" },
        { "BW", "Botswana" },
        { "BY", "Belarus" },
        { "BZ", "Belize" },
        { "CA", "Canada" },
        { "CC", "Cocos (Keeling) Islands" },
        { "CD", "Congo, The Democratic Republic of the" },
        { "CF", "Central African Republic" },
        { "CG", "Congo" },
        { "CH", "Switzerland" },
        { "CI", "Côte d'Ivoire" },
        { "CK", "Cook Islands" },
        { "CL", "Chile" },
        { "CM", "Cameroon" },
        { "CN", "China" },
        { "CO", "Colombia" },
        { "CR", "Costa Rica" },
        { "CU", "Cuba" },
        { "CV", "Cabo Verde" },
        { "CW", "Curaçao" },
        { "CX", "Christmas Island" },
        { "CY", "Cyprus" },
        { "CZ", "Czechia" },
        { "DE", "Germany" },
        { "DJ", "Djibouti" },
        { "DK", "Denmark" },
        { "DM", "Dominica" },
        { "DO", "Dominican Republic" },
        { "DZ", "Algeria" },
        { "EC", "Ecuador" },
        { "EE", "Estonia" },
        { "EG", "Egypt" },
        { "EH", "Western Sahara" },
        { "ER", "Eritrea" },
        { "ES", "Spain" },
        { "ET", "Ethiopia" },
        { "FI", "Finland" },
        { "FJ", "Fiji" },
        { "FM", "Micronesia, Federated States of" },
        { "FO", "Faroe Islands" },
        { "FR", "France" },
        { "GA", "Gabon" },
        { "GB", "United Kingdom" },
        { "GD", "Grenada" },
        { "GE", "Georgia" },
        { "GF", "French Guiana" },
        { "GG", "Guernsey" },
        { "GH", "Ghana" },
        { "GI", "Gibraltar" },
        { "GL", "Greenland" },
        { "GM", "Gambia" },
        { "GN", "Guinea" },
        { "GP", "Guadeloupe" },
        { "GQ", "Equatorial Guinea" },
        { "GR", "Greece" },
        { "GT", "Guatemala" },
        { "GU", "Guam" },
        { "GW", "Guinea-Bissau" },
        { "GY", "Guyana" },
        { "HK", "Hong Kong" },
        { "HM", "Heard Island and McDonald Islands" },
        { "HN", "Honduras" },
        { "HR", "Croatia" },
        { "HT", "Haiti" },
        { "HU", "Hungary" },
        { "ID", "Indonesia" },
        { "IE", "Ireland" },
        { "IL", "Israel" },
        { "IM", "Isle of Man" },
        { "IN", "India" },
        { "IO", "British Indian Ocean Territory" },
        { "IQ", "Iraq" },
        { "IR", "Iran, Islamic Republic of" },
        { "IS", "Iceland" },
        { "IT", "Italy" },
        { "JE", "Jersey" },
        { "JM", "Jamaica" },
        { "JO", "Jordan" },
        { "JP", "Japan" },
        { "KE", "Kenya" },
        { "KG", "Kyrgyzstan" },
        { "KH", "Cambodia" },
        { "KI", "Kiribati" },
        { "KM", "Comoros" },
        { "KN", "Saint Kitts and Nevis" },
        { "KP", "Korea, Democratic People's Republic of" },
        { "KR", "Korea, Republic of" },
        { "KW", "Kuwait" },
        { "KY", "Cayman Islands" },
        { "KZ", "Kazakhstan" },
        { "LA", "Lao People's Democratic Republic" },
        { "LB", "Lebanon" },
        { "LC", "Saint Lucia" },
        { "LI", "Liechtenstein" },
        { "LK", "Sri Lanka" },
        { "LR", "Liberia" },
        { "LS", "Lesotho" },
        { "LT", "Lithuania" },
        { "LU", "Luxembourg" },
        { "LV", "Latvia" },
        { "LY", "Libya" },
        { "MA", "Morocco" },
        { "MC", "Monaco" },
        { "MD", "Moldova, Republic of" },
        { "ME", "Montenegro" },
        { "MF", "Saint Martin (French part)" },
        { "MG", "Madagascar" },
        { "MH", "Marshall Islands" },
        { "MK", "North Macedonia" },
        { "ML", "Mali" },
        { "MM", "Myanmar" },
        { "MN", "Mongolia" },
        { "MO", "Macao" },
        { "MP", "Northern Mariana Islands" },
        { "MQ", "Martinique" },
        { "MR", "Mauritania" },
        { "MS", "Montserrat" },
        { "MT", "Malta" },
        { "MU", "Mauritius" },
        { "MV", "Maldives" },
        { "MW", "Malawi" },
        { "MX", "Mexico" },
        { "MY", "Malaysia" },
        { "MZ", "Mozambique" },
        { "NA", "Namibia" },
        { "NC", "New Caledonia" },
        { "NE", "Niger" },
        { "NF", "Norfolk Island" },
        { "NG", "Nigeria" },
        { "NI", "Nicaragua" },
        { "NL", "Netherlands" },
        { "NO", "Norway" },
        { "NP", "Nepal" },
        { "NR", "Nauru" },
        { "NU", "Niue" },
        { "NZ", "New Zealand" },
        { "OM", "Oman" },
        { "PA", "Panama" },
        { "PE", "Peru" },
        { "PF", "French Polynesia" },
        { "PG", "Papua New Guinea" },
        { "PH", "Philippines" },
        { "PK", "Pakistan" },
        { "PL", "Poland" },
        { "PM", "Saint Pierre and Miquelon" },
        { "PN", "Pitcairn" },
        { "PR", "Puerto Rico" },
        { "PT", "Portugal" },
        { "PW", "Palau" },
        { "PY", "Paraguay" },
        { "QA", "Qatar" },
        { "RE", "Réunion" },
        { "RO", "Romania" },
        { "RS", "Serbia" },
        { "RU", "Russian Federation" },
        { "RW", "Rwanda" },
        { "SA", "Saudi Arabia" },
        { "SB", "Solomon Islands" },
        { "SC", "Seychelles" },
        { "SD", "Sudan" },
        { "SE", "Sweden" },
        { "SG", "Singapore" },
        { "SH", "Saint Helena, Ascension and Tristan da Cunha" },
        { "SI", "Slovenia" },
        { "SJ", "Svalbard and Jan Mayen" },
        { "SK", "Slovakia" },
        { "SL", "Sierra Leone" },
        { "SM", "San Marino" },
        { "SN", "Senegal" },
        { "SO", "Somalia" },
        { "SR", "Suriname" },
        { "SS", "South Sudan" },
        { "ST", "Sao Tome and Principe" },
        { "SV", "El Salvador" },
        { "SX", "Sint Maarten (Dutch part)" },
        { "SY", "Syrian Arab Republic" },
        { "SZ", "Eswatini" },
        { "TC", "Turks and Caicos Islands" },
        { "TD", "Chad" },
        { "TF", "French Southern Territories" },
        { "TG", "Togo" },
        { "TH", "Thailand" },
        { "TJ", "Tajikistan" },
        { "TK", "Tokelau" },
        { "TL", "Timor-Leste" },
        { "TM", "Turkmenistan" },
        { "TN", "Tunisia" },
        { "TO", "Tonga" },
        { "TR", "Turkey" },
        { "TT", "Trinidad and Tobago" },
        { "TV", "Tuvalu" },
        { "TW", "Taiwan, Province of China" },
        { "TZ", "Tanzania, United Republic of" },
        { "UA", "Ukraine" },
        { "UG", "Uganda" },
        { "UM", "United States Minor Outlying Islands" },
        { "US", "United States" },
        { "UY", "Uruguay" },
        { "UZ", "Uzbekistan" },
        { "VA", "Holy See (Vatican City State)" },
        { "VC", "Saint Vincent and the Grenadines" },
        { "VE", "Venezuela, Bolivarian Republic of" },
        { "VG", "Virgin Islands, British" },
        { "VI", "Virgin Islands, U.S." },
        { "VN", "Viet Nam" },
        { "VU", "Vanuatu" },
        { "WF", "Wallis and Futuna" },
        { "WS", "Samoa" },
        { "YE", "Yemen" },
        { "YT", "Mayotte" },
        { "ZA", "South Africa" },
        { "ZM", "Zambia" },
        { "ZW", "Zimbabwe" }
    };
}