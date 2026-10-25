using Xunit;

namespace SafeQuery.Tests;

public class MarkdownTableTests
{
    [Fact]
    public void RendersHeaderSeparatorAndRows()
    {
        var table = MarkdownTable.Render(["id", "name"], [["1", "Ann"], ["2", "Bob"]]);

        Assert.Equal("| id | name |\n| --- | --- |\n| 1 | Ann |\n| 2 | Bob |\n", table);
    }

    [Fact]
    public void NoRowsAddsZeroRowsMarker()
    {
        var table = MarkdownTable.Render(["id"], []);

        Assert.Equal("| id |\n| --- |\n_(0 rows)_\n", table);
    }

    [Theory]
    [InlineData("a|b", "a\\|b")]
    [InlineData("a\\b", "a\\\\b")]
    [InlineData("a\nb", "a b")]
    [InlineData("a\r\nb", "a b")]
    [InlineData("a\rb", "a b")]
    [InlineData("", "")]
    public void EscapesCells(string value, string expected)
        => Assert.Equal(expected, MarkdownTable.EscapeCell(value));

    [Fact]
    public void PipeInCellDoesNotAddAColumn()
    {
        var table = MarkdownTable.Render(["a"], [["x|y"]]);

        Assert.Contains("| x\\|y |", table);
    }
}
