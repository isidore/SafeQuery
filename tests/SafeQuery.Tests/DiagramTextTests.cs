using Xunit;

namespace SafeQuery.Tests;

public class DiagramTextTests
{
    [Theory]
    [InlineData("decimal(18,2) price", "decimal(18-2) price")]
    [InlineData("numeric(5,0) a, decimal(9,4) b", "numeric(5-0) a, decimal(9-4) b")]
    [InlineData("varchar(50) name", "varchar(50) name")]
    public void MakesTypesWithCommasParseable(string input, string expected)
        => Assert.Equal(expected, DiagramText.MakeMermaidParseable(input));

    [Fact]
    public void ExtractsMermaidBlock()
        => Assert.Equal(
            "erDiagram\n  A ||--o{ B : x\n",
            DiagramText.ExtractMermaid("# Schema\n\n```mermaid\nerDiagram\n  A ||--o{ B : x\n```\n\ntext"));

    [Fact]
    public void ExtractMermaidReturnsInputWhenThereIsNoBlock()
        => Assert.Equal("erDiagram", DiagramText.ExtractMermaid("erDiagram"));
}
