## Why

Lexical Edit is the main editing surface in FLEx, but its current WinForms/DataTree/XMLViews architecture mixes view definition, control creation, LCModel access, refresh state, and legacy rendering concerns in ways that make the Avalonia migration risky. The existing Advanced Entry Speckit work proves useful pieces of an Avalonia path, but the broader migration needs an OpenSpec plan that treats XML Parts/Layout as a transitional compatibility contract and makes testability/refactoring the first-class work before replacing UI.

Current branch scope is judged by the branch-only diff against `main`, not by same-day commit timestamps. This Phase 1/2 branch now contains OpenSpec planning, migration-review skills, legacy WinForms/DataTree/XMLViews characterization coverage, the net48 `FwAvalonia` spike, a net48 `FwAvaloniaPreviewHost` + preview-host UIA harness, typed view-definition and seam foundation code, and product-facing app-wide lexical-edit UI mode wiring through existing `RecordEditView` hosts. The older net8 `AdvancedEntry.Avalonia` prototype remains on `010-advanced-entry-preview-prototype` as a separate prototype track.

The detached spike-only `Poc*` runtime stack has now been retired in favor of the shared
region-model preview path documented in `poc-retiring.md`; the old
`lexical-edit-avalonia-poc-spike` change is folded forward and removed.

## What Changes

- Migrate the Advanced Entry Speckit research, parity checklist, and task intent into OpenSpec under a broader Lexical Edit migration change.
- Establish a phased migration contract: baseline tests first, legacy refactoring seams second, then Avalonia simple controls/popups, table views, slices, and full Lexical Edit views.
- Introduce a typed, managed view-definition/Presentation IR as the migration boundary. Existing XML Parts/Layout remains an import source during transition; long-term runtime XML dependency is retired only after parity is proven.
- Treat lexical-edit UI mode as an app-wide product switch while keeping host behavior explicit per consumer: every current `RecordEditView` consumer must declare whether Avalonia mode is supported, falls back to legacy, or is blocked with a deliberate product-facing diagnostic.
- Make native viewing/rendering decommissioning a completion gate for each migrated region: if native code owns display, layout, measurement, hit testing, selection, or editor realization, it SHALL NOT be brought into the completed Avalonia region. Custom linguistics engines and native services such as XAmple, spelling, parser/conversion tools, or similar language-documentation capability may remain when isolated behind service seams outside the Avalonia render/editor path.
- Graphite policy (revised 2026-06-09, owned by the `graphite-transition-support` change): Graphite remains supported on legacy surfaces until the M2 mid-decommissioning milestone and sunsets with tooling, while Avalonia surfaces render Graphite-affected writing systems with OpenType shaping plus a graded per-writing-system warning — never a hard block. This change retains only the engine-isolation audit (no native Graphite/Views shaping on the Avalonia path) and the Gecko/browser/PDF classification.
- Require dependency-injected services around DataTree/Slice/Launcher behavior, view-definition source/import/compile/cache, editor selection, edit sessions, LCModel transactions, undo/redo grouping, validation, command/focus routing, UI dispatch, lifetime/disposal, diagnostics, and render/parity capture.
- Freeze seam-specific recommendations in dedicated capability specs for edit sessions, undo/redo, validation, command/focus, UI scheduling, and lifetime so phase-one lexical work and phase-two shell work consume the same decisions.
- Define migrated-region manifests and hard gates so each claimed Avalonia region has explicit entry points, allowed legacy adapters, forbidden native/Graphite call paths, custom linguistics service dependencies, parity fixtures, performance budgets, and rollback/default-switch rules.
- Extend render verification from pixel/timing snapshots to semantic parity snapshots covering legacy WinForms/DataTree, typed IR, and Avalonia output.
- Define automation strategy: UIA2/FlaUI-style tests for legacy WinForms workflow reachability; Avalonia.Headless tests for new controls; layered unit/integration tests for IR, LCModel, refresh, and transactions.
- Keep Avalonia build and test participation on the normal repo scripts (`./build.ps1`, `./test.ps1`). The legacy-vs-Avalonia choice is a runtime product behavior switch, not a separate build lane.
- Allow Avalonia package updates or targeted upstream/local control work when stock controls cannot preserve FieldWorks density, interaction semantics, OpenType/HarfBuzz text, or TreeView requirements.
- Retire the detached POC renderer/data path once the shared region renderer can serve both
	product and preview, so preview stays lightweight without carrying a duplicate UI stack.

## Non-goals

