---
applyTo: "**/*"
name: "build.instructions"
description: "FieldWorks build guidelines and inner-loop tips"
---
# Build guidelines (FieldWorks)

## Purpose & Scope
This file documents the **supported** build workflow for FieldWorks.

FieldWorks is **Windows-first** and **x64-only**. Use the repo scripts so build ordering (native before managed) is correct.

## Quick start (PowerShell)
```powershell
# Full traversal build (Debug/x64 defaults)
.\build.ps1

# Release build
.\build.ps1 -Configuration Release
```

## Non-negotiable rules
- Use `.\build.ps1` for builds and `.\test.ps1` for tests.
- Avoid ad-hoc `msbuild`/`dotnet build` invocations unless you are explicitly debugging build infrastructure.
- Do not change COM/registry behavior without an explicit plan and tests.

## Warnings policy
- Treat *compiler* warnings as errors by default (fix warnings; don’t suppress them).
- Keep warning output **visible**. Avoid “make it quiet” changes that hide warnings without resolving the cause.
- Some MSBuild warnings are expected today due to external dependencies (notably `MSB3277`/`MSB3243` assembly version conflicts). These warnings are documented as informational in `Directory.Build.props`.
	- Do **not** attempt to suppress these with warning filters; if the dependency landscape changes, update the documentation and reassess.
- Other MSBuild warnings (for example `MSB3245`/`MSB3246` unresolved references / bad images) are **actionable**: fix the underlying reference or generation issue rather than suppressing the warning.

## Why build.ps1
FieldWorks uses an MSBuild traversal (`FieldWorks.proj`) with ordered phases.

Key ordering constraint:
- **Native C++ must build first** (Phase 2) because later managed code-generation depends on native artifacts.

## Outputs
- Build outputs: `Output/<Configuration>/` (for example `Output/Debug/`)
- Intermediate outputs: `Obj/<ProjectName>/` (centralized; do not rely on per-project `obj/` folders)

## Worktrees and concurrent builds
This repo supports multiple concurrent builds across git worktrees. Prefer the scripts because they handle environment setup and avoid cross-worktree process conflicts.

## Troubleshooting (common)

### “Native artifacts missing” / code-generation failures
Re-run the scripted build:
```powershell
.\build.ps1
```
If the failure persists, check the first error (later ones often cascade) and confirm native Phase 2 succeeds.

### Stale intermediates
Prefer re-running `.\build.ps1` first (it performs cleanup needed for this repo). If you must do a hard reset, delete `Output/` and `Obj/`, then run `.\build.ps1` again.

## References
- Traversal order: `FieldWorks.proj`
- Build infrastructure: `Build/`
- Tests: `.github/instructions/testing.instructions.md`

## CLARIFICATIONS_NEEDED
- Confirm whether any MSBuild warning codes other than `MSB3277`/`MSB3243` are considered acceptable, and under what conditions.
