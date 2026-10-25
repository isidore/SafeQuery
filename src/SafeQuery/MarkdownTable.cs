using System.Text;

namespace SafeQuery;

internal static class MarkdownTable
{
    internal static string Render(IReadOnlyList<string> columnNames, IEnumerable<IReadOnlyList<string>> rows)
    {
        var output = new StringBuilder();
        output.Append("| ").Append(string.Join(" | ", columnNames)).Append(" |\n");
        output.Append("| ").Append(string.Join(" | ", columnNames.Select(_ => "---"))).Append(" |\n");

        var rowCount = 0;
        foreach (var row in rows)
        {
            rowCount++;
            output.Append("| ").Append(string.Join(" | ", row.Select(EscapeCell))).Append(" |\n");
        }

        if (rowCount == 0)
        {
            output.Append("_(0 rows)_\n");
        }

        return output.ToString();
    }

    internal static string EscapeCell(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("|", "\\|")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
    }
}
