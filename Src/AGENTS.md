# Src Guidance

Minimal guidance for source-tree work.

## Build/test

- Build from repo root with `.\build.ps1`.
- Run affected tests with `.\test.ps1` (or targeted `-TestProject` / `-Native`).

## Constraints

- Preserve native-first build order.
- Keep managed code compatible with .NET Framework 4.8 / C# 7.3.
- Keep interop boundaries explicit; do not introduce COM registration side-effects.
- Keep user-visible strings in resources.

## Discoverability

Use `Repository.Intelligence.Graph.json` for project/dependency/test topology instead of per-folder AGENTS files.