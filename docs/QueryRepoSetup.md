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
1. **Agent instructions**
   - Create a `CLAUDE.md` that tells the agent to:
     - Run all SQL with `safequery`.
     - Follow the [workflow](#workflow) below for each query file.
1. **Database connection**
   - Run `safequery "select 1"` in the repo.
   - It prompts for a connection string and saves it for this repo.
   - Use a login that only has read permission.
   - See the [SafeQuery README](https://github.com/isidore/SafeQuery#configure) for named environments like `--env prod`.
1. **Schema diagram**
   - Run `safequery --create-diagram`.
   - This gives the agent a map of the tables.

## Workflow

1. Create one Markdown file per question, for example `queries/monthly-customer-sales.md`.
1. Write the **goal** at the top, in plain English.
1. Mention the tables involved, if you know them.
1. Describe the **output format**: a Markdown table, a CSV, an HTML chart.
1. Ask Claude to read the file and ask you anything unclear.
1. Answer its questions; it updates the file with your answers.
1. Claude runs the query and gives you the result.
1. For queries you run often, save the latest SQL next to the file, for example `monthly-customer-sales.sql`.
   Rerun it yourself with `safequery -f queries/monthly-customer-sales.sql`.

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
