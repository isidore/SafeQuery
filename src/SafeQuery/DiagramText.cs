using System.Text.RegularExpressions;

namespace SafeQuery;

internal static class DiagramText
{
    // SqlServerToMermaid output that Mermaid cannot parse: commas inside types (e.g. "decimal(18,2)").
    internal static string MakeMermaidParseable(string markdown)
    {
        return Regex.Replace(markdown, @"\((\d+),(\d+)\)", "($1-$2)");
    }

    internal static string ExtractMermaid(string markdown)
    {
        var match = Regex.Match(markdown, @"```mermaid\s*\r?\n(.*?)```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : markdown;
    }
}
