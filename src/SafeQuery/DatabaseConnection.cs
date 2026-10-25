using Microsoft.Data.SqlClient;

namespace SafeQuery;

internal static class DatabaseConnection
{
    internal const int MaxConnectTimeoutSeconds = 10;

    // Saved connection strings often carry a 60s timeout, which feels like a hang when the server is down.
    internal static SqlConnectionStringBuilder Prepare(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.ConnectTimeout = Math.Min(builder.ConnectTimeout, MaxConnectTimeoutSeconds);
        return builder;
    }

    internal static string DescribeFailure(SqlConnectionStringBuilder builder, string reason)
        => $"Could not connect to '{builder.DataSource}' (database '{builder.InitialCatalog}') within {builder.ConnectTimeout}s. " +
        $"Is the server running and reachable? Reason: {reason}";

    internal static async Task<SqlConnection> OpenAsync(SqlConnectionStringBuilder builder)
    {
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch (SqlException ex)
        {
            await connection.DisposeAsync();
            Exception root = ex;
            while (root.InnerException != null)
            {
                root = root.InnerException;
            }

            throw new InvalidOperationException(DescribeFailure(builder, root.Message));
        }
    }
}
