# FieldWorks Verify.WinForms Integration Plan

## Executive Summary
**Feasibility: High, with setup complexity.**
FieldWorks uses a hybrid architecture where the C++ complex rendering engine (`Views.dll`) paints onto a managed .NET `UserControl` (`SimpleRootSite`) via `OnPaint` and GDI HDCs. This architecture is compatible with `Verify.WinForms` (likely needing a custom converter), making it a suitable tool. The primary challenge is constructing the necessary `IVwRootBox` and data dependencies (`ISilDataAccess`) within a test harness, which requires moving from "Mock" data to "Real" file-based data test contexts.

---

## 10-Point Comparison & Analysis

### 1. Rendering Architecture Compatibility
*   **Verify.WinForms**: Captures screenshots using `Control.DrawToBitmap`. However, standard `DrawToBitmap` often fails (returns black/empty) for controls that rely on complex native `OnPaint` interop like FieldWorks.
*   **FieldWorks**: `SimpleRootSite.cs` overrides `OnPaint`, obtains a GDI Handle (`HDC`), and passes it to the C++ layer (`m_rootb.DrawRoot`).
*   **Resolution**: We will likely need a **Custom Converter** that utilizes `IVwDrawRootBuffered` (or similar off-screen buffer interfaces exposed by the Views engine) to reliably extract pixels, rather than relying on GDI/WinForms `DrawToBitmap`.

### 2. Handling Complex Scripts & Text Rendering
*   **Verify.WinForms**: Pixel-perfect output capture.
*   **Benefit**: Ideal for "crown jewel" logic (Graphite, Cairo, Pango). Snapshot testing is superior to assertion-based testing for text layout engines because it catches subtle kerning, ligature, or line-breaking regressions that are impossible to assert programmatically.

### 3. Test Harness Complexity (The "Mocking" Problem)
*   **Issue**: The `Views` engine requires a heavy initialization context (`FwKernel`, `ISilDataAccess`, `IVwRootBox`). Using `ScrInMemoryLcmTestBase` (Mock LCM) proved insufficient because it lacks the deep MetaData required by `StVc`.
*   **Plan**: Switch to **Real Data** context. Use `FwNewLangProjectModel` (as seen in `FwCoreDlgsTests`) to spin up temporary, real XML-based projects. This guarantees a valid schema and avoids crashes in the View Constructors.

### 4. Handling Unmanaged Resources (GDI Leaks)
*   **Risk**: `SimpleRootSite` manages HDCs manually.
*   **Monitor**: Automated snapshot tests run many times. We must ensure `ReleaseHdc` is called scrupulously in the test harness to prevent GDI handle exhaustion.

### 5. Cross-Platform / OS Rendering Differences
*   **Risk**: Font rendering (ClearType) varies by OS.
*   **Mitigation**: Use `Verify.Phash` (perceptual hashing) or configure rigid rendering settings if running across disparate CI agents. Initially, we will target local developer verification.

### 6. Tooling Integration
*   **Stack**: NUnit + Verify.
*   **Verdict**: Seamless. FieldWorks already deeply invests in NUnit.

### 7. Performance
*   **Expectation**: 100ms-500ms per test due to View Engine spin-up.
*   **Strategy**: These are Integration Tests, not Unit Tests. We will group them continuously.

### 8. Failure Diagnostics
*   **Benefit**: "Diff" images showing exact pixel regressions (e.g., "The glyph for 'A' moved 2 pixels right") instead of generic crashes.

### 9. Custom Control Support
*   **FieldWorks**: `SimpleRootSite` is a `UserControl`, which `Verify` supports, but our specific Native painting requires the custom handling mentioned in Point 1.

### 10. Fallback Strategy
*   **Safety**: If `DrawToBitmap` is flaky, we have access to `IVwGraphics` internally. We can programmatically extract a bitmap from the engine, bypassing the WinForms layer entirely if needed.

---

## Implementation Plan

### Phase 1: Stabilization (Immediate)
1.  **Revert Hacks**: Remove the unstable `try-catch` blocks from `StVc.cs` and `GenericScriptureView.cs`.
2.  **Clean Slate**: Ensure `RenderBenchmarkTests` is failing cleanly with the expected `LcmInvalidFieldException` (confirming the need for Real Data).

### Phase 2: Test Harness "Real Data"
1.  **Create `RealDataTestsBase`**:
    -   Location: `Src/Common/RootSite/RootSiteTests/RealDataTestsBase.cs`
    -   Logic: Adapt initialization code from `FwCoreDlgsTests` using `FwNewLangProjectModel` and `LcmFileCache`.
    -   Dependencies: Ensure `FwCoreDlgs` utilities are accessible or duplicated safely.
2.  **Refactor Benchmark**: Update `RenderBenchmarkTestsBase` to inherit from `RealDataTestsBase`.

### Phase 3: Verify Integration
1.  **Dependencies**: Install `Verify.WinForms` (Target **v19.14.1** for .NET Framework 4.8 compatibility).
2.  **Infrastructure**: Configure `Verify` standard settings (clipboard, diff tool).
3.  **Custom Converter**: Implement a `Verify` converter for `SimpleRootSite` that uses `IVwDrawRootBuffered` to create the `Bitmap`.

### Phase 4: Pilot Test
1.  **Test Case**: `GenerateBaselineSnapshot_Simple`.
2.  **Action**: Run test. It should pass initialization (Phase 2), render the view, and produce a `*.verified.png` (Phase 3).