- Replacing the full Lexical Edit UI in one change.
- Removing XML Parts/Layout before the typed IR and migration tooling can prove parity for user overrides, custom fields, ghost items, choosers, and nested sequences.
- Replacing LCModel or changing stored lexicon data schemas.
- Treating pixel-perfect WinForms output as the target. The target is near-pixel parity with equivalent information density, font/script behavior, interaction semantics, and accessibility.
- Deleting the global native Views engine before all non-migrated consumers are accounted for. Native rendering may remain as a legacy baseline or for other regions during transition, but it is not acceptable in completed Avalonia Lexical Edit regions.

## Capabilities

### New Capabilities

- `lexical-edit-avalonia-migration`: End-to-end phased migration requirements for Lexical Edit from WinForms/DataTree/XMLViews toward Avalonia.
- `lexical-edit-view-definition`: Typed view-definition and Presentation IR requirements, including XML import during transition, dynamic editor diagnostics, stable identity, virtualization/focus metadata, and XML retirement gates.
- `lexical-edit-parity-automation`: Test, UI automation, render verification, and semantic parity requirements for WinForms and Avalonia migration safety.
- `lexical-edit-font-decommissioning`: engine-isolation audit for the Avalonia path, OpenType/HarfBuzz font-option migration where supported, Gecko/browser/PDF impact, and native dependency requirements. (Graphite *support policy* — legacy-surface support, Avalonia warnings, sunset milestones — moved to the dedicated `graphite-transition-support` change, 2026-06-09.)
- `avalonia-edit-sessions`: FieldWorks-owned edit-session and commit-boundary requirements for editable Avalonia regions, starting from the current direct LCModel fenced-session model.
- `avalonia-undo-redo`: Domain-authoritative undo/redo requirements with control-local leaf undo allowed only as a subordinate behavior.
- `avalonia-validation`: FieldWorks-owned validation seam requirements with Avalonia-native presentation and package-backed rule engines as subordinate options.
- `avalonia-command-focus`: Global command/focus bridge requirements for shell and popup behavior, while allowing screen-local Avalonia commands inside migrated regions.
- `avalonia-ui-scheduler`: Thin UI-thread scheduling seam requirements for non-view layers with direct Avalonia dispatcher use allowed only at the view and startup edge.
- `avalonia-lifetime`: Thin app/window/dialog lifetime seam requirements for non-view layers, with heavier region frameworks explicitly deferred.

### Modified Capabilities

- `architecture/ui-framework/views-rendering`: Add semantic parity capture and Avalonia comparison requirements to existing render baseline guidance.
- `architecture/ui-framework/winforms-patterns`: Add DetailControls/DataTree refactoring and UIA2 baseline expectations for legacy WinForms surfaces.
- `architecture/interop/native-boundary`: Add the requirement that migrated Avalonia regions eliminate runtime managed-to-native render interop.
- `architecture/testing/test-strategy`: Add layered UI migration testing expectations using unit/integration tests, UIA2 for WinForms, and Avalonia.Headless for Avalonia.

## Impact

- Managed code: `Src/Common/Controls/DetailControls/`, `Src/Common/Controls/XMLViews/`, `Src/xWorks/`, `Src/LexText/`, `Src/Common/FwAvalonia/` (the owning Avalonia project on this branch; `AdvancedEntry.Avalonia` is the prototype-branch reference only), `Src/Common/RenderVerification/`, and related managed test projects.
- Native code: no native viewing/rendering path is planned for completed Avalonia regions. Existing Views/native rendering remains in baseline and comparison scope until replaced, but the dependency audit for each migrated region must prove there is no runtime call path through native display, layout, measurement, hit testing, selection, or editor-realization code before that region is considered complete. Native custom linguistics services that support FieldWorks' language-documentation mission, such as XAmple, spelling, parser/conversion tools, ICU, or Encoding Converters, may remain as explicit service dependencies when kept outside the Avalonia render/editor boundary. Graphite native code and render-engine selection are explicitly in inventory/decommissioning scope for the migrated default path, while legacy fallback/export consumers are classified separately.
- Browser/export code: Gecko/XULRunner initialization currently enables Graphite rendering and `XWebBrowser`/`GeckofxHtmlToPdf` support preview, print, and PDF flows. Those paths must be audited, replaced, or moved outside the default Avalonia Lexical Edit boundary before default switch.
- Configuration: `DistFiles/Language Explorer/Configuration/Parts/*.fwlayout` and `*Parts.xml` become migration inputs to a managed typed view definition rather than the long-term runtime UI format.
- Dependencies: Avalonia/Avalonia.Headless packages may be updated in sync; owned FieldWorks controls should be preferred over hard-forking third-party controls unless a narrow upstream/local patch is justified.
