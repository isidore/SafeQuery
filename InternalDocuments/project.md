# Project

SafeQuery is a .NET global tool (`safequery`) that runs read-only SQL against a SQL Server database and prints the result as a Markdown table. It can also generate a Mermaid/SVG diagram of the database schema.
It exists so AI coding agents (and humans in a hurry) can look at real data without being able to change it.


# Decisions

1. Only a single `SELECT` or `WITH` statement is accepted. Anything else fails with `Read-Only Queries Only`.
1. Write keywords (`INSERT`, `UPDATE`, `DELETE`, `EXEC`, `INTO`, ...) are rejected by a keyword filter. This is not a security boundary: users must connect with a login that only has read permission.
1. Connection string lookup order: the `Repositories` entry in `~/.safequery/settings.json` for the current directory or the nearest parent up to the git root (outside a git repo, the current directory only), then the `ConnectionStrings__DefaultConnection` environment variable, then an interactive prompt (showing an example connection string) whose answer is saved under the git root in `~/.safequery/settings.json` (owner read/write only on Unix). The old global `ConnectionStrings:DefaultConnection` entry in the file is no longer read.
1. `--env <name>` picks a named environment. A `Repositories` entry is either a connection string (the `default` environment) or an object of environment names to connection strings. Without `--env`, `default` is used, or the only environment if there is one; several with no `default` is an error. A named environment falls back to `ConnectionStrings__<name>` instead of `DefaultConnection`, and the prompt saves under that name, converting a plain string entry to an object that keeps it as `default`.
1. Output is a Markdown table; errors go to stderr with exit code 1.
1. `--create-diagram [path]` writes a Mermaid markdown file and a matching `.svg`; the path defaults to `./InternalDocuments/database-schema.md` and missing folders are created. Commas inside types are rewritten (`decimal(18,2)` becomes `decimal(18-2)`) and relationship labels are blanked so Mermaid can parse and render it.
1. Target framework: `net10.0`.
1. Package ID: `SafeQuery`, command name: `safequery`.
1. License: Apache-2.0.


# Project Structure

Scripts: build_and_test.sh
Unit tests: use xunit
CI: Github actions
    build and test (uses the script) on push
    publish, on github deploy, version from tag (example tag: v1.2.3)
Nuget: publishes the tool package to nuget.org on GitHub release, with the version taken from the tag (v1.2.3 becomes 1.2.3), using nuget.org trusted publishing (OIDC, no long-lived API key).


# Release Checklist

Work through these in order; see the git history for what is done.

1. Repo scaffolding: `.gitattributes`, `AGENTS.md`, this file, `SafeQuery.sln`.
1. Tests: extract logic out of `Program`, add xunit project (safety filter first).
1. Safety hardening: deferred, see TODO below.
1. Build and CI: `build_and_test.sh`, `build.yml`, `publish.yml`, version from tag.
1. Packaging: icon, repository metadata, SourceLink, symbols, package ID check, local install test.
1. README: badges, `--help`, exit codes, read-only login guidance.
1. CLI polish: `--help`, `--version`, System.CommandLine.


# TODO

## Safety hardening (deferred)

1. Keywords inside string literals are rejected (`WHERE name = 'delete'`). The filter should ignore string literals and bracketed identifiers. Flip `KnownFalsePositive_KeywordInsideStringLiteral` in `ReadOnlyGuardTests` when fixed.
1. `sp_` and `xp_` never match, because `_` is a word character and `\bsp_\b` needs a boundary after it. `SELECT * FROM sp_who` is accepted today. Flip `KnownGap_SpPrefixIsNotActuallyBlocked` when fixed.
1. Consider also blocking `OPENROWSET`, `OPENQUERY` and `OPENDATASOURCE`.
1. Back the README claim "writes are rejected inside a transaction" with a test, or run queries with `ApplicationIntent=ReadOnly` / a rolled-back transaction.
1. Add a row limit and a `--timeout` flag (the timeout is hard-coded to 30 seconds and there is no row cap).
1. Never echo the connection string or password in error output.
