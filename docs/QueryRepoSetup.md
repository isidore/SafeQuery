# Query Repo Setup

A setup guide for asking questions of a production database with an AI agent and SafeQuery.
Works on Mac and Windows.

> **Claude:** if you were given this document, follow [Setup steps](#setup-steps) in order.
> Check each tool before installing it, and skip it if it is already there.
> Ask the user before anything you cannot decide yourself, one question at a time.

## The idea

Keep your queries in a git repo of their own.
Each question you want answered gets its own Markdown file.
You describe what you want in plain English, Claude asks clarifying questions, then runs the query through SafeQuery.
SafeQuery only allows reads, so the agent can look at real data without changing it.

## Toolchain

| Need            | Mac                                                 | Windows                    |
| --------------- | --------------------------------------------------- | -------------------------- |
| Version control | Git + GitHub Desktop                                | Git + GitHub Desktop       |
| Editor          | Visual Studio Code                                  | Visual Studio Code         |
| Terminal        | cmux                                                | wmux                       |
| Agent           | Claude Code (or another CLI agent)                  | Claude Code                |
| Voice input     | Superwhisper                                        | Built in: `Win + H`        |
| Runtime         | .NET 10 SDK                                         | .NET 10 SDK                |
| Query tool      | SafeQuery                                           | SafeQuery                  |

## Setup steps

1. **Git**
   - Check: `git --version`.
   - Mac: `xcode-select --install`.
   - Windows: `winget install Git.Git`.
1. **GitHub Desktop**
   - Mac: `brew install --cask github`.
   - Windows: `winget install GitHub.GitHubDesktop`.
1. **Visual Studio Code**
   - Check: `code --version`.
   - Mac: `brew install --cask visual-studio-code`.
   - Windows: `winget install Microsoft.VisualStudioCode`.
1. **Terminal**
   - Mac: install cmux.
   - Windows: install wmux.
1. **Claude Code**
   - Check: `claude --version`.
   - Install from https://claude.com/claude-code.
1. **Voice input**
   - Mac: install Superwhisper from https://superwhisper.com.
   - Windows: nothing to install, press `Win + H`.
1. **.NET 10 SDK**
   - Check: `dotnet --version` shows 10 or later.
   - Mac: `brew install --cask dotnet-sdk`.
   - Windows: `winget install Microsoft.DotNet.SDK.10`.
1. **SafeQuery**
   - Check: `safequery --help`.
   - Install: `dotnet tool install --global SafeQuery`.
   - Update: `dotnet tool update --global SafeQuery`.
1. **Query repo**
   - If the current folder is not a git repo, run `git init`.
   - Create a `queries/` folder.
   - Copy this document into the repo.
   - Add the repo to GitHub Desktop.
1. **Query process**
   - Write [`query.process.md`](#queryprocessmd) to the repo root.
1. **Slash commands**
   - Write [`.claude/skills/new-query/SKILL.md`](#new-query-skill).
   - Write [`.claude/skills/query/SKILL.md`](#query-skill).
1. **Agent instructions**
   - Create a `CLAUDE.md` that says:
     - Run all SQL with `safequery`.
     - Work on queries by following `query.process.md`.
1. **Database connection**
   - Run `safequery "select 1"` in the repo.
   - It prompts for a connection string and saves it for this repo.
   - Use a login that only has read permission.
   - See the [SafeQuery README](https://github.com/isidore/SafeQuery#configure) for named environments like `--env prod`.
1. **Schema diagram**
   - Run `safequery --create-diagram`.
   - This gives the agent a map of the tables.

## Workflow

1. Type `/new-query` and describe what you want to know.
1. Answer Claude's questions.
1. Get the result.
1. Later, type `/query` to pick from your existing queries and run one again.

The details live in `query.process.md`, so you can change how it works by editing one file.

## Files to create

### query.process.md

~~~markdown
# Query process

Each question about the data gets its own file in `queries/`.
Run all SQL with `safequery`; it only allows reads.

## New query

1. Ask the user what they want to know.
1. Create `queries/<kebab-case-name>.md` with these sections:
   - **Goal**: what the user wants, in plain English.
   - **Tables**: the tables involved, if known.
   - **Output**: the format, such as a Markdown table, a CSV or an HTML chart.
1. Read the file and ask the user anything unclear, one question at a time.
1. Update the file with each answer.
1. Write the SQL to `queries/<name>.sql`.
1. Run it with `safequery -f queries/<name>.sql`.
1. Produce the output in the requested format.

## Existing queries

1. List every `queries/*.md` as a numbered list, each with its goal.
1. Add a last item: new query.
1. Ask the user to pick a number.
1. For an existing query, check the file is still what the user wants, then run its `.sql` file.
1. For a new query, follow [New query](#new-query).

## Example query file

```markdown
# Monthly customer sales

## Goal
Total sales per customer for each of the last 12 months.

## Tables
Probably `dbo.Orders` and `dbo.Customers`.

## Output
A CSV with one row per customer and one column per month.
```
~~~

### new-query skill

~~~markdown
---
name: new-query
description: Start a new database query. Use when the user wants to ask a new question of the data.
---

Follow the **New query** section of `query.process.md`.
~~~

### query skill

~~~markdown
---
name: query
description: List existing database queries and run or continue one. Use when the user wants to rerun or revisit a query.
---

Follow the **Existing queries** section of `query.process.md`.
~~~
