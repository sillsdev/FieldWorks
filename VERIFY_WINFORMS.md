# FieldWorks Verify.WinForms Integration Plan

## Executive Summary

**Feasibility: High. Core infrastructure is proven.**

FieldWorks uses a hybrid architecture where the C++ rendering engine (`Views.dll`) paints onto a managed .NET `UserControl` (`SimpleRootSite`) via `OnPaint` and GDI HDCs. We have built a working test harness that captures pixel-perfect bitmaps from this pipeline using `IVwDrawRootBuffered`, bypassing the unreliable `DrawToBitmap` path entirely. The harness renders richly styled Scripture content (bold section headings, colored chapter/verse numbers, indented paragraphs) through the production `StVc` view constructor against a real file-backed `LcmCache`.

The next step is replacing our hand-rolled baseline comparison infrastructure with [Verify](https://github.com/VerifyTests/Verify), which provides diff tooling, automatic `.verified.png` management, and seamless NUnit integration — all compatible with .NET Framework 4.8.

---

## Current State (Phases 1-2 Complete)

### What's Working

| Component | File | Status |
|-----------|------|--------|
| Real-data test base | `RootSiteTests/RealDataTestsBase.cs` | **Done** — Spins up temporary `.fwdata` projects via `FwNewLangProjectModel` |
| Scripture style creation | `RootSiteTests/RenderBenchmarkTestsBase.cs` | **Done** — Creates `Paragraph`, `Section Head`, `Chapter Number`, `Verse Number`, `Title Main` styles with full formatting rules (bold, colors, sizes, alignment, superscript) |
| Rich test data | `RootSiteTests/RenderBenchmarkTestsBase.cs` | **Done** — `AddRichSections()` generates section headings, chapter numbers, verse numbers, and varied prose text |
| View constructor | `RootSiteTests/GenericScriptureView.cs` | **Done** — `GenericScriptureVc` extends `StVc`, handles Book→Sections→Heading+Content hierarchy |
| Bitmap capture | `RootSiteTests/RenderBenchmarkHarness.cs` | **Done** — Uses `VwDrawRootBuffered.DrawTheRoot()` to render RootBox directly to GDI+ bitmap |
| Offscreen layout | `RootSiteTests/RenderBenchmarkHarness.cs` | **Done** — `VwGraphicsWin32` drives layout without a visible window |
| Baseline comparison | `RootSiteTests/RenderBitmapComparer.cs` | **Done** — Pixel-diff with diff-image generation |
| Environment validation | `RootSiteTests/RenderEnvironmentValidator.cs` | **Done** — DPI/theme/font hashing for deterministic checks |
| Content density check | `RootSiteTests/RenderBaselineTests.cs` | **Done** — `ValidateBitmapContent()` ensures >0.4% non-white pixels (currently hitting 6.3%) |
| All tests passing | 8/8 RenderBaselineTests | **Done** — Bootstrap mode creates baseline, subsequent runs compare |

### Key Metrics
- **Content density**: 6.30% non-white pixels (up from 0.46% with plain text)
- **Cold render**: ~260ms (includes view creation, MakeRoot, layout)
- **Warm render**: ~180ms (Reconstruct + relayout)
- **Test suite time**: ~30s for all 8 tests

### Problems Solved

1. **Mock vs Real data** — `ScrInMemoryLcmTestBase` lacked the deep metadata `StVc` needs. Solved by creating real XML-backed projects via `FwNewLangProjectModel`.

2. **Missing styles = plain black text** — New projects from the template have no Scripture styles. The Views engine fell back to unformatted defaults. Solved by manually creating styles with `IStStyleFactory.Create()` and setting `Rules` via `ITsPropsBldr`.

3. **`DrawToBitmap` failures** — WinForms `DrawToBitmap` returns black for Views controls because painting goes through native `HDC` interop. Solved by using `VwDrawRootBuffered` to render the `IVwRootBox` directly to an offscreen bitmap.

4. **Handle creation crash** — `SimpleRootSite.OnHandleCreated` called `MakeRoot()` with wrong fragment before test data was ready. Solved by setting `m_fMakeRootWhenHandleIsCreated = false` and calling `MakeRoot()` explicitly with the correct parameters.

---

## 10-Point Comparison & Analysis

### 1. Rendering Architecture Compatibility
- **Verify.WinForms** uses `Control.DrawToBitmap` internally, which **does not work** for FieldWorks Views controls.
- **Our approach**: Use `IVwDrawRootBuffered` to render the `IVwRootBox` directly to a `System.Drawing.Bitmap`, then pass the raw bitmap to Verify for snapshot management.
- **We don't need `Verify.WinForms`** — only `Verify.NUnit` for bitmap stream verification.

### 2. Handling Complex Scripts & Text Rendering
- Snapshot testing catches subtle kerning, ligature, and line-breaking regressions that are impossible to assert programmatically.
- Our harness already renders through the full production pipeline (Graphite, Uniscribe, ICU) via `StVc`.

### 3. Test Harness Complexity (Solved)
- `RealDataTestsBase` creates temporary `.fwdata` projects with full schema.
- `RenderBenchmarkTestsBase` populates Scripture styles and rich data.
- `RenderBenchmarkHarness` handles view lifecycle and bitmap capture.

### 4. GDI Handle Management
- `PerformOffscreenLayout()` and `CaptureViewBitmap()` carefully acquire and release HDCs in try/finally blocks.
- View disposal cascades through Form → Control cleanup.

### 5. Cross-Platform / OS Rendering Differences
- Font rendering varies with ClearType settings, DPI, and Windows version.
- `RenderEnvironmentValidator` hashes these settings so tests can detect environment drift.
- For CI, consider `Verify.Phash` (perceptual hashing) if pixel-exact comparison is too strict.

### 6. Tooling Integration
- **NUnit** is already the project's test framework.
- **Verify.NUnit** provides `[VerifyAttribute]` and `Verifier.Verify()` for net48.
- Packages target `net48` explicitly.

### 7. Performance
- ~260ms cold, ~180ms warm per render. Acceptable for integration tests.
- Data setup adds ~3s per test (project creation + DB population).

### 8. Failure Diagnostics
- Verify automatically opens a diff tool on mismatch.
- `.received.png` vs `.verified.png` side-by-side comparison.
- Our `RenderBitmapComparer` can still generate diff images as supplementary evidence.

### 9. Custom Control Support
- Not needed. We bypass `Control.DrawToBitmap` entirely and verify raw bitmaps.

### 10. Fallback Strategy
- If Verify integration has issues, our existing `RenderBitmapComparer` + `simple.png` baseline approach is fully functional.
- Migration is incremental — we can keep both systems running in parallel.

---

## Implementation Plan

### Phase 1: Stabilization ✅ COMPLETE
1. ~~Revert hacks~~ — `StVc.cs` is clean; `GenericScriptureView.cs` uses proper `MakeRoot` override.
2. ~~Clean slate~~ — All 8 tests pass with rich formatted rendering.

### Phase 2: Test Harness "Real Data" ✅ COMPLETE
1. ~~Create `RealDataTestsBase`~~ — `Src/Common/RootSite/RootSiteTests/RealDataTestsBase.cs`
2. ~~Refactor Benchmark~~ — `RenderBenchmarkTestsBase` inherits from `RealDataTestsBase`
3. ~~Scripture styles~~ — `CreateScriptureStyles()` populates `Paragraph`, `Section Head`, `Chapter Number`, `Verse Number`, `Title Main`
4. ~~Rich data~~ — `AddRichSections()` creates headings, chapter/verse markers, varied prose

### Phase 3: Verify Integration (Next)

#### 3.1 Install NuGet Package
Add to `RootSiteTests.csproj`:
```xml
<PackageReference Include="Verify.NUnit" Version="31.11.0" />
```

**Not needed**: `Verify.WinForms` — we capture bitmaps directly via `IVwDrawRootBuffered`.

#### 3.2 Configure Verify Settings

Create `RootSiteTests/VerifySetup.cs` using NUnit's `[SetUpFixture]` (required because FieldWorks uses C# 8.0 / `LangVersion=8.0` globally, so `[ModuleInitializer]` from C# 9 is unavailable):

```csharp
[SetUpFixture]
public class VerifySetup
{
    [OneTimeSetUp]
    public void Setup()
    {
        // Use Verify's default PNG comparison for bitmap streams
        VerifierSettings.ScrubEmptyLines();

        // Don't auto-open diff tool in CI
        if (Environment.GetEnvironmentVariable("CI") != null)
            DiffRunner.Disabled = true;
    }
}
```

#### 3.3 Register Custom Bitmap Converter

Register a typed converter so `Verify(bitmap)` works directly:

```csharp
VerifierSettings.RegisterFileConverter<Bitmap>(
    conversion: (bitmap, context) =>
    {
        var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return new ConversionResult(null, "png", stream);
    });
```

#### 3.4 Create Verify-Based Test

Add `RenderVerifyTests.cs` alongside existing `RenderBaselineTests.cs`:

```csharp
[TestFixture]
[Category("RenderBenchmark")]
public class RenderVerifyTests : RenderBenchmarkTestsBase
{
    protected override void CreateTestData() => SetupScenarioData("simple");

    [Test]
    public Task SimpleScenario_MatchesVerifiedSnapshot()
    {
        var scenario = new RenderScenario
        {
            Id = "simple",
            RootObjectHvo = m_hvoRoot,
            RootFlid = m_flidContainingTexts,
            FragmentId = m_frag
        };

        using var harness = new RenderBenchmarkHarness(Cache, scenario);
        harness.ExecuteColdRender();
        var bitmap = harness.CaptureViewBitmap();

        return Verifier.Verify(bitmap);
        // First run: creates RenderVerifyTests.SimpleScenario_MatchesVerifiedSnapshot.verified.png
        // Subsequent runs: compares against verified file
    }
}
```

#### 3.5 Verified Files Location

Verify stores `.verified.png` files next to the test source. Structure:
```
RootSiteTests/
├── RenderVerifyTests.cs
├── RenderVerifyTests.SimpleScenario_MatchesVerifiedSnapshot.verified.png
├── RenderVerifyTests.MediumScenario_MatchesVerifiedSnapshot.verified.png
└── ...
```

These files are committed to source control and serve as the approved baselines.

#### 3.6 Git Configuration

Add to `.gitattributes`:
```
*.verified.png binary
*.received.png binary
```

Add to `.gitignore`:
```
*.received.png
```

### Phase 4: Pilot Test

1. **Run** `SimpleScenario_MatchesVerifiedSnapshot` — first run creates `.received.png`.
2. **Accept** — Copy `.received.png` to `.verified.png` (Verify's diff tool prompts this).
3. **Commit** the `.verified.png` file.
4. **Re-run** — test should pass, comparing against the committed baseline.

### Phase 5: Expand Coverage

| Scenario | Data | What It Tests |
|----------|------|---------------|
| `simple` | 3 sections, 4 verses each | Basic formatting pipeline |
| `medium` | 5 sections, 6 verses each | Style resolution at scale |
| `complex` | 10 sections, 8 verses each | Layout performance and wrapping |
| `deep-nested` | 3 sections, 12 verses each | Dense content in single paragraphs |
| `rtl-script` | Arabic/Hebrew text | Bidirectional layout |
| `multi-ws` | Mixed writing systems | Font fallback and WS switching |

### Phase 6: Deprecate Hand-Rolled Comparison

Once Verify-based tests are stable:
1. Remove `RenderBitmapComparer.cs` and hand-rolled diff logic.
2. Remove `TestData/RenderSnapshots/` directory (replaced by `.verified.png` files).
3. Keep `RenderBenchmarkHarness` and `RenderEnvironmentValidator` — they're the capture infrastructure.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                    Test Layer                        │
│                                                     │
│  RenderVerifyTests ──→ Verifier.Verify(bitmap)      │
│        ↓                      ↓                     │
│  RenderBenchmarkTestsBase   .verified.png           │
│    (styles + data)          (committed baseline)    │
│        ↓                                            │
│  RealDataTestsBase                                  │
│    (FwNewLangProjectModel)                          │
└─────────────┬───────────────────────────────────────┘
              │
┌─────────────▼───────────────────────────────────────┐
│              Capture Layer                           │
│                                                     │
│  RenderBenchmarkHarness                             │
│    ├── CreateView() → GenericScriptureView          │
│    │     └── GenericScriptureVc (extends StVc)      │
│    ├── ExecuteColdRender() → MakeRoot + Layout      │
│    │     └── VwGraphicsWin32 offscreen layout       │
│    └── CaptureViewBitmap()                          │
│          └── VwDrawRootBuffered.DrawTheRoot()        │
│               → System.Drawing.Bitmap               │
└─────────────┬───────────────────────────────────────┘
              │
┌─────────────▼───────────────────────────────────────┐
│           Views Engine (C++ / COM)                   │
│                                                     │
│  IVwRootBox → IVwRootSite                           │
│  StVc (ApplyParagraphStyleProps, InsertParagraphBody)│
│  IVwStylesheet (LcmStyleSheet)                      │
│  IVwDrawRootBuffered (GDI HDC → Bitmap)             │
└─────────────────────────────────────────────────────┘
```

---

## Risk Register

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| 1 | Verify package breaks on net48 in future | Low | High | Pin version; Verify has explicit net48 support |
| 2 | Pixel differences across machines | Medium | Medium | Use `RenderEnvironmentValidator` hash; consider `Verify.Phash` for fuzzy matching |
| 3 | Test data changes break baselines | Medium | Low | Re-accept `.verified.png`; clear separation between data setup and rendering |
| 4 | GDI handle leaks in long test runs | Low | High | Proven try/finally pattern in harness; monitor in CI |
| 5 | Charis SIL font not installed on CI | Medium | High | Add font dependency to CI setup or use system default with known metrics |

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-03 | Use `RealDataTestsBase` over mock data | `ScrInMemoryLcmTestBase` lacks metadata for StVc |
| 2026-02-03 | Use `IVwDrawRootBuffered` not `DrawToBitmap` | WinForms DrawToBitmap returns black for Views controls |
| 2026-02-05 | Override `MakeRoot` in GenericScriptureView | Prevent auto-MakeRoot on handle creation with wrong fragment |
| 2026-02-05 | Create Scripture styles manually via `IStStyleFactory` | Avoids xWorks assembly dependency; FlexStyles.xml not loaded in test projects |
| 2026-02-05 | Use `Verify.NUnit` only, skip `Verify.WinForms` | We capture bitmaps ourselves; Verify.WinForms uses DrawToBitmap internally |
| 2026-02-05 | Use `[SetUpFixture]` not `[ModuleInitializer]` | FieldWorks uses C# 8.0 (LangVersion=8.0); ModuleInitializer requires C# 9 |
