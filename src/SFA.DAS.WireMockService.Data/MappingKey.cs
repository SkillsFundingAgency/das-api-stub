using System;
using System.Linq;

namespace SFA.DAS.WireMockService.Data
{
    // Builds the table storage row key a mapping is saved under and looked up by, so that equivalent requests resolve to the same row.
    // Method and URL case, a leading slash, a trailing "?", percent-encoding and query parameter order are all ignored.
    public static class MappingKey
    {
        public static string For(string httpMethod, string url)
        {
            // Unescaped as a whole first, as the original repository did, so a fully escaped URL matches its plain form.
            // Saves and incoming requests both pass through here, so an escaped "&" or "=" in a value is treated the same on both sides.
            var parts = Uri.UnescapeDataString(url).Split('?', 2);
            var path = parts[0].TrimStart('/');
            var query = parts.Length > 1 ? NormalisedQuery(parts[1]) : string.Empty;

            var normalisedUrl = query.Length > 0 ? $"/{path}?{query}" : $"/{path}";

            // Escaping removes "/", "\", "#" and "?", which table storage forbids in row keys
            return $"{httpMethod.ToUpperInvariant()}_{Uri.EscapeDataString(normalisedUrl.ToLowerInvariant())}";
        }

        private static string NormalisedQuery(string query)
        {
            var parameters = query
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(parameter => parameter.Split('=', 2))
                .Select(pair => (Name: pair[0], Value: pair.Length > 1 ? pair[1] : string.Empty))
                .OrderBy(pair => pair.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            return string.Join("&", parameters.Select(pair => $"{Uri.EscapeDataString(pair.Name)}={Uri.EscapeDataString(pair.Value)}"));
        }
    }
}
