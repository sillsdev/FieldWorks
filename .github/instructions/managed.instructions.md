---
applyTo: "**/*.{cs,xaml,config,resx}"
description: "FieldWorks managed (.NET/C#) development guidelines"
---
# Managed development guidelines for C# and .NET

## Context loading
- Review `.github/src-catalog.md` and `Src/<Folder>/COPILOT.md` for component responsibilities and entry points.
- Follow localization patterns (use .resx resources; avoid hardcoded UI strings). Crowdin sync is configured via `crowdin.json`.

## Deterministic requirements
- Threading: UI code must run on the UI thread; prefer async patterns for long-running work. Avoid deadlocks; do not block the UI.
- Exceptions: Fail fast for unrecoverable errors; log context. Avoid swallowing exceptions.
- Encoding: Favor UTF-16/UTF-8; be explicit at interop boundaries; avoid locale-dependent APIs.
- Tests: Co-locate unit/integration tests under `Src/<Component>.Tests` (NUnit patterns are common). Keep tests deterministic and portable.
- Resources: Place images/strings in resource files; avoid absolute paths; respect `.editorconfig`.

## Structured output
- Public APIs include XML docs; keep namespaces consistent.
- Include minimal tests (happy path + one edge case) when modifying behavior.
- Follow existing project/solution structure; avoid creating new top-level patterns without consensus.

## References
- Build: `bash ./agent-build-fw.sh` or `msbuild FW.sln /m /p:Configuration=Debug`
- Tests: Use Test Explorer or `dotnet test` for SDK-style; NUnit console for .NET Framework assemblies.
- Localization: See `DistFiles/CommonLocalizations/` and `crowdin.json`.
