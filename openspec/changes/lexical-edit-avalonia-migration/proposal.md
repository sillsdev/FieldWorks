## Why

Lexical Edit is the main editing surface in FLEx, but its current WinForms/DataTree/XMLViews architecture mixes view definition, control creation, LCModel access, refresh state, and legacy rendering concerns in ways that make the Avalonia migration risky. The existing Advanced Entry Speckit work proves useful pieces of an Avalonia path, but the broader migration needs an OpenSpec plan that treats XML Parts/Layout as a transitional compatibility contract and makes testability/refactoring the first-class work before replacing UI.

## What Changes

- Migrate the Advanced Entry Speckit research, parity checklist, and task intent into OpenSpec under a broader Lexical Edit migration change.
- Establish a phased migration contract: baseline tests first, legacy refactoring seams second, then Avalonia simple controls/popups, table views, slices, and full Lexical Edit views.
- Introduce a typed, managed view-definition/Presentation IR as the migration boundary. Existing XML Parts/Layout remains an import source during transition; long-term runtime XML dependency is retired only after parity is proven.
- Make native viewing/rendering decommissioning a completion gate for each migrated region: if native code owns display, layout, measurement, hit testing, selection, or editor realization, it SHALL NOT be brought into the completed Avalonia region. Custom linguistics engines and native services such as XAmple, spelling, parser/conversion tools, or similar language-documentation capability may remain when isolated behind service seams outside the Avalonia render/editor path.
- Start Graphite decommissioning with the migration. Graphite SHALL NOT be supported in Avalonia, and Avalonia SHALL NOT become the default Lexical Edit screen until Graphite font settings, native Graphite engines, Gecko Graphite rendering, PDF/export assumptions, tests, docs, and build/package artifacts are retired or converted to OpenType/HarfBuzz-only behavior.
- Require dependency-injected services around DataTree/Slice/Launcher behavior, view-definition compilation, editor selection, LCModel access, refresh coordination, diagnostics, and render/parity capture.
- Extend render verification from pixel/timing snapshots to semantic parity snapshots covering legacy WinForms/DataTree, typed IR, and Avalonia output.
- Define automation strategy: UIA2/FlaUI-style tests for legacy WinForms workflow reachability; Avalonia.Headless tests for new controls; layered unit/integration tests for IR, LCModel, refresh, and transactions.
- Allow Avalonia package updates or targeted upstream/local control work when stock controls cannot preserve FieldWorks density, interaction semantics, OpenType/HarfBuzz text, or TreeView requirements.

## Non-goals

- Replacing the full Lexical Edit UI in one change.
- Removing XML Parts/Layout before the typed IR and migration tooling can prove parity for user overrides, custom fields, ghost items, choosers, and nested sequences.
- Replacing LCModel or changing stored lexicon data schemas.
- Treating pixel-perfect WinForms output as the target. The target is near-pixel parity with equivalent information density, font/script behavior, interaction semantics, and accessibility.
- Deleting the global native Views engine before all non-migrated consumers are accounted for. Native rendering may remain as a legacy baseline or for other regions during transition, but it is not acceptable in completed Avalonia Lexical Edit regions.

## Capabilities

### New Capabilities

- `lexical-edit-avalonia-migration`: End-to-end phased migration requirements for Lexical Edit from WinForms/DataTree/XMLViews toward Avalonia.
- `lexical-edit-view-definition`: Typed view-definition and Presentation IR requirements, including XML import during transition and XML retirement gates.
- `lexical-edit-parity-automation`: Test, UI automation, render verification, and semantic parity requirements for WinForms and Avalonia migration safety.
- `lexical-edit-font-decommissioning`: Graphite decommissioning, OpenType/HarfBuzz font-option migration, Gecko/browser/PDF impact, and native dependency classification requirements.

### Modified Capabilities

- `architecture/ui-framework/views-rendering`: Add semantic parity capture and Avalonia comparison requirements to existing render baseline guidance.
- `architecture/ui-framework/winforms-patterns`: Add DetailControls/DataTree refactoring and UIA2 baseline expectations for legacy WinForms surfaces.
- `architecture/interop/native-boundary`: Add the requirement that migrated Avalonia regions eliminate runtime managed-to-native render interop.
- `architecture/testing/test-strategy`: Add layered UI migration testing expectations using unit/integration tests, UIA2 for WinForms, and Avalonia.Headless for Avalonia.

## Impact

- Managed code: `Src/Common/Controls/DetailControls/`, `Src/Common/Controls/XMLViews/`, `Src/xWorks/`, `Src/LexText/`, `Src/LexText/AdvancedEntry.Avalonia/`, `Src/Common/FwAvalonia/`, `Src/Common/RenderVerification/`, and related managed test projects.
- Native code: no native viewing/rendering path is planned for completed Avalonia regions. Existing Views/native rendering remains in baseline and comparison scope until replaced, but the dependency audit for each migrated region must prove there is no runtime call path through native display, layout, measurement, hit testing, selection, or editor-realization code before that region is considered complete. Native custom linguistics services that support FieldWorks' language-documentation mission, such as XAmple, spelling, parser/conversion tools, ICU, or Encoding Converters, may remain as explicit service dependencies when kept outside the Avalonia render/editor boundary. Graphite native code (`Lib/src/graphite2`, `GraphiteEngine`, and render-engine selection) is explicitly in decommissioning scope.
- Browser/export code: Gecko/XULRunner initialization currently enables Graphite rendering and `XWebBrowser`/`GeckofxHtmlToPdf` support preview, print, and PDF flows. Those paths must be audited, replaced, or moved outside the default Avalonia Lexical Edit boundary before default switch.
- Configuration: `DistFiles/Language Explorer/Configuration/Parts/*.fwlayout` and `*Parts.xml` become migration inputs to a managed typed view definition rather than the long-term runtime UI format.
- Dependencies: Avalonia/Avalonia.Headless packages may be updated in sync; owned FieldWorks controls should be preferred over hard-forking third-party controls unless a narrow upstream/local patch is justified.
