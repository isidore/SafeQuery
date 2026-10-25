using Xunit;

namespace SafeQuery.Tests;

public class ReadOnlyGuardTests
{
    [Theory]
    [InlineData("SELECT 1")]
    [InlineData("select top 10 * from dbo.company")]
    [InlineData("  SELECT 1;  ")]
    [InlineData("WITH a AS (SELECT 1 AS x) SELECT x FROM a")]
    [InlineData("-- note\nSELECT 1")]
    [InlineData("/* note */ SELECT 1")]
    [InlineData("SELECT 1 /* DROP TABLE x; */")]
    [InlineData("SELECT 1 -- DROP TABLE x")]
    [InlineData("SELECT CreatedDate, UpdatedBy FROM t")]
    public void AcceptsReadOnlyQueries(string sql)
        => Assert.True(ReadOnlyGuard.IsReadOnlySelect(sql, out _));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-- only a comment")]
    [InlineData("/* only a comment */")]
    public void RejectsEmptyQueries(string? sql)
        => Assert.False(ReadOnlyGuard.IsReadOnlySelect(sql, out _));

    [Theory]
    [InlineData("INSERT INTO t VALUES (1)")]
    [InlineData("UPDATE t SET a = 1")]
    [InlineData("DELETE FROM t")]
    [InlineData("DROP TABLE t")]
    [InlineData("EXEC sp_who")]
    [InlineData("  exec('select 1')")]
    public void RejectsStatementsThatDoNotStartWithSelectOrWith(string sql)
    {
        Assert.False(ReadOnlyGuard.IsReadOnlySelect(sql, out var reason));
        Assert.Equal("Query must start with SELECT or WITH", reason);
    }

    [Theory]
    [InlineData("SELECT 1; DROP TABLE t")]
    [InlineData("SELECT 1; SELECT 2")]
    public void RejectsMultipleStatements(string sql)
    {
        Assert.False(ReadOnlyGuard.IsReadOnlySelect(sql, out var reason));
        Assert.Equal("Multiple statements are not allowed", reason);
    }

    [Theory]
    [InlineData("SELECT * INTO copy FROM t", "INTO")]
    [InlineData("WITH a AS (SELECT 1 AS x) INSERT INTO t SELECT x FROM a", "INSERT")]
    [InlineData("WITH a AS (SELECT 1 AS x) DELETE FROM t", "DELETE")]
    [InlineData("SELECT 1 WHERE 1 = (SELECT 1) EXEC x", "EXEC")]
    [InlineData("select 1 /* hidden */ drop table t", "DROP")]
    public void RejectsForbiddenKeywords(string sql, string keyword)
    {
        Assert.False(ReadOnlyGuard.IsReadOnlySelect(sql, out var reason));
        Assert.Equal($"Forbidden keyword detected: {keyword}", reason);
    }

    [Fact]
    public void CommentCannotHideSecondStatement()
    {
        Assert.False(ReadOnlyGuard.IsReadOnlySelect("SELECT 1;/* x */ DROP TABLE t", out _));
        Assert.False(ReadOnlyGuard.IsReadOnlySelect("SELECT 1 --\n; DROP TABLE t", out _));
    }

    // Known gap, tracked in the "Safety hardening" step: the filter does not understand string literals.
    [Fact]
    public void KnownFalsePositive_KeywordInsideStringLiteral()
        => Assert.False(ReadOnlyGuard.IsReadOnlySelect("SELECT * FROM t WHERE name = 'delete'", out _));

    // Known gap: '_' is a word character, so \bsp_\b never matches 'sp_who'.
    [Fact]
    public void KnownGap_SpPrefixIsNotActuallyBlocked()
        => Assert.True(ReadOnlyGuard.IsReadOnlySelect("SELECT * FROM sp_who", out _));
}
