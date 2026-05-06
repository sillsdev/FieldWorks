# Quickstart: Render Performance Baselines

## Prerequisites

- Windows x64 with deterministic settings (fonts, DPI, theme).
- Debug build output available locally.
- No external services required.

## Build

```powershell
.\build.ps1
```

## Run the RootSite snapshot suite

```powershell
.\test.ps1 -TestProject "RootSiteTests" -TestFilter "FullyQualifiedName~RenderBaselineTests"
```

This validates the committed RootSite snapshot baselines and the shared snapshot verifier.

## Run the timing suite

```powershell
.\test.ps1 -TestProject "RootSiteTests" -TestFilter "FullyQualifiedName~RenderTimingSuiteTests"
```

Results are written to:

- `Output/RenderBenchmarks/results.json` for machine-readable timing data.
- `Output/RenderBenchmarks/summary.md` for the human-readable summary.

## Snapshot files and regeneration

Committed baselines live next to the tests that own them:

- `Src/Common/RootSite/RootSiteTests/*.verified.png`
- `Src/Common/Controls/DetailControls/DetailControlsTests/*.verified.png`
- `Src/Common/Controls/DetailControls/DetailControlsTests/*.verified.json` for scenarios that store extra metadata.

Transient verification outputs are ignored:

- `*.received.png`
- `*.diff.png`

To regenerate a committed baseline, delete the matching `*.verified.png` file and rerun the owning test suite.

## Timing baselines

`Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTimingBaselines.json` is local advisory data. When it is missing, timing threshold checks are skipped instead of failing CI.

## Trace diagnostics

Use `RenderDiagnosticsToggle` from the shared render infrastructure when a test run needs managed trace capture:

```csharp
using (var diagnostics = new RenderDiagnosticsToggle())
{
    diagnostics.EnableDiagnostics();
    // Run benchmark or snapshot capture.
    diagnostics.Flush();
    var traceContent = diagnostics.GetTraceLogContent();
}
```

`Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config` keeps the core xWorks trace switches enabled for Debug runs. Native `VwRenderTrace.h` timing output is still compile-time-gated by `TRACING_RENDER`; there is no always-on runtime `Views_RenderTiming` switch in the final configuration.

## Project layout

- `Src/Common/RenderTestInfrastructure/RenderTestInfrastructure.csproj` exposes the lightweight benchmark, snapshot, and trace helpers that test projects can reference broadly.
- The source for those reusable helpers lives under `Src/Common/RenderVerification/`.
- `Src/Common/RenderVerification/RenderVerification.csproj` adds the heavier DataTree and composite-capture pieces that depend on `DetailControls`.
- `Src/Common/RootSite/RootSiteTests/RenderBenchmarkHarness.cs` stays in `RootSiteTests` because it still depends on RootSite test-only view scaffolding.

## Environment requirements

For pixel-perfect validation to pass, the runtime environment must match the baseline:

- DPI typically 96x96 (100% scaling).
- Theme must match the captured baseline.
- Font smoothing state must match the baseline.
- Text scale factor should remain 100%.
