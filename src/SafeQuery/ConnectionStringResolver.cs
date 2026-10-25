using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace SafeQuery;

internal static class ConnectionStringResolver
{
    private const string RepositoriesKey = "Repositories";
    private const string DefaultEnvironment = "default";
    private const string ExampleConnectionString =
        "Server=localhost;Database=MyDb;User Id=readonly_user;Password=your_password;TrustServerCertificate=True";

    internal static string DefaultSettingsPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".safequery", "settings.json");

    // Order: the settings-file entry for the nearest directory (from workingDirectory up to its git root),
    // then the ConnectionStrings__DefaultConnection env var, then an interactive prompt (saved under the git root).
    // Outside a git repo only workingDirectory itself is used.
    // A repository entry is either a connection string (the "default" environment) or an object of named environments.
    // environmentName: the --env value; null picks "default", or the only environment if there is just one.
    // A named environment falls back to the ConnectionStrings__<name> env var instead of DefaultConnection.
    // environment: null reads the real process environment.
    internal static string Resolve(
        string settingsPath,
        string workingDirectory,
        TextReader input,
        TextWriter prompt,
        bool inputRedirected,
        IEnumerable<KeyValuePair<string, string?>>? environment = null,
        string? environmentName = null)
    {
        var directories = DirectoriesToCheck(workingDirectory);

        var saved = ReadRepositories(settingsPath);
        foreach (var directory in directories)
        {
            if (saved.TryGetValue(directory, out var entry)
                && FindInEntry(entry, environmentName, directory) is { } found
                && !string.IsNullOrWhiteSpace(found))
            {
                return found;
            }
        }

        var builder = new ConfigurationBuilder();
        if (environment is null)
        {
            builder.AddEnvironmentVariables();
        }
        else
        {
            builder.AddInMemoryCollection(environment);
        }

        var connectionString = builder.Build().GetConnectionString(
            environmentName is null or DefaultEnvironment ? "DefaultConnection" : environmentName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return PromptAndSave(settingsPath, directories[^1], environmentName ?? DefaultEnvironment, input, prompt, inputRedirected);
    }

    private static string? FindInEntry(JsonNode entry, string? environmentName, string directory)
    {
        if (entry is JsonValue value)
        {
            return environmentName is null or DefaultEnvironment ? value.GetValue<string>() : null;
        }

        if (entry is not JsonObject environments)
        {
            return null;
        }

        if (environmentName is not null)
        {
            return environments[environmentName]?.GetValue<string>();
        }

        if (environments[DefaultEnvironment] is { } defaultEntry)
        {
            return defaultEntry.GetValue<string>();
        }

        return environments.Count switch
        {
            0 => null,
            1 => environments.First().Value?.GetValue<string>(),
            _ => throw new InvalidOperationException(
                $"Several environments are configured for {directory} ({string.Join(", ", environments.Select(e => e.Key))}). " +
                "Pass --env <name> to pick one."),
        };
    }

    // Directories from the working directory up to the git root, nearest first. The last item is where a new entry is saved.
    private static List<string> DirectoriesToCheck(string workingDirectory)
    {
        var start = Path.TrimEndingDirectorySeparator(Path.GetFullPath(workingDirectory));
        var directories = new List<string>();
        for (var current = start; current is not null; current = Path.GetDirectoryName(current))
        {
            directories.Add(current);
            if (Directory.Exists(Path.Combine(current, ".git")) || File.Exists(Path.Combine(current, ".git")))
            {
                return directories;
            }
        }

        return [start];
    }

    private static Dictionary<string, JsonNode> ReadRepositories(string settingsPath)
    {
        var comparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var result = new Dictionary<string, JsonNode>(comparer);
        if (File.Exists(settingsPath)
            && JsonNode.Parse(File.ReadAllText(settingsPath)) is JsonObject root
            && root[RepositoriesKey] is JsonObject repositories)
        {
            foreach (var (path, value) in repositories)
            {
                if (value is not null)
                {
                    result[Path.TrimEndingDirectorySeparator(path)] = value;
                }
            }
        }

        return result;
    }

    private static string PromptAndSave(string settingsPath, string saveDirectory, string environmentName, TextReader input, TextWriter prompt, bool inputRedirected)
    {
        if (inputRedirected)
        {
            throw new InvalidOperationException(
                $"Connection string not configured for {saveDirectory} ({environmentName}). Run safequery once interactively (without piped input) to be prompted, " +
                $"add it to {settingsPath}, or set the ConnectionStrings__DefaultConnection environment variable.");
        }

        prompt.WriteLine($"No {environmentName} connection string found for {saveDirectory}. It will be saved to {settingsPath}");
        prompt.WriteLine($"Example: {ExampleConnectionString}");
        prompt.Write("SQL Server connection string: ");
        var entered = input.ReadLine();
        if (string.IsNullOrWhiteSpace(entered))
        {
            throw new InvalidOperationException("No connection string entered.");
        }

        entered = entered.Trim();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var root = File.Exists(settingsPath) ? JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject ?? new JsonObject() : new JsonObject();
        var repositories = root[RepositoriesKey] as JsonObject ?? new JsonObject();
        repositories[saveDirectory] = (repositories[saveDirectory], environmentName) switch
        {
            (null, DefaultEnvironment) => entered,
            (JsonObject environments, _) => WithEnvironment(environments, environmentName, entered),
            (JsonValue existing, _) => new JsonObject { [DefaultEnvironment] = existing.GetValue<string>(), [environmentName] = entered },
            _ => new JsonObject { [environmentName] = entered },
        };
        root[RepositoriesKey] = repositories;
        File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(settingsPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return entered;
    }

    private static JsonObject WithEnvironment(JsonObject environments, string environmentName, string connectionString)
    {
        environments[environmentName] = connectionString;
        return environments;
    }
}
