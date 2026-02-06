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
| All tests passing | 34/34 Render tests | **Done** — 4 baseline infra + 15 Verify snapshots + 15 timing suite |

### Key Metrics
- **Content density**: 6.30% non-white pixels (up from 0.46% with plain text)
- **Cold render**: ~147ms (includes view creation, MakeRoot, layout)
- **Warm render**: ~107ms (Reconstruct + relayout)
- **Test suite time**: ~40s for all 34 tests (4 baseline infra + 15 Verify + 15 timing suite)

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

### Phase 3: Verify Integration ✅ COMPLETE

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

### Phase 4: Pilot Test ✅ COMPLETE

1. ~~**Run**~~ `SimpleScenario_MatchesVerifiedSnapshot` — first run created `.received.png`.
2. ~~**Accept**~~ — Copied `.received.png` to `.verified.png`.
3. ~~**Commit**~~ the `.verified.png` file.
4. ~~**Re-run**~~ — test passes, comparing against the committed baseline.

### Phase 5: Expand Coverage ✅ COMPLETE

| Scenario | Data | What It Tests |
|----------|------|---------------|
| `simple` | 3 sections, 4 verses each | Basic formatting pipeline |
| `medium` | 5 sections, 6 verses each | Style resolution at scale |
| `complex` | 10 sections, 8 verses each | Layout performance and wrapping |
| `deep-nested` | 3 sections, 12 verses each | Dense content in single paragraphs |
| `custom-heavy` | 5 sections, 8 verses each | Mixed style properties |
| `many-paragraphs` | 50 sections, 1 verse each | Paragraph layout overhead |
| `footnote-heavy` | 8 sections, 20 verses + footnotes | Footnote rendering path |
| `mixed-styles` | 6 sections, unique formatting per verse | Style resolver stress |
| `long-prose` | 4 sections, 80 verses each | Line-breaking computation |
| `multi-book` | 3 books, 5 sections each | Large cache stress |
| `rtl-script` | Arabic/Hebrew text | Bidirectional layout |
| `multi-ws` | Mixed writing systems | Font fallback and WS switching |

### Phase 6: Deprecate Hand-Rolled Comparison ✅ COMPLETE

Completed:
1. ~~Remove `RenderBitmapComparer.cs` and hand-rolled diff logic.~~ — Deleted.
2. ~~Remove `TestData/RenderSnapshots/` directory (replaced by `.verified.png` files).~~ — Cleared.
3. Keep `RenderBenchmarkHarness` and `RenderEnvironmentValidator` — they're the capture infrastructure.
4. `RenderBaselineTests` trimmed to 4 infrastructure tests (harness, warm/cold, environment, diagnostics).
5. `RenderTimingSuiteTests` uses content-density sanity check instead of pixel comparison.

### Phase 7: Lexical Entry Benchmarks ✅ COMPLETE

**Motivation**: The primary speed-up target is deeply nested lexical entry rendering. In production, `XmlVc.ProcessPartRef` with `visibility="ifdata"` causes every part to render its entire subtree **twice** — once via `TestCollectorEnv` to check if data exists, then again into the real `IVwEnv`. With recursive `LexSense → Senses → LexSense` layouts and ~15 ifdata parts per sense, this creates O(N·2^d) work. The benchmarks track rendering time at varying nesting depths to quantify speedup from optimizations.

Completed:
1. Created `GenericLexEntryView.cs` with custom `LexEntryVc : VwBaseVc` — exercises the same recursive nested-field pattern as `XmlVc` with configurable `SimulateIfDataDoubleRender` flag for modeling the ifdata overhead.
2. Extended `RenderBenchmarkHarness` with `RenderViewType` enum (`Scripture`, `LexEntry`) and split view creation into `CreateScriptureView()` / `CreateLexEntryView()`.
3. Added 3 lex entry scenarios to `RenderBenchmarkTestsBase` — `lex-shallow` (depth 2, breadth 3 = 12 senses), `lex-deep` (depth 4, breadth 2 = 30 senses), `lex-extreme` (depth 6, breadth 2 = 126 senses).
4. Added `CreateLexEntryScenario()` and `CreateNestedSenses()` — creates realistic lex entry data with headword, glosses, definitions, and recursively nested subsenses using `ILexEntryFactory`/`ILexSenseFactory`/`IMoStemAllomorphFactory`.
5. Extended `RenderBenchmarkScenarios.json` with 3 new lex scenarios (with `viewType: "LexEntry"`).
6. Updated both `RenderTimingSuiteTests` and `RenderVerifyTests` to propagate `ViewType` and `SimulateIfDataDoubleRender` from config.
7. Accepted 3 new `.verified.png` baselines for lex entry scenarios.

**Lex Entry Rendering Metrics**:
| Scenario | Senses | Depth | Non-white density |
|----------|--------|-------|-------------------|
| `lex-shallow` | 12 | 2 | 5.83% |
| `lex-deep` | 30 | 4 | 6.78% |
| `lex-extreme` | 126 | 6 | 6.94% |

### Phase 8: Shared Render Verification Library _(PLANNED)_

**Vision**: Extract the render capture/comparison engine into a shared class library (`RenderVerification`) that any test project can consume. This library does not contain tests itself — it provides the infrastructure to create views, render them to bitmaps, and compare against established baselines. The long-term goal is for significant parts of the FieldWorks view architecture to have unit/integration test verification through this library.

**Location**: `Src/Common/RenderVerification/RenderVerification.csproj` (class library, not test project)

**What moves from RootSiteTests → RenderVerification**:
- `RenderBenchmarkHarness` — Core bitmap capture via `VwDrawRootBuffered`
- `RenderEnvironmentValidator` — DPI/theme/font determinism checks
- `RenderDiagnosticsToggle` — Trace switch management
- `RenderBenchmarkReportWriter` — Timing report generation
- `GenericScriptureView` + `GenericScriptureVc` — Scripture rendering path
- `GenericLexEntryView` + `LexEntryVc` — Views-only lex entry rendering path
- `RenderScenario`, `RenderTimingResult`, `RenderViewType` — Shared model classes
- `RenderScenarioDataBuilder` — JSON scenario loading
- Verify `InnerVerifier` integration wrapper

**What's new in RenderVerification**:
- `DataTreeRenderHarness` — Creates a `DataTree` with production layout inventories, populates it via `ShowObject()`, captures full-view bitmaps including all WinForms chrome
- `CompositeViewCapture` — Multi-pass bitmap capture:
  1. `DrawToBitmap` on the `DataTree` control to capture WinForms chrome (labels, grey backgrounds, splitters, expand/collapse icons, section headers)
  2. Iterate `ViewSlice` children, render each `RootBox` via `VwDrawRootBuffered` into the correct region
  3. Composite the Views-rendered text over the WinForms chrome bitmap
- `RenderViewType.DataTree` enum value — New pipeline for full DataTree/Slice rendering
- Layout inventory helpers — Load production `.fwlayout` and `*Parts.xml` from `DistFiles/Language Explorer/Configuration/Parts/`

**Dependencies** (RenderVerification.csproj references):
- `DetailControls.csproj` (brings DataTree, Slice, SliceFactory, ViewSlice, SummarySlice, etc.)
- `RootSite.csproj` / `SimpleRootSite.csproj` (base view rendering)
- `ViewsInterfaces.csproj` (IVwRootBox, IVwDrawRootBuffered)
- `xCore.csproj` + `xCoreInterfaces.csproj` (Mediator, PropertyTable — required by DataTree)
- `XMLViews.csproj` (XmlVc, LayoutCache, Inventory — required by DataTree)
- `FwUtils.csproj` (FwDirectoryFinder for locating DistFiles)
- Verify (31.11.0) NuGet package
- SIL.LCModel packages

**Consumer pattern** (how test projects use it):
```csharp
// In any test project:
using SIL.FieldWorks.Common.RenderVerification;

// Views-only capture (existing):
using (var harness = new RenderBenchmarkHarness(cache, scenario))
{
    harness.ExecuteColdRender();
    var bitmap = harness.CaptureViewBitmap();
}

// Full DataTree capture (new):
using (var harness = new DataTreeRenderHarness(cache, entry, "Normal"))
{
    harness.PopulateSlices();
    var bitmap = harness.CaptureCompositeBitmap(); // WinForms chrome + Views content
    // bitmap includes grey labels, icons, expand/collapse, AND rendered text
}
```

Steps:
1. Create `Src/Common/RenderVerification/RenderVerification.csproj` with dependencies
2. Move existing harness classes from `RootSiteTests` → `RenderVerification`
3. Update `RootSiteTests.csproj` to reference `RenderVerification` instead of containing the infrastructure directly
4. Implement `DataTreeRenderHarness` with composite bitmap capture
5. Implement `CompositeViewCapture` (DrawToBitmap + VwDrawRootBuffered overlay)
6. Add layout inventory loading from `DistFiles/` production XML
7. Verify all existing tests still pass after extraction

### Phase 9: DataTree Full-View Verification Tests _(PLANNED)_

**Motivation**: The full lexical entry edit view (as seen in FLEx) includes WinForms UI chrome that is critical to verify: grey field labels ("Lexeme Form", "Citation Form"), writing system indicators ("Frn", "FrIPA"), expand/collapse tree icons, section headers ("Sense 1 — to do - v"), separator lines, indentation of nested senses, "Variants"/"Allomorphs"/"Grammatical Info. Details" sections. These elements are rendered by the `DataTree`/`Slice` system and are part of the rendering pipeline that must be pixel-perfect and fully exercised.

**Location**: New test class(es) in `DetailControlsTests` (already references `DetailControls.csproj`, `xCore`, etc.) OR a new dedicated test project that references `RenderVerification`.

**Test scenarios**:
| Scenario ID | Description | What it exercises |
|-------------|-------------|-------------------|
| `datatree-lex-simple` | Lex entry with 3 senses, default layout | Basic DataTree population + slice chrome |
| `datatree-lex-deep` | 4-level nested senses, all expanded | Recursive slice indentation + tree lines |
| `datatree-lex-extreme` | 6-level nesting, 126 senses | Scrollable DataTree stress + slice count |
| `datatree-lex-collapsed` | All sense sections collapsed | SummarySlice rendering + expand icons |
| `datatree-lex-expanded` | All sections expanded including Variants, Allomorphs | Full slice set, all chrome visible |
| `datatree-lex-multiws` | Entry with Frn + FrIPA + Eng writing systems | WS indicator labels, MultiStringSlice layout |

**What's captured per scenario**:
- Full composite bitmap (WinForms chrome + Views text content)
- `.verified.png` baseline via Verify for pixel-perfect regression
- Timing: DataTree population (CreateSlices), layout, composite capture
- Slice count and type distribution (how many MultiStringSlice, ViewSlice, SummarySlice, etc.)

**What the bitmaps should show** (matching the FLEx screenshot):
- Blue/grey header bar with "Entry" label _(if contained in DataTree)_
- Summary line with formatted headword, grammar, sense numbers
- "Lexeme Form" / "Citation Form" grey labels with WS indicators
- Sense summary lines ("Sense 1 — to do - v") with expand/collapse buttons
- Indented subsense sections
- "Variants" / "Allomorphs" / "Grammatical Info. Details" / "Publication Settings" headers
- Grey backgrounds, separator lines, tree indentation lines

Steps:
1. Add `Verify` (31.11.0) NuGet package to `DetailControlsTests.csproj`
2. Create `DataTreeRenderTests.cs` — Verify snapshot tests for each DataTree scenario
3. Create `DataTreeTimingTests.cs` — Timing benchmarks measuring DataTree population + rendering
4. Create shared data factory for lex entries with controlled structure (headword, senses, glosses, variants, allomorphs)
5. Load production layout inventories from `DistFiles/` for realistic slice generation
6. Accept initial `.verified.png` baselines
7. Validate all scenarios produce non-trivial bitmaps (content density check)

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Test Projects (consumers)                           │
│                                                                             │
│  RootSiteTests/                       DetailControlsTests/                  │
│    RenderVerifyTests                    DataTreeRenderTests (Phase 9)       │
│    RenderTimingSuiteTests               DataTreeTimingTests (Phase 9)       │
│    RenderBaselineTests                  DataTreeTests (existing)            │
│        ↓                                       ↓                            │
│  RenderBenchmarkTestsBase               (shared data factories)             │
│    (styles + data)                             ↓                            │
│        ↓                                       ↓                            │
│  RealDataTestsBase                             ↓                            │
│    (FwNewLangProjectModel)                     ↓                            │
└──────────────┬─────────────────────────────────┼────────────────────────────┘
               │                                 │
┌──────────────▼─────────────────────────────────▼────────────────────────────┐
│              RenderVerification (shared library) (Phase 8)                   │
│                                                                             │
│  Views-Only Path:              DataTree Path:                               │
│    RenderBenchmarkHarness        DataTreeRenderHarness                      │
│    ├── CreateScriptureView()     ├── DataTree + Inventory.Load()            │
│    ├── CreateLexEntryView()      ├── ShowObject() → CreateSlices()          │
│    └── CaptureViewBitmap()       └── CaptureCompositeBitmap()              │
│         └ VwDrawRootBuffered          ├ DrawToBitmap (WinForms chrome)      │
│                                       └ VwDrawRootBuffered per ViewSlice    │
│                                                                             │
│  Shared Infrastructure:                                                     │
│    RenderEnvironmentValidator    CompositeViewCapture                        │
│    RenderDiagnosticsToggle       RenderScenario + RenderViewType            │
│    RenderBenchmarkReportWriter   Verify InnerVerifier wrapper               │
└──────────────┬─────────────────────────────────┬────────────────────────────┘
               │                                 │
