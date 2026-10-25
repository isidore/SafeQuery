using Xunit;

namespace SafeQuery.Tests;

public class DatabaseConnectionTests
{
    [Theory]
    [InlineData("Data Source=x;Connection Timeout=60", 10)]
    [InlineData("Data Source=x;Connection Timeout=3", 3)]
    public void CapsConnectTimeout(string connectionString, int expected)
        => Assert.Equal(expected, DatabaseConnection.Prepare(connectionString).ConnectTimeout);

    [Fact]
    public void DescribesFailureWithServerAndDatabase()
        => Assert.Equal(
            "Could not connect to 'tcp:localhost,1433' (database 'Db') within 10s. Is the server running and reachable? Reason: boom",
            DatabaseConnection.DescribeFailure(
                DatabaseConnection.Prepare("Data Source=tcp:localhost,1433;Initial Catalog=Db;Connection Timeout=60"), "boom"));
}
