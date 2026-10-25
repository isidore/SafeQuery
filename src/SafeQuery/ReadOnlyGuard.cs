using System.Text.RegularExpressions;

namespace SafeQuery;

internal static class ReadOnlyGuard
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "INTO", "sp_", "xp_"
    ];

    internal static bool IsReadOnlySelect(string? rawSql, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(rawSql))
        {
            reason = "Empty query";
            return false;
        }

        var stripped = StripComments(rawSql).Trim();

        if (string.IsNullOrWhiteSpace(stripped))
        {
            reason = "Empty query after stripping comments";
            return false;
        }

        var semicolonIndex = stripped.IndexOf(';');
        if (semicolonIndex >= 0)
        {
            var remainder = stripped[(semicolonIndex + 1)..].Trim();
            if (remainder.Length > 0)
            {
                reason = "Multiple statements are not allowed";
                return false;
            }

            stripped = stripped[..semicolonIndex].TrimEnd();
        }

        if (!Regex.IsMatch(stripped, @"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase))
        {
            reason = "Query must start with SELECT or WITH";
            return false;
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            if (Regex.IsMatch(stripped, pattern, RegexOptions.IgnoreCase))
            {
                reason = $"Forbidden keyword detected: {keyword}";
                return false;
            }
        }

        return true;
    }

    private static string StripComments(string sql)
    {
        var noBlockComments = Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        var noLineComments = Regex.Replace(noBlockComments, @"--[^\r\n]*", string.Empty);
        return noLineComments;
    }
}
