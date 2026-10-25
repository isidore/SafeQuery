namespace SafeQuery;

internal static class SqlInput
{
    internal static string Resolve(string? sql, string? filePath, TextReader stdin, bool stdinRedirected)
    {
        if (filePath != null)
        {
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"SQL file not found: {filePath}");
            }

            return File.ReadAllText(filePath);
        }

        if (!string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        if (stdinRedirected)
        {
            return stdin.ReadToEnd();
        }

        throw new InvalidOperationException(
            "No SQL provided. Pass SQL as an argument, use -f <path>, or pipe SQL via stdin.");
    }
}
