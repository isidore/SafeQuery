using DbToMermaid;
using Microsoft.Data.SqlClient;
using Naiad;
using System.CommandLine;

namespace SafeQuery;

public static class Program
{
    private const string DefaultDiagramPath = "./InternalDocuments/database-schema.md";

    private static string? _environmentName;

    public static async Task<int> Main(string[] args)
    {
        var sqlArgument = new Argument<string?>("sql")
        {
            Description = "The SELECT statement to run. Omit it to use -f or stdin.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var fileOption = new Option<string?>("-f")
        {
            Description = "Read the SQL from this file.",
            HelpName = "path"
        };
        var envOption = new Option<string?>("--env")
        {
            Description = "Use the named connection environment.",
            HelpName = "name"
        };
        var diagramOption = new Option<string?>("--create-diagram")
        {
            Description = $"Write a Mermaid schema diagram and SVG (default path: {DefaultDiagramPath}).",
            HelpName = "path",
            Arity = ArgumentArity.ZeroOrOne
        };

        var rootCommand = new RootCommand("Read-only SQL Server query CLI.")
        {
            sqlArgument, fileOption, envOption, diagramOption
        };
        rootCommand.SetAction(async parseResult =>
        {
            _environmentName = parseResult.GetValue(envOption);
            if (parseResult.GetResult(diagramOption) != null)
            {
                return await CreateDiagram(parseResult.GetValue(diagramOption) ?? DefaultDiagramPath);
            }

            return await RunQuery(parseResult.GetValue(sqlArgument), parseResult.GetValue(fileOption));
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task<int> RunQuery(string? sqlArgument, string? filePath)
    {
        string sql;
        try
        {
            sql = SqlInput.Resolve(sqlArgument, filePath, Console.In, Console.IsInputRedirected);
        }
        catch (Exception ex)
        {
            return await ReportFailure(ex.Message);
        }

        if (!ReadOnlyGuard.IsReadOnlySelect(sql, out _))
        {
            return await ReportFailure("Read-Only Queries Only");
        }

        try
        {
            var connectionString = GetConnectionString();
            await RunQueryAsync(connectionString, sql);
            return 0;
        }
        catch (Exception ex)
        {
            return await ReportFailure(ex.Message);
        }
    }

    private static async Task<int> ReportFailure(params string[] messages)
    {
        foreach (var message in messages)
        {
            await Console.Error.WriteLineAsync($"Exception: {message}");
        }

        return 1;
    }

    private static async Task<int> CreateDiagram(string path)
    {
        try
        {
            // SMO opens its own connection from connection.ConnectionString, which drops the password after Open unless this is set.
            var builder = new SqlConnectionStringBuilder(GetConnectionString()) { PersistSecurityInfo = true };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            var markdown = await SqlServerToMermaid.RenderMarkdown(connection);
            var parseable = DiagramText.MakeMermaidParseable(markdown);
            var narrow = DiagramText.BlankRelationshipLabels(parseable);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            await File.WriteAllTextAsync(path, narrow);
            Console.WriteLine($"Diagram written to {path}");

            var svgPath = Path.ChangeExtension(path, ".svg");
            await File.WriteAllTextAsync(svgPath, Mermaid.Render(DiagramText.ExtractMermaid(narrow)));
            Console.WriteLine($"SVG written to {svgPath}");
            return 0;
        }
        catch (Exception ex)
        {
            // SMO wraps the real cause (login, TLS, ...) in inner exceptions.
            return await ReportFailure([.. ExceptionChain(ex).Select(inner => inner.Message)]);
        }
    }

    private static IEnumerable<Exception> ExceptionChain(Exception ex)
    {
        for (var inner = ex; inner != null; inner = inner.InnerException)
        {
            yield return inner;
        }
    }

    private static async Task RunQueryAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 30
        };

        await using var reader = await command.ExecuteReaderAsync();

        var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<string[]>();
        while (await reader.ReadAsync())
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
            }

            rows.Add(values);
        }

        Console.Write(MarkdownTable.Render(columnNames, rows));
    }

    private static string GetConnectionString() =>
        ConnectionStringResolver.Resolve(
            ConnectionStringResolver.DefaultSettingsPath(), Directory.GetCurrentDirectory(), Console.In,
            Console.Error,
            Console.IsInputRedirected,
            environmentName: _environmentName);
}
