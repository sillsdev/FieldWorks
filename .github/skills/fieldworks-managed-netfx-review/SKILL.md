---
name: fieldworks-managed-netfx-review
description: Use when reviewing or changing FieldWorks managed C# projects that cross .NET Framework 4.8, C# 7.3, SDK-style net8, tests, or project-file boundaries.
---

# FieldWorks Managed NetFx Review

## Compatibility Split
- Legacy product code is .NET Framework 4.8 and C# 7.3 unless a project explicitly targets modern .NET.
- New Avalonia modules may target `net8.0-windows`; do not leak C# 8+ syntax or net8-only APIs into net48 projects.
- Legacy `.csproj` files require explicit source inclusion; SDK-style projects have different defaults.

## Required Checks
- User-visible strings use `.resx` patterns where product-facing.
- UI and async code marshals to the correct UI thread and does not use sync-over-async.
- Disposable WinForms/GDI/LCModel/test resources are owned and disposed deterministically.
- Test discovery changes must be validated across both net48 and net8 test assemblies.
- Use repo scripts for evidence: `./build.ps1` and `./test.ps1`.

## Review Red Flags
- Nullable annotations, records, file-scoped namespaces, switch expressions, or `using var` in net48/C# 7.3 projects.
- Broad project/test-runner changes justified only by one local test passing.
- Hardcoded Debug paths or absolute repo assumptions in tests.
- Skipped tests used as evidence of covered behavior.

## Handoff
Report target frameworks touched, project-file implications, test commands/results, and any remaining compatibility risks.