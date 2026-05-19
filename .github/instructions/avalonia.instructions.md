---
applyTo: "**/*"
name: "avalonia.instructions"
description: "Guidance for FieldWorks Avalonia modules and the shared Preview Host"
---

# Avalonia Modules (FieldWorks)

## Purpose & Scope
- Provide a consistent way to **create, build, test, and preview** Avalonia UI modules in FieldWorks.
- Applies to the Advanced Entry Avalonia work under `specs/010-advanced-entry-view/` and future Avalonia modules.

## Key Rules

### Build & test (always use repo scripts)
- Build the repo using the traversal script:
  - `./build.ps1`
- Run tests using the repo test runner:
  - `./test.ps1`
- Do **not** rely on `dotnet build` for repo-wide builds; FieldWorks build targets include tasks that require full Visual Studio/MSBuild.

### Project locations & naming
- Feature modules live under `Src/<Area>/<Feature>.Avalonia/`.
  - Example: `Src/LexText/AdvancedEntry.Avalonia/`
- Shared Avalonia utilities live under `Src/Common/FwAvalonia/`.
- Preview tooling lives under `Src/Common/FwAvaloniaPreviewHost/`.

### Solution + traversal integration (required)
For every new Avalonia module or tool:
- Add the project(s) to the traversal build so `./build.ps1` and `./test.ps1` naturally cover them:
  - `FieldWorks.proj`
- Add the project(s) to the solution so developers can open/build/debug in Visual Studio:
  - `FieldWorks.sln`

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
  - `./scripts/Agent/Run-AvaloniaPreview.ps1 -Module advanced-entry -Data sample`
- Supported `-Data` modes depend on the module’s data provider; the current convention is:
  - `empty` (minimal/default DataContext)
  - `sample` (representative sample data)

## Expected Structure (current)

- Module:
  - `Src/LexText/AdvancedEntry.Avalonia/`
- Shared utilities/contracts:
  - `Src/Common/FwAvalonia/`
    - `Diagnostics/` (logging shim)
    - `Preview/` (module registration + data provider contracts)
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

### Preview the Advanced Entry module
```powershell
./scripts/Agent/Run-AvaloniaPreview.ps1 -Module advanced-entry -Data sample
```

## Notes & Constraints
- Avalonia modules should remain **detached from LCModel** for preview scenarios (use DTO/view-model sample data) to keep the Preview Host lightweight.
- Keep all user-visible strings localizable (use `.resx` patterns where applicable; do not hardcode translatable UI text).
- Treat any input that crosses managed/native boundaries as untrusted; sanitize and validate per repo security guidance.
