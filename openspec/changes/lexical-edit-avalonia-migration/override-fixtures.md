# Customer and User Override XML Fixtures

This document defines the fixture plan for preserving FieldWorks project/user layout overrides while migrating Lexical Edit toward typed view definitions and Avalonia controls. It is a test plan, not just an inventory.

## 1. Override Sources to Preserve

| Source | Examples / Paths | Why It Matters |
|---|---|---|
| Shipped detail layouts | `DistFiles/Language Explorer/Configuration/Parts/*.fwlayout`, `*Parts.xml` | Baseline LexEntry/LexSense/Morphology behavior, labels, visibility, ghost slices, and custom-field placeholders. |
| Project dictionary configs | `<Project>/Configuration/Dictionary/*.xml` | User-edited dictionary publication layouts, columns, before/between/after text, writing-system options, list options, and shared nodes. |
| Project reversal configs | `<Project>/Configuration/ReversalIndex/*.xml` | Reversal-specific labels, headword/gloss options, writing-system choices, and migrated historical config variants. |
| Historical configs | `PreHistoricMigrator`, `FirstAlphaMigrator`, `FirstBetaMigrator`, and related xWorks tests | Old projects must migrate into the typed-definition path without losing custom choices. |
| CSS overrides | `ProjectDictionaryOverrides.css`, `ProjectReversalOverrides.css` | Legacy preview/export styling. The migrated edit surface must explicitly classify this as legacy-only, translated, or unsupported with diagnostics. |
| Writing-system/font metadata | LDML writing-system store, `IsGraphiteEnabled`, `DefaultFontFeatures` | Layout and rendering behavior can change when writing-system and font metadata are ignored. |

## 2. Concrete Fixture Families

Each family must have input files, expected typed IR or diagnostics, and mismatch artifacts.

| Fixture Family | Minimum Cases | Expected Assertions |
|---|---|---|
| Shipped `LexEntry-detail-Normal` | Top-level identity fields, lexeme form, citation form, pronunciations, senses | Stable node IDs, class/flid binding, editor kind, visibility, focus order, accessibility ID/name. |
| Nested senses and ghost entries | Senses with subsenses, examples, ghost labels/init methods | Ghost metadata and lazy item templates survive compilation without recursive expansion loops. |
| Custom fields | Entry, sense, example, allomorph, pronunciation, etymology, entry-ref, extended-note, translation, and picture custom scalar/multistring/GenDate/possibility-list fields | Custom field placeholders resolve deterministically on every shipped detail-surface insertion point and retain flid/type/writing-system metadata. |
| Dictionary configuration migration | Current, pre-8.3, alpha, beta-style dictionary configs from xWorks tests | Migrator output remains schema-valid and typed-definition import preserves order and labels. |
| Reversal configuration migration | Reversal language variants, subentries, missing or invalid reversal writing systems | Reversal-specific labels and writing-system options are preserved or diagnosed. |
| Duplicate/shared nodes | Shared senses, referenced complex forms, duplicate custom nodes | Stable node IDs remain unique; referenced nodes do not duplicate children accidentally. |
| CSS/browser styling | Dictionary and reversal CSS override samples | Classified as legacy preview/export, converted to Avalonia resources, or reported as unsupported with a diagnostic. |

## 3. Compiler and Merger Strategy

The typed-definition compiler must consume an immutable snapshot of fully merged configuration data. It must not read directly from WinForms controls, live `PropertyTable` mutation state, or mutable `LcmCache` objects on background threads.

Required pipeline:

1. Load shipped parts/layouts.
2. Apply project/user override precedence using the same semantics as `LayoutCache`/`Inventory` and dictionary migrators.
3. Convert the merged XML to typed view definitions / Presentation IR.
4. Normalize stable comparison keys: node ID, class/flid/object binding, editor kind, writing-system metadata, visibility, ghost metadata, focus order, and accessibility identity.
5. Emit unsupported-construct diagnostics with source file, layout ID, part ref, XML path, and suggested owner.

## 4. Test and Artifact Requirements

| Test Type | Required Evidence |
|---|---|
| Snapshot tests | Expected JSON committed for selected fixtures; actual JSON attached on mismatch. |
| Migration tests | Existing xWorks migrator tests remain green; selected fixtures run through the AdvancedEntry typed-definition compiler. |
| Override precedence tests | Same layout/part ID in shipped and project config proves project override wins. |
| Unsupported construct tests | Dynamic/custom editor constructs produce deterministic diagnostics instead of silent drops. |
| Localization tests | User-visible labels come from layout/resource metadata and remain localizable; no new hardcoded production strings. |
| Writing-system tests | Writing-system options, font metadata, direction/culture, and missing WS cases are captured or diagnosed. |

## 5. Phasing

| Phase | Goal | Exit Criteria |
|---|---|---|
| Phase 1 inventory | Select fixture families and map existing migrator/layout tests. | Every fixture has source path, owner, behavior being protected, and known current test coverage. |
| Phase 2 characterization | Add semantic baselines before refactor. | DataTree/Slice and Avalonia IR baselines pass; unsupported gaps are documented. |
| Phase 4 typed import | Implement merged XML to typed-definition compiler. | Fixtures compile deterministically or emit approved diagnostics; cache invalidation/cancellation tests pass. |
| Phase 9 retirement | Disable runtime XML for a gated migrated surface. | Import/audit fallback exists, customer override fixtures pass, rollback switch remains available. |

## 6. Non-Goals for First Slice

- Do not translate arbitrary user CSS into full Avalonia styling until a real styling compiler is designed.
- Do not delete XML layouts while legacy DataTree/XMLViews still use them.
- Do not claim UIA2 parity without a real UI automation harness.
- Do not treat Graphite-only font settings as harmless; preserve or diagnose them until the rendering policy is proven.