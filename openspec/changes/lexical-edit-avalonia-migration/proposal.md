## Why

Lexical Edit is the main editing surface in FLEx, but its current WinForms/DataTree/XMLViews architecture mixes view definition, control creation, LCModel access, refresh state, and legacy rendering concerns in ways that make the Avalonia migration risky. The existing Advanced Entry Speckit work proves useful pieces of an Avalonia path, but the broader migration needs an OpenSpec plan that treats XML Parts/Layout as a transitional compatibility contract and makes testability/refactoring the first-class work before replacing UI.

## What Changes

- Migrate the Advanced Entry Speckit research, parity checklist, and task intent into OpenSpec under a broader Lexical Edit migration change.
- Establish a phased migration contract: baseline tests first, legacy refactoring seams second, then Avalonia simple controls/popups, table views, slices, and full Lexical Edit views.
- Introduce a typed, managed view-definition/Presentation IR as the migration boundary. Existing XML Parts/Layout remains an import source during transition; long-term runtime XML dependency is retired only after parity is proven.
- Require dependency-injected services around DataTree/Slice/Launcher behavior, view-definition compilation, editor selection, LCModel access, refresh coordination, diagnostics, and render/parity capture.
- Extend render verification from pixel/timing snapshots to semantic parity snapshots covering legacy WinForms/DataTree, typed IR, and Avalonia output.
- Define automation strategy: UIA2/FlaUI-style tests for legacy WinForms workflow reachability; Avalonia.Headless tests for new controls; layered unit/integration tests for IR, LCModel, refresh, and transactions.
- Allow Avalonia package updates or targeted upstream/local control work when stock controls cannot preserve FieldWorks density, interaction semantics, OpenType/HarfBuzz text, or TreeView requirements.

## Non-goals

- Replacing the full Lexical Edit UI in one change.
- Removing XML Parts/Layout before the typed IR and migration tooling can prove parity for user overrides, custom fields, ghost items, choosers, and nested sequences.
- Replacing LCModel or changing stored lexicon data schemas.
- Treating pixel-perfect WinForms output as the target. The target is near-pixel parity with equivalent information density, font/script behavior, interaction semantics, and accessibility.
- Adding new native C++ UI dependencies. Native rendering may remain covered by baselines, but new UI architecture is managed/Avalonia-first.

## Capabilities

### New Capabilities

- `lexical-edit-avalonia-migration`: End-to-end phased migration requirements for Lexical Edit from WinForms/DataTree/XMLViews toward Avalonia.
- `lexical-edit-view-definition`: Typed view-definition and Presentation IR requirements, including XML import during transition and XML retirement gates.
- `lexical-edit-parity-automation`: Test, UI automation, render verification, and semantic parity requirements for WinForms and Avalonia migration safety.

### Modified Capabilities

- `architecture/ui-framework/views-rendering`: Add semantic parity capture and Avalonia comparison requirements to existing render baseline guidance.
- `architecture/ui-framework/winforms-patterns`: Add DetailControls/DataTree refactoring and UIA2 baseline expectations for legacy WinForms surfaces.
- `architecture/testing/test-strategy`: Add layered UI migration testing expectations using unit/integration tests, UIA2 for WinForms, and Avalonia.Headless for Avalonia.

## Impact

- Managed code: `Src/Common/Controls/DetailControls/`, `Src/Common/Controls/XMLViews/`, `Src/xWorks/`, `Src/LexText/`, `Src/LexText/AdvancedEntry.Avalonia/`, `Src/Common/FwAvalonia/`, `Src/Common/RenderVerification/`, and related managed test projects.
- Native code: no new native UI surface is planned, but existing Views/native rendering remains in baseline and comparison scope until replaced.
- Configuration: `DistFiles/Language Explorer/Configuration/Parts/*.fwlayout` and `*Parts.xml` become migration inputs to a managed typed view definition rather than the long-term runtime UI format.
- Dependencies: Avalonia/Avalonia.Headless packages may be updated in sync; owned FieldWorks controls should be preferred over hard-forking third-party controls unless a narrow upstream/local patch is justified.