using Xunit;

namespace SafeQuery.Tests;

public class SqlInputTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("safequery-tests").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void PositionalArgumentIsTheSql() =>
        Assert.Equal("select 1", SqlInput.Resolve("select 1", null, TextReader.Null, false));

    [Fact]
    public void FileReadsTheFile()
    {
        var path = Path.Combine(_dir, "q.sql");
        File.WriteAllText(path, "select 2");

        Assert.Equal("select 2", SqlInput.Resolve(null, path, TextReader.Null, false));
    }

    [Fact]
    public void FileWinsOverPositionalArgument()
    {
        var path = Path.Combine(_dir, "q.sql");
        File.WriteAllText(path, "select 2");

        Assert.Equal("select 2", SqlInput.Resolve("select 1", path, TextReader.Null, false));
    }

    [Fact]
    public void MissingFileThrows()
    {
        var path = Path.Combine(_dir, "nope.sql");

        var ex = Assert.Throws<InvalidOperationException>(() => SqlInput.Resolve(null, path, TextReader.Null, false));

        Assert.Equal($"SQL file not found: {path}", ex.Message);
    }

    [Fact]
    public void ReadsStdinWhenRedirectedAndNoArguments() =>
        Assert.Equal("select 3", SqlInput.Resolve(null, null, new StringReader("select 3"), true));

    [Fact]
    public void ArgumentWinsOverStdin() =>
        Assert.Equal("select 1", SqlInput.Resolve("select 1", null, new StringReader("select 3"), true));

    [Fact]
    public void NoInputAtAllThrows()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SqlInput.Resolve(null, null, TextReader.Null, false));

        Assert.StartsWith("No SQL provided.", ex.Message);
    }
}
