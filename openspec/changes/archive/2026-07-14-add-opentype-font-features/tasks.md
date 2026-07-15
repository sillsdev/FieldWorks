> Review-driven update (2026-05-11): Sections 1-11 record research, evidence,
> and earlier planning already captured for this change. They are not a
> merge-readiness signal. Sections 12-22 record the review-driven implementation
> backlog and the final status discovered during implementation.

## 1. Post-Speedup Preflight

- [x] 1.1 Rebase or merge the implementation branch after `001-render-speedup` is merged, then inspect render/cache changes in `Src/Common/SimpleRootSite/`, `Src/ManagedVwDrawRootBuffered/`, and `Src/views/`. [Managed C# + Native C++]
- [x] 1.2 Verify the render snapshot/baseline infrastructure from `001-render-speedup` is present and runnable before adding LT-22324 visual tests. [Managed C#]
- [x] 1.3 Confirm redistributable test fonts and licenses for OpenType feature scenarios; `CharisSIL-5.000` from the JIRA issue is committed under the native Views test data with its OFL license. [Planning/Test]
- [x] 1.4 Record selected fonts, feature tags, expected visual differences, and licensing notes in the FieldWorks test project assets or OpenSpec research notes. [Docs/Test]
- [x] 1.5 Use `views-migration-matrix.md` as the Views subsystem checklist when selecting Phase 1 visual, selection, cache, and future-migration baseline scenarios. [Planning/Test]

## 2. Renderer-Neutral Feature Model

- [x] 2.1 Add or identify a shared managed parser/normalizer for `tag=value` font feature strings, including duplicate, empty, invalid, and ordering behavior. File area: `Src/Common/FwUtils/` or existing font utility location. [Managed C#]
- [x] 2.2 Add unit tests for parser/normalizer behavior, including OpenType tags such as `smcp=1`, `kern=0`, and alternate values such as `cv01=2`. [Managed C#]
- [x] 2.3 Keep Graphite tag-to-ID conversion isolated at the Graphite boundary; do not reuse Graphite numeric conversion for OpenType. Files: `Src/Common/FwUtils/GraphiteFontFeatures.cs`, `Src/views/lib/GraphiteEngine.cpp`. [Managed C# + Native C++]
- [x] 2.4 Define a font-feature provider seam for UI discovery with Graphite and OpenType implementations. Files: `Src/FwCoreDlgs/FwCoreDlgControls/FontFeaturesButton.cs` and nearby controls. [Managed C#]

## 3. Native OpenType Rendering

- [x] 3.1 Audit current feature flow through `FwTextPropType.ktptFontVariations`, `VwPropertyStore`, `LgCharRenderProps`, and `UniscribeSegment` to confirm the per-run feature carrier. Files: `Src/views/VwPropertyStore.cpp`, `Src/views/lib/UniscribeSegment.cpp`. [Native C++]
- [x] 3.2 Add native parsing from the run feature string into OpenType feature records suitable for Uniscribe OpenType APIs. Files: `Src/views/lib/UniscribeEngine.cpp`, `Src/views/lib/UniscribeSegment.cpp/.h`. [Native C++]
- [x] 3.3 Replace or branch the no-feature `ScriptShape`/`ScriptPlace` flow with `ScriptItemizeOpenType`, `ScriptShapeOpenType`, and `ScriptPlaceOpenType` when OpenType features are present. File: `Src/views/lib/UniscribeSegment.cpp`. [Native C++]
- [x] 3.4 Preserve old Uniscribe behavior for empty feature strings and preserve Graphite rendering behavior for Graphite fonts. [Native C++]
- [x] 3.5 Add native tests for OpenType feature-on/off shaping, placement, metrics, line breaking, and selection-related placement. Files: `Src/views/Test/TestUniscribeEngine.h` or adjacent native tests. [Native C++]
- [x] 3.6 Run affected native tests after native implementation compiles: `./test.ps1 -SkipManaged -TestProject TestViews -StartedBy agent`. [Validation]

## 4. WinForms Font Feature UI

- [x] 4.1 Refactor `FontFeaturesButton` to use the provider seam and enable when the selected font has Graphite or OpenType configurable features. File: `Src/FwCoreDlgs/FwCoreDlgControls/FontFeaturesButton.cs`. [Managed C#]
- [x] 4.2 Decouple `DefaultFontsControl` feature availability from `m_ws.IsGraphiteEnabled`; keep `Enable Graphite` limited to Graphite renderer selection. File: `Src/FwCoreDlgs/FwCoreDlgControls/DefaultFontsControl.cs`. [Managed C# WinForms]
- [x] 4.3 Ensure `FwFontAttributes`, `FwFontTab`, and `FwFontDialog` load/save OpenType feature strings through existing `FontInfo.m_features` paths. Files: `Src/FwCoreDlgs/FwCoreDlgControls/FwFontAttributes.cs`, `FwFontTab.cs`, `Src/FwCoreDlgs/FwFontDialog.cs`. [Managed C# WinForms]
- [x] 4.4 Update `.resx` labels/help strings from Graphite-only wording to generic Font Features/Font Options wording. Files: `Src/FwCoreDlgs/FwCoreDlgControls/*.resx`. [Localization]
- [x] 4.5 Add UI/component tests for `FontFeaturesButton`, `DefaultFontsControl`, `FwFontAttributes`, `FwFontTab`, and `FwFontDialog` covering OpenType features without Graphite enabled. Test project: `Src/FwCoreDlgs/FwCoreDlgsTests/` or existing control test project. [Managed C# Tests]

## 5. Render Cache and Speedup Integration

- [x] 5.1 After `001-render-speedup` is merged, identify every cache/guard that can reuse shaped, laid-out, or captured output and document its feature-string identity requirement. [Managed C# + Native C++]
- [x] 5.2 Update renderer, layout, warm-render, and buffered-frame invalidation so feature changes dirty affected output. Files likely include `RenderEngineFactory.cs`, `SimpleRootSite.cs`, `VwRootBox.cpp`, and render verification infrastructure. [Managed C# + Native C++]
- [x] 5.3 Add tests proving toggling a feature does not reuse stale layout, glyph output, line breaks, or cached bitmap output. [Managed C# + Native C++ Tests]
- [x] 5.4 Verify same-font runs with different feature strings remain distinct in rendering and layout. [Tests]

## 6. Visual Rendering and Comparison Tests

- [x] 6.1 Add WinForms/Views render baseline scenarios for feature-off and feature-on output using writing-system default features. Use post-`001-render-speedup` render snapshot infrastructure. [Managed C# Tests]
- [x] 6.2 Add WinForms/Views render baseline scenarios for style-specific OpenType features, including Normal style for vernacular writing system. [Managed C# Tests]
- [x] 6.3 Add at least one bidi or multi-writing-system scenario to guard against complex-script regressions. [Managed C# Tests]
- [x] 6.4 Add test-only HarfBuzzSharp + SkiaSharp dependencies to a test/comparison project only; ensure production projects do not reference them. [Managed C# Tests]
- [x] 6.5 Add HarfBuzzSharp shaping-data comparisons for the same feature scenarios, comparing glyph IDs, clusters, advances, or offsets where deterministic. [Managed C# Tests]
- [x] 6.6 Add SkiaSharp visual comparison output with documented tolerance rules for future Avalonia migration evidence. [Managed C# Tests]
- [x] 6.7 Document deterministic font asset and baseline status: native Views end-to-end render tests use committed `CharisSIL-5.000`; HarfBuzz/Skia comparison tests still use installed-font probes for test-only comparison. [Managed C# Tests]

## 7. Exports, Help, and Documentation

- [x] 7.1 Verify existing CSS export emits OpenType feature strings correctly; extend `CssGeneratorTests` only if gaps remain. File: `Src/xWorks/CssGenerator.cs` and tests. [Managed C#]
- [x] 7.2 Audit Word/Notebook/export paths for feature-string omissions and file follow-up tasks for non-Phase-1 gaps. Files include `Src/xWorks/WordStylesGenerator.cs`, `NotebookExportDialog.cs`. [Managed C#]
- [x] 7.3 Update FieldWorks Help or context help for OpenType Font Features and the continued temporary role of Graphite. [Docs/Help]
- [x] 7.4 Update OpenSpec research/design notes if implementation discovers a different safe renderer path. [OpenSpec]

## 8. Validation

- [x] 8.1 Run affected managed UI/control tests through `./test.ps1` with the relevant test project filters. [Validation]
- [x] 8.2 Run affected render baseline tests and review received/verified images. [Validation]
- [x] 8.3 Run `./build.ps1` after native and managed changes are complete. [Validation]
- [x] 8.4 Run `CI: Full local check` before committing or pushing; commit-message lint still fails on pre-existing commit `c30c1e7d16`, and whitespace was checked separately with no problems. [Validation]
- [x] 8.5 Confirm OpenSpec status is complete and all tasks/spec requirements are still aligned before implementation PR review. [OpenSpec]

## 9. Manual WinApp Evidence

- [x] 9.1 Launch `Output/Debug/FieldWorks.exe` with WinApp MCP and confirm the Sena 3 project is loaded, using `Sena 3 2018-09-11 1145.fwbackup` only if restore is needed. [Manual Validation]
- [x] 9.2 Capture Writing System Properties > Font evidence showing `Font Options`, unchecked `Enable Graphite`, and enabled `Font Features`. [Manual Validation]
- [x] 9.3 Capture the Styles dialog > Font tab showing the shared `Font features` control. [Manual Validation]
- [x] 9.4 Record manual test steps, screenshots, and the before-state capture limitation in `manual-testing.md`. [OpenSpec]
- [x] 9.5 Fetch `LT-22324` through the refreshed Atlassian read-only skill and record exact JIRA font recommendations, comments, and attachment status. [Manual Validation]
- [x] 9.6 Inventory local and installer font availability for JIRA-recommended and FieldWorks-bundled fonts. [Manual Validation]
- [x] 9.7 Capture WinForms MCP UIA2 evidence showing `Charis SIL` selected and the OpenType Font Features menu visible. [Manual Validation]

## 10. Deterministic Font Fixture Rendering

- [x] 10.1 Add the JIRA-specified `CharisSIL-5.000` regular font, OFL license, and README under `Src/views/Test/TestData/Fonts/CharisSIL-5.000`. [Native C++ Tests]
- [x] 10.2 Update `TestViews.vcxproj` to copy the Charis SIL fixture beside `TestViews.exe` for native test execution. [Native C++ Tests]
- [x] 10.3 Add `TestUniscribeEngine` coverage that loads the Charis SIL fixture privately and verifies feature-off/feature-on OpenType rendering through `FindBreakPoint`, `ILgSegment::DrawText`, and bitmap pixel comparison. [Native C++ Tests]
- [x] 10.4 Add `RenderEngineFactoryTests` coverage proving writing-system default OpenType features are normalized into `LgCharRenderProps.szFontVar`, equivalent feature strings reuse the renderer cache entry, and different feature strings create separate renderer cache entries. [Managed C# Tests]
- [x] 10.5 Run `./test.ps1 -TestProject SimpleRootSiteTests -StartedBy agent`; result: `Total tests: 113`, `Passed: 113`. [Validation]
- [x] 10.6 Run `./test.ps1 -SkipManaged -TestProject TestViews -StartedBy agent`; result: `Tests [Ok-Fail-Error]: [295-0-0]`. [Validation]

## 11. Word DOCX Export Parity

- [x] 11.1 Add OpenSpec requirements, research analysis, implementation plan, and tasks for Microsoft Word DOCX OpenType feature subset export. [OpenSpec]
- [x] 11.2 Add failing managed tests proving style and explicit run font features emit documented WordprocessingML `w14` typography elements. [Managed C# Tests]
- [x] 11.3 Implement a Word DOCX feature mapper that reuses the renderer-neutral parser and maps supported tags to `w14` elements. [Managed C#]
- [x] 11.4 Keep unsupported tags non-fatal and document Word export subset behavior in `Docs/opentype-font-features.md`. [Docs]
- [x] 11.5 Run targeted xWorks Word generator tests. [Validation]

## 12. Review Baseline And Churn Reconciliation

- [x] 12.1 Confirm the final implementation branch resolves review-only churn before validation; remaining `.serena/project.yml` churn is unrelated local tooling state and generated native test collection changes come from the test build. [Validation/Process]
- [x] 12.2 Re-run targeted managed and native validation against one coherent final tree after the review-driven fixes are implemented. [Validation]

## 13. Filter OpenType Discovery To User-Configurable Features

- [x] 13.1 Define discovery rules that keep optional user-facing features and filter required shaping or engine-controlled features before the UI is populated. [Managed C# + Native C++]
- [x] 13.2 Document the Phase 1 discovery constraint: managed UI discovery reads layout-table feature records and filters required shaping features; script/language-aware managed enumeration remains a future enhancement if raw optional tags prove too noisy. [Managed C#]
- [x] 13.3 Add tests covering filtered-out required features and preserved optional features. [Tests]

## 14. Prefer OpenType With A Clear Explicit Toggle

- [x] 14.1 Make OpenType the default feature provider for fonts that expose both OpenType and Graphite feature sets. [Managed C# WinForms]
- [x] 14.2 Use the existing Graphite selection surface as the explicit provider switch while defaulting the shared button to OpenType; document remaining copy/help refinements separately from renderer behavior. [Managed C# WinForms]
- [x] 14.3 Thread provider choice through the shared `FontFeaturesButton` path used by default-font and style/font surfaces. [Managed C# WinForms]
- [x] 14.4 Add tests for OpenType-default behavior, provider filtering, inherited default font behavior, and font-feature round-tripping. [Tests]

## 15. Add Trace Logging

- [x] 15.1 Define trace switches/messages using the existing FieldWorks diagnostics infrastructure rather than modal assertions. [Managed C# + Native C++]
- [x] 15.2 Log malformed tags and strings, filtered discovery decisions, provider selection decisions, native shaping failures, retry attempts, and fallback reasons. [Managed C# + Native C++]
- [x] 15.3 Add tests or harness checks where practical to verify key diagnostics points without overcoupling to exact message text. [Tests]

## 16. Fix Truncation Logic Risk

- [x] 16.1 Replace comma-only truncation loops in `VwPropertyStore.cpp` with fail-safe logic that handles overlong strings with no comma boundary and always makes forward progress or aborts safely. [Native C++]
- [x] 16.2 Preserve compatible behavior for valid legacy multi-tag strings while rejecting or truncating malformed no-progress cases safely. [Native C++]
- [x] 16.3 Add tests for overlong strings with commas, overlong strings without commas, exact-boundary strings, and inherited overlong strings. [Tests]

## 17. Validate Accepted Tag Names And Trace Malformed Tags

- [x] 17.1 Align tag validation with published OpenType/CSS syntax: accept any four-character printable ASCII tag and do not restrict support to registered tags only. [Managed C#]
- [x] 17.2 Ignore malformed tags safely, emit trace diagnostics, and continue processing remaining valid entries in the same feature string. [Managed C#]
- [x] 17.3 Keep storage and rendering acceptance liberal for valid custom/private tags while making CSS/DOCX output boundaries safe. [Managed C#]
- [x] 17.4 Add parser/normalizer tests for valid registered tags, valid custom tags, malformed tags, duplicate tags, and mixed valid/invalid input. [Tests]

## 18. Follow The Existing Inheritance Path

- [x] 18.1 Attempt removal of the parallel `StyleInfo` loader; validation showed it is still required in the active build graph, so retain it as a minimal compatibility adapter and document the boundary. [Managed C#]
- [x] 18.2 Audit related call sites so writing-system defaults, style overrides, and dialog reloads still round-trip through `FontInfo.m_features` / `ktptFontVariations`. [Managed C#]
- [x] 18.3 Add and run tests for inherited default features, explicit overrides, and reopen/save cycles with the compatibility adapter in place. [Tests]

## 19. Update OpenSpec Scope And Review Artifacts

- [x] 19.1 Capture clarification, analysis, and planning passes in OpenSpec artifacts and add a dedicated in-depth review note. [OpenSpec]
- [x] 19.2 Expand proposal, design, research, capability specs, and tasks to include review-driven scope: filtering, OpenType preference, logging, truncation safety, tag validation, inheritance cleanup, and additional state-of-the-art follow-ups. [OpenSpec]

## 20. Add Recommended Tests

- [x] 20.1 Add managed UI tests for required-feature filtering, OpenType-preferred behavior, and inherited/default font-feature round-tripping. [Managed C# Tests]
- [x] 20.2 Add native tests for malformed/overlong feature strings, retryable OpenType shaping behavior, script/language handling, and safe traced fallback. [Native C++ Tests]
- [x] 20.3 Add export tests for CSS-safe serialization and DOCX supported-subset mapping; audit Notebook default-font-features preservation through existing XML emission. [Managed C# Tests]
- [x] 20.4 Add cache-identity tests ensuring feature changes invalidate render/layout caches without stale reuse. [Managed + Native Tests]
- [x] 20.5 Add manual/diagnostic checklist coverage for provider behavior and trace collection steps. [OpenSpec/Manual Validation]

## 21. Apply Review-Driven Architecture Changes

- [x] 21.1 Introduce an explicit font-feature provider abstraction that separates discovery, selection UI, and renderer choice. [Managed C#]
- [x] 21.2 Keep renderer-neutral `tag=value` strings as the authoritative contract and convert only at renderer/export boundaries. [Managed C# + Native C++]
- [x] 21.3 Make provider choice explicit in UI state instead of implicit in `Enable Graphite`, and document the OpenType-default rule for dual-technology fonts. [Managed C# WinForms]
- [x] 21.4 Keep native Uniscribe changes additive and reg-free-COM-safe while improving diagnostics and robustness. [Native C++]

## 22. Add Additional State-Of-The-Art Follow-Ups

- [x] 22.1 Retry `ScriptShapeOpenType` / `ScriptPlaceOpenType` on `E_OUTOFMEMORY` before abandoning OpenType shaping. [Native C++]
- [x] 22.2 Keep script tags authoritative from `ScriptItemizeOpenType`, use available font/locale language candidates, and trace fallback decisions. [Native C++]
- [x] 22.3 Ensure CSS export safely serializes any valid accepted tag, including characters that require escaping in CSS strings. [Managed C#]
- [x] 22.4 Keep Word DOCX export on the documented `w14` subset and document unsupported tags explicitly rather than inventing hidden storage. [Managed C# + Docs]
- [x] 22.5 Audit Notebook export preservation of default font features and close review-documented evidence gaps in OpenSpec notes. [Managed C# Tests + OpenSpec]