┌──────────────▼─────────────────────────────────▼────────────────────────────┐
│           Views Engine (C++ / COM)       DataTree / Slice (WinForms)        │
│                                                                             │
│  IVwRootBox → IVwRootSite           DataTree : UserControl                  │
│  StVc, LexEntryVc, XmlVc            ├── Slice (SplitContainer)             │
│  IVwStylesheet (LcmStyleSheet)       │    ├── SliceTreeNode (grey labels)   │
│  IVwDrawRootBuffered (HDC→Bitmap)    │    └── Control (editor panel)        │
│                                      ├── ViewSlice (Views RootSite)         │
│                                      ├── SummarySlice (sense summaries)     │
│                                      └── MultiStringSlice (multi-WS)       │
└─────────────────────────────────────────────────────────────────────────────┘
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
| 6 | DataTree composite capture misalignment | Medium | Medium | ViewSlice regions must precisely match DrawToBitmap coordinates; validate with edge-detection in tests |
| 7 | Production layout XML changes break DataTree snapshots | Medium | Low | Re-accept `.verified.png`; layout inventories loaded from `DistFiles/` track production state |
| 8 | DataTree initialization requires Mediator/PropertyTable | Low | Medium | Create minimal stubs for test context; existing `DataTreeTests` proves this pattern works |
| 9 | Shared RenderVerification library increases coupling | Low | Medium | Library is infrastructure only (no tests); consumers decide which capabilities to use |

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
| 2026-02-05 | Use base `Verify` package, not `Verify.NUnit` | All Verify.NUnit versions require NUnit ≥ 4.x; FieldWorks pins NUnit 3.13.3. Use `InnerVerifier` from base `Verify` package directly |
| 2026-02-05 | Wrap `SetupScenarioData()` in UoW in timing suite | `RenderTimingSuiteTests.RunBenchmark()` was calling `SetupScenarioData()` outside `UndoableUnitOfWorkHelper`, causing `InvalidOperationException` — all 5 scenario test cases now pass |
| 2026-02-05 | Add 5 new stress scenarios | many-paragraphs (50 sections), footnote-heavy (footnotes on every other verse), mixed-styles (unique formatting per verse), long-prose (80 verses per paragraph), multi-book (3 books) |
| 2026-02-05 | Add RTL and multi-WS scenarios | Arabic (RTL) and trilingual (English+Arabic+French) data factories for bidirectional and multilingual rendering coverage |
| 2026-02-05 | Expand Verify to all 12 scenarios | Parameterized `VerifyScenario(scenarioId)` with UoW-wrapped data setup; 12 `.verified.png` baselines accepted |
| 2026-02-05 | Delete RenderBitmapComparer | Hand-rolled pixel diff replaced by Verify snapshots; timing suite uses content-density sanity check instead |
| 2026-02-05 | Clear TestData/RenderSnapshots | Old baseline PNGs deleted; Verify `.verified.png` files live alongside test class |
| 2026-02-06 | Custom LexEntryVc over XmlVc | RootSiteTests can't reference XMLViews (massive dependency chain). Custom `LexEntryVc : VwBaseVc` exercises the same recursive nested-field Views pattern with `SimulateIfDataDoubleRender` flag for modeling ifdata overhead |
| 2026-02-06 | Add lex-shallow/deep/extreme scenarios | Three nesting depths (2/4/6 levels) to quantify O(N·2^d) rendering overhead from ifdata double-render; primary target for speedup measurement |
| 2026-02-06 | Plan shared RenderVerification library | Current harness only captures Views engine text, not WinForms chrome (grey labels, icons, section headers). Full DataTree/Slice rendering requires DetailControls dependency chain. Extract harness into shared library reusable across test projects — long-term goal: verification coverage for significant parts of the view architecture |
| 2026-02-06 | Composite bitmap capture for DataTree | `DrawToBitmap` works for WinForms controls (labels, splitters, backgrounds) but not for Views engine content in ViewSlice. Solution: multi-pass capture (DrawToBitmap for chrome + VwDrawRootBuffered per ViewSlice region) |
