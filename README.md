# SafeQuery

[![build](https://github.com/isidore/SafeQuery/actions/workflows/build.yml/badge.svg)](https://github.com/isidore/SafeQuery/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SafeQuery.svg)](https://www.nuget.org/packages/SafeQuery)
[![NuGet downloads](https://img.shields.io/nuget/dt/SafeQuery.svg)](https://www.nuget.org/packages/SafeQuery)

A small .NET CLI that runs **read-only** SQL against a SQL Server database and prints the result as a Markdown table. It also generates a Mermaid/SVG diagram of the database schema.

It was built so AI coding agents (and humans in a hurry) can look at real data without being able to change it.

> **Safety note:** SafeQuery rejects anything that isn't a single `SELECT`/`WITH` statement and blocks write keywords (`INSERT`, `UPDATE`, `DELETE`, `EXEC`, `INTO`, ...). This is a keyword filter, not a security boundary. Always connect with a database login that only has read permission.

## Install

```bash
dotnet tool install --global SafeQuery
```

## Configure

SafeQuery looks for the connection string in this order:

1. `~/.safequery/settings.json`, using the entry for the current directory or the nearest parent up to the git root (outside a git repo, the current directory only)
1. The `ConnectionStrings__DefaultConnection` environment variable
1. An interactive prompt (with an example connection string), whose answer is saved to `~/.safequery/settings.json` under the git root

Each repo gets its own connection, so different repos can point at different databases.

A repo can also hold several named environments. Pick one with `--env <name>`. Without `--env`, SafeQuery uses `default`, or the only environment if there is just one. A plain string is the `default` environment. A named environment falls back to the `ConnectionStrings__<name>` environment variable, and a missing one is prompted for and saved under that name.

```json
{
  "Repositories": {
    "/Users/me/Github/MyApp": { "default": "Server=dev...;", "prod": "Server=prod...;" }
  }
}
```

```json
{
  "Repositories": {
    "/Users/me/Github/MyApp": "Server=...;Database=...;User Id=...;Password=...;"
  }
}
```

If SQL is piped in on stdin there is no terminal to prompt on, so run `safequery` once interactively first (or set the env var).

## Usage

```bash
safequery "select top 10 * from dbo.company"   # inline query
safequery -f query.sql                         # from a file
type query.sql | safequery                     # piped stdin

safequery --create-diagram                     # writes ./InternalDocuments/database-schema.md and .svg
safequery --create-diagram docs/schema.md      # custom path
safequery --env prod "select count(*) from dbo.company"  # named environment
```

Writes are rejected every time, including inside a transaction.

## License

Apache-2.0. See [LICENSE](LICENSE).
