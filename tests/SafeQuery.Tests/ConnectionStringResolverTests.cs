using System.Text.Json;
using Xunit;

namespace SafeQuery.Tests;

public class ConnectionStringResolverTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("safequery-tests").FullName;
    private string SettingsPath => Path.Combine(_dir, "sub", "settings.json");

    private string WorkDir => Path.Combine(_dir, "repo");

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static KeyValuePair<string, string?>[] Env(string? value) =>
        value is null ? [] : [new("ConnectionStrings:DefaultConnection", value)];

    private void WriteSettings(string value, string? directory = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var key = JsonSerializer.Serialize(directory ?? WorkDir);
        File.WriteAllText(SettingsPath, $$"""{ "Repositories": { {{key}}: "{{value}}" } }""");
    }

    [Fact]
    public void UsesEnvironmentWhenNoFile() =>
        Assert.Equal("env", ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env("env")));

    [Fact]
    public void UsesSettingsFileWhenNoEnvironment()
    {
        WriteSettings("file");

        Assert.Equal("file", ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env(null)));
    }

    [Fact]
    public void SettingsFileWinsOverEnvironment()
    {
        WriteSettings("file");

        Assert.Equal("file", ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env("env")));
    }

    [Fact]
    public void RedirectedInputWithNothingConfiguredThrows()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env(null)));

        Assert.StartsWith("Connection string not configured for ", ex.Message);
        Assert.Contains(SettingsPath, ex.Message);
    }

    [Fact]
    public void PromptsAndSavesTheAnswer()
    {
        var prompt = new StringWriter();

        var result = ConnectionStringResolver.Resolve(
            SettingsPath, WorkDir, new StringReader("  Server=x;Database=y  \n"), prompt, false, Env(null));

        Assert.Equal("Server=x;Database=y", result);
        Assert.Contains("SQL Server connection string:", prompt.ToString());
        Assert.Equal(
            "Server=x;Database=y",
            ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env(null)));
    }

    [Fact]
    public void SavedSettingsFileIsOwnerOnlyOnUnix()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        ConnectionStringResolver.Resolve(SettingsPath, WorkDir, new StringReader("Server=x\n"), TextWriter.Null, false, Env(null));

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(SettingsPath));
    }

    [Fact]
    public void EmptyPromptAnswerThrows()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConnectionStringResolver.Resolve(SettingsPath, WorkDir, new StringReader("\n"), TextWriter.Null, false, Env(null)));

        Assert.Equal("No connection string entered.", ex.Message);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void PromptShowsExampleAndDirectory()
    {
        var prompt = new StringWriter();

        ConnectionStringResolver.Resolve(SettingsPath, WorkDir, new StringReader("Server=x\n"), prompt, false, Env(null));

        Assert.Contains("Example: Server=localhost;", prompt.ToString());
        Assert.Contains(WorkDir, prompt.ToString());
    }

    [Fact]
    public void DifferentDirectoriesKeepTheirOwnConnections()
    {
        var other = Path.Combine(_dir, "other");
        ConnectionStringResolver.Resolve(SettingsPath, WorkDir, new StringReader("A\n"), TextWriter.Null, false, Env(null));
        ConnectionStringResolver.Resolve(SettingsPath, other, new StringReader("B\n"), TextWriter.Null, false, Env(null));

        Assert.Equal("A", ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env(null)));
        Assert.Equal("B", ConnectionStringResolver.Resolve(SettingsPath, other, TextReader.Null, TextWriter.Null, true, Env(null)));
    }

    [Fact]
    public void SubdirectoryUsesGitRootEntry()
    {
        var sub = Path.Combine(WorkDir, "src", "Api");
        Directory.CreateDirectory(Path.Combine(WorkDir, ".git"));
        Directory.CreateDirectory(sub);
        WriteSettings("root");

        Assert.Equal("root", ConnectionStringResolver.Resolve(SettingsPath, sub, TextReader.Null, TextWriter.Null, true, Env(null)));
    }

    [Fact]
    public void PromptFromSubdirectorySavesUnderGitRoot()
    {
        var sub = Path.Combine(WorkDir, "src");
        Directory.CreateDirectory(Path.Combine(WorkDir, ".git"));
        Directory.CreateDirectory(sub);

        ConnectionStringResolver.Resolve(SettingsPath, sub, new StringReader("Server=x\n"), TextWriter.Null, false, Env(null));

        Assert.Equal("Server=x", ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env(null)));
    }

    [Fact]
    public void WithoutGitRootParentEntryIsNotUsed()
    {
        var sub = Path.Combine(WorkDir, "src");
        Directory.CreateDirectory(sub);
        WriteSettings("parent");

        Assert.Throws<InvalidOperationException>(() =>
            ConnectionStringResolver.Resolve(SettingsPath, sub, TextReader.Null, TextWriter.Null, true, Env(null)));
    }

    private void WriteEnvironments(string environmentsJson)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, $$"""{ "Repositories": { {{JsonSerializer.Serialize(WorkDir)}}: {{environmentsJson}} } }""");
    }

    private string ResolveEnv(string? name, IEnumerable<KeyValuePair<string, string?>>? env = null) =>
        ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, env ?? Env(null), name);

    [Fact]
    public void NamedEnvironmentIsPicked()
    {
        WriteEnvironments("""{ "dev": "D", "prod": "P" }""");

        Assert.Equal("P", ResolveEnv("prod"));
        Assert.Equal("D", ResolveEnv("dev"));
    }

    [Fact]
    public void NoEnvironmentPicksDefault()
    {
        WriteEnvironments("""{ "default": "X", "prod": "P" }""");

        Assert.Equal("X", ResolveEnv(null));
    }

    [Fact]
    public void NoEnvironmentPicksTheOnlyOne()
    {
        WriteEnvironments("""{ "prod": "P" }""");

        Assert.Equal("P", ResolveEnv(null));
    }

    [Fact]
    public void NoEnvironmentWithSeveralAndNoDefaultThrows()
    {
        WriteEnvironments("""{ "dev": "D", "prod": "P" }""");

        var ex = Assert.Throws<InvalidOperationException>(() => ResolveEnv(null));

        Assert.Contains("dev, prod", ex.Message);
        Assert.Contains("--env", ex.Message);
    }

    [Fact]
    public void PlainStringIsTheDefaultEnvironment()
    {
        WriteSettings("file");

        Assert.Equal("file", ResolveEnv("default"));
        Assert.Throws<InvalidOperationException>(() => ResolveEnv("prod"));
    }

    [Fact]
    public void NamedEnvironmentUsesMatchingEnvironmentVariable()
    {
        KeyValuePair<string, string?>[] env = [new("ConnectionStrings:DefaultConnection", "D"), new("ConnectionStrings:prod", "P")];

        Assert.Equal("P", ResolveEnv("prod", env));
    }

    [Fact]
    public void PromptSavesUnderEnvironmentAndKeepsExistingDefault()
    {
        WriteSettings("file");
        var prompt = new StringWriter();

        ConnectionStringResolver.Resolve(SettingsPath, WorkDir, new StringReader("P\n"), prompt, false, Env(null), "prod");

        Assert.Contains("No prod connection string", prompt.ToString());
        Assert.Equal("P", ResolveEnv("prod"));
        Assert.Equal("file", ResolveEnv(null));
    }

    [Fact]
    public void PromptAddsEnvironmentToExistingEnvironments()
    {
        WriteEnvironments("""{ "dev": "D" }""");

        ConnectionStringResolver.Resolve(SettingsPath, WorkDir, new StringReader("P\n"), TextWriter.Null, false, Env(null), "prod");

        Assert.Equal("D", ResolveEnv("dev"));
        Assert.Equal("P", ResolveEnv("prod"));
    }

    [Fact]
    public void OldGlobalEntryIsIgnored()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, """{ "ConnectionStrings": { "DefaultConnection": "old" } }""");

        Assert.Throws<InvalidOperationException>(() =>
            ConnectionStringResolver.Resolve(SettingsPath, WorkDir, TextReader.Null, TextWriter.Null, true, Env(null)));
    }
}
