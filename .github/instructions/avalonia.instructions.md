---
applyTo: "**/*"
name: "avalonia.instructions"
description: "Guidance for FieldWorks Avalonia modules and the shared Preview Host"
---

# Avalonia Modules (FieldWorks)

## Purpose & Scope
- Provide a consistent way to **create, build, test, and preview** Avalonia UI modules in FieldWorks.
- Applies to FieldWorks' Avalonia modules (the `FwAvalonia`/`FwAvaloniaDialogs` lexical-edit surfaces and their Preview Host) and future Avalonia work.
- This file covers mechanics (build, layout, logging, preview). The
  migration playbook, decided architecture patterns, and parity/evidence
  rules live in the skills under `.claude/skills/` — start with
  `fieldworks-winforms-to-avalonia-migration` (hub) and
  `fieldworks-avalonia-ui`.

## Key Rules

### Build & test (always use repo scripts)
- Build the repo using the traversal script:
  - `./build.ps1`
- Run tests using the repo test runner:
  - `./test.ps1`
- Do **not** rely on `dotnet build` for repo-wide builds; FieldWorks build targets include tasks that require full Visual Studio/MSBuild.

### Project locations & naming
- A new, self-contained feature module should live under `Src/<Area>/<Feature>.Avalonia/`.
- Shared Avalonia utilities live under `Src/Common/FwAvalonia/` (region/composer framework, seams,
  view-definition IR) and `Src/Common/FwAvaloniaDialogs/` (the MVVM dialog kit).
- Preview tooling lives under `Src/Common/FwAvaloniaPreviewHost/`.

### Solution + traversal integration (required)
For every new Avalonia module or tool:
- Add the project(s) to the solution so developers can open/build/debug in Visual Studio:
  - `FieldWorks.sln`
- The traversal build picks up new projects automatically via `FieldWorks.proj`'s
  `Src\**\*.csproj` glob once they are added to `FieldWorks.sln` — verify solution
  membership rather than editing `FieldWorks.proj`.

### Logging (use FieldWorks diagnostics)
- Module logging must route through the existing FieldWorks diagnostics pipeline (`System.Diagnostics`, `TraceSwitch`, `EnvVarTraceListener`).
- Add a `TraceSwitch` entry for each module/component in the dev diagnostics config:
  - `Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config`

### Preview Host diagnostics (log file)
- The Preview Host writes startup errors and trace output to a log file next to the executable:
  - `Output/<Configuration>/FieldWorks.trace.log` (e.g. `Output/Debug/FieldWorks.trace.log`)
- To override the log path, set environment variable `FW_PREVIEW_TRACE_LOG` to a full file path.

### Preview Host (fast UI iteration)
To preview UI without launching the full FieldWorks app, use the shared Preview Host.

**How modules opt-in**
- Register the module using an assembly-level attribute:
  - `FwPreviewModuleAttribute` in `Src/Common/FwAvalonia/Preview/`
- Provide an optional data provider implementing:
  - `IFwPreviewDataProvider`

**Run the preview**
- Use the agent script (build + run):
  - `./scripts/Agent/Run-AvaloniaPreview.ps1 -Module lexical-edit-preview -Data sample`
- Supported `-Data` modes depend on the module’s data provider; the current convention is:
  - `empty` (minimal/default DataContext)
  - `sample` (representative sample data)

## Expected Structure (current)

- Shared utilities/contracts:
  - `Src/Common/FwAvalonia/`
    - `Region/` (composer, region model, field editors)
    - `ViewDefinition/` (typed IR compiled from legacy XML layouts)
    - `Seams/` (framework-neutral interfaces)
    - `Preview/` (module registration + data provider contracts, e.g. `AssemblyPreviewModules.cs`
      registers the `lexical-edit-preview` module)
- Dialog kit:
  - `Src/Common/FwAvaloniaDialogs/`
- Preview host executable:
  - `Src/Common/FwAvaloniaPreviewHost/`
- Launcher script:
  - `scripts/Agent/Run-AvaloniaPreview.ps1`

## Examples

### Build everything (recommended)
```powershell
./build.ps1
```

### Run tests
```powershell
./test.ps1
```

### Preview the Lexical Edit module
```powershell
./scripts/Agent/Run-AvaloniaPreview.ps1 -Module lexical-edit-preview -Data sample
```

## Notes & Constraints
- Avalonia modules should remain **detached from LCModel** for preview scenarios (use DTO/view-model sample data) to keep the Preview Host lightweight.
- Keep all user-visible strings localizable (use `.resx` patterns where applicable; do not hardcode translatable UI text).
- Treat any input that crosses managed/native boundaries as untrusted; sanitize and validate per repo security guidance.
