# Quickstart: Render Performance Baseline & Optimization Plan

## Prerequisites

- Windows environment with deterministic settings (fixed fonts, DPI, theme).
- FieldWorks Debug build available locally.
- No external services required.

## Building

```powershell
# Build the solution
.\build.ps1
```

## Running the Benchmark Suite

### Full Five-Scenario Suite

```powershell
# Run the complete timing suite (bootstraps snapshots if missing)
.\test.ps1 -TestProject "Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj" -TestFilter "FullyQualifiedName~RenderTimingSuiteTests"
```

Results will be written to:
- `Output/RenderBenchmarks/results.json` - Machine-readable benchmark data
- `Output/RenderBenchmarks/summary.md` - Human-readable summary

### Individual Scenario Tests

```powershell
# Run baseline tests (Unit Tests)
.\test.ps1 -TestProject "Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj" -TestFilter "FullyQualifiedName~RenderBaselineTests"
```

## Generating Baseline Snapshots

Snapshots are automatically generated (bootstrapped) if they do not exist. To regenerate a snapshot, delete the file in `Src/Common/RootSite/RootSiteTests/TestData/RenderSnapshots/` and run the suite again.

```powershell
# Example: Regenerate 'simple' snapshot
rm Src/Common/RootSite/RootSiteTests/TestData/RenderSnapshots/simple.png
.\test.ps1 -TestFilter "FullyQualifiedName~RenderTimingSuiteTests"
```

## Creating Reproducible Test Data

To create deterministic scenario data for testing:

1. **Simple Scenario**: Create a lexical entry with one sense and one definition.
2. **Medium Scenario**: Create an entry with 3 senses, each with definitions and example sentences.
3. **Complex Scenario**: Create an entry with 10+ senses, subsenses, and cross-references.
4. **Deep-Nested Scenario**: Create an entry with 5+ levels of nested subsenses.
5. **Custom-Field-Heavy Scenario**: Add custom fields of various types to an entry.

Use the LCModel test infrastructure to create in-memory test data that matches these patterns.

## Enabling Trace Diagnostics

Edit `TestData/RenderBenchmarkFlags.json`:

```json
{
  "diagnosticsEnabled": true,
  "traceEnabled": true,
  "captureMode": "DrawToBitmap"
}
```

Or enable programmatically in tests:

```csharp
using (var diagnostics = new RenderDiagnosticsToggle())
{
    diagnostics.EnableDiagnostics();
    // Run benchmark...
    diagnostics.Flush();
    var traceContent = diagnostics.GetTraceLogContent();
}
```

## Output Artifacts

| File | Description |
|------|-------------|
| `Output/RenderBenchmarks/results.json` | Complete benchmark data (all scenarios, timings, validation) |
| `Output/RenderBenchmarks/summary.md` | Human-readable summary with recommendations |
| `Output/RenderBenchmarks/render-trace.log` | Trace diagnostics (when enabled) |
| `Output/RenderBenchmarks/comparison.md` | Regression comparison (when baseline provided) |
| `TestData/RenderSnapshots/*.png` | Approved baseline snapshots |

## Environment Requirements

For pixel-perfect validation to pass, the environment must match the baseline:

- **DPI**: Typically 96x96 (100% scaling)
- **Theme**: Light or Dark (must match baseline)
- **Font Smoothing**: ClearType enabled/disabled must match
- **Text Scale Factor**: 100%

Check environment hash in snapshot `.environment.txt` files to verify compatibility.

