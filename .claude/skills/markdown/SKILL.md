---
name: markdown
description: How to edit Markdown in this project. Use when writing or editing any .md file.
---

## Code Blocks

Prefer `begin-snippet`/`end-snippet` in the test files and let `mdsnippets` generate C# code blocks, so examples stay compiled and tested.

## Generated regions

Markdown files may contain regions that `mdsnippets` overwrites. Never hand-edit the content between these markers; edit the source instead.

| Marker | Source |
| --- | --- |
| `<!-- snippet: Name -->` ... `<!-- endSnippet -->` | `// begin-snippet: Name` ... `// end-snippet` in a test under `tests/` |
| `<!-- include: path -->` ... `<!-- endInclude -->` | The included file |

To add a code example, wrap it in `begin-snippet`/`end-snippet` comments in the test file, then add an empty `snippet: Name` line to the Markdown. `mdsnippets` fills in the body and adds `<!-- endSnippet -->`.

Don't worry about regenerating snippets in the Markdown. The Tidy Code workflow runs `mdsnippets` on every push.
