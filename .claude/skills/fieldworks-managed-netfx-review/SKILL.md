---
name: fieldworks-managed-netfx-review
description: "Review or change FieldWorks managed C# code that crosses the .NET Framework 4.8 / C# 7.3 vs SDK-style net8 boundary: project files, language-feature compatibility, test discovery across both runtimes, UI-thread marshaling, and deterministic disposal. Use whenever a change touches a .csproj, adds C# to an unfamiliar project, moves code between net48 and net8 projects, or changes test runners — even if the compile passes locally."
---

# FieldWorks Managed NetFx Review

## Compatibility Split

- Legacy product code is .NET Framework 4.8 and C# 7.3 unless a project
  explicitly targets modern .NET. The compiler will not always save you:
  check the project's `LangVersion`/target before writing modern syntax.
- New Avalonia modules may target `net8.0-windows`; do not leak C# 8+
  syntax or net8-only APIs into net48 projects. Note that
  `Src/Common/FwAvalonia/` itself is consumed from net48 hosts — verify a
  project's actual target rather than assuming Avalonia ⇒ net8.
- Legacy `.csproj` files require explicit source inclusion; SDK-style
  projects glob by default. A file added on disk is not necessarily in the
  build.
- SDK-style projects need an explicit `<RootNamespace>` for the Crowdin
  satellite-assembly build (see `fieldworks-localization-review`).

## Required Checks

- User-visible strings use `.resx` patterns where product-facing.
- UI and async code marshals to the correct UI thread (via `IUiScheduler`
  in region code) and does not use sync-over-async.
- Disposable WinForms/GDI/LCModel/test resources are owned and disposed
  deterministically; region code follows the `IRegionLifetime` rules
  (idempotent disposal, late-callback suppression, event unsubscribe).
- Test discovery changes are validated across both net48 and net8 test
  assemblies.
- Use repo scripts for evidence: `./build.ps1` and `./test.ps1` — never
  bare `dotnet build` conclusions.

## Review Red Flags

- Nullable annotations, records, file-scoped namespaces, switch
  expressions, or `using var` in net48/C# 7.3 projects.
- Broad project/test-runner changes justified only by one local test
  passing.
- Hardcoded Debug paths or absolute repo assumptions in tests.
- Skipped tests used as evidence of covered behavior.
- A new project added to disk but missing from `FieldWorks.proj`
  (traversal build) or `FieldWorks.sln` (IDE discovery).

## Handoff

Report target frameworks touched, project-file implications, test
commands/results, and any remaining compatibility risks.

## Keep This Skill Current

When a new cross-target pitfall, project-file gotcha, or runtime
difference bites a migration, add it here in the same PR; route durable
lessons through
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
