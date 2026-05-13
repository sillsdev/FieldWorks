# Tasks

## 1. Migration Baseline and Spec Audit

- [ ] 1.1 Review Speckit artifacts against this OpenSpec change and keep `migration-map.md` current.
- [ ] 1.2 Inventory current Lexical Edit view entry points: `RecordEditView`, `DataTree`, `SliceFactory`, XMLViews browse/table views, popup choosers, and AdvancedEntry Avalonia spike.
- [ ] 1.3 Build a coverage map for DataTree refresh, SliceFactory/editor selection, launchers, popup choosers, XML table views, and render verification.
- [ ] 1.4 Identify customer/user override XML fixtures that must be included before XML retirement.

## 2. Test Coverage Before Refactor

- [ ] 2.1 Add or extend unit/integration tests for DataTree refresh state transitions and postponed `PropChanged` behavior.
- [ ] 2.2 Add or extend launcher pure-logic tests, prioritizing morph type swap/data-loss logic and chooser decision paths.
- [ ] 2.3 Add semantic baseline capture for DataTree/Slice output: sections, labels, object/flid bindings, editor kind, visibility, ghost state, expansion, focus order, and accessibility identity.
- [ ] 2.4 Add focused UIA2 smoke baselines for WinForms launcher/chooser workflows and XMLViews table header/filter reachability.
- [ ] 2.5 Add failure artifact bundling to render/parity tests where missing.

## 3. Refactor Seams First

- [ ] 3.1 Introduce narrow DataTree service interfaces for cache, mediator/property table, diagnostics, writing-system access, and LCModel access without changing behavior.
- [ ] 3.2 Extract refresh coordination into a testable service or state object while preserving current behavior.
- [ ] 3.3 Put an editor registry boundary in front of `SliceFactory` so editor keys can resolve to legacy slices now and Avalonia editors later.
- [ ] 3.4 Extract at least one launcher humble object path, using morph type swap as the first target.
- [ ] 3.5 Define host/surface interfaces around `RecordEditView`/`DataTree` initialization, focus, context menus, and view replacement.

## 4. Typed View Definition and XML Import

- [ ] 4.1 Define the typed view-definition model for sections, fields, sequences, tables, tree nodes, editor descriptors, visibility, ghost behavior, stable IDs, writing-system metadata, and accessibility metadata.
- [ ] 4.2 Implement or extend XML Parts/Layout import into typed view-definition/Presentation IR using existing Inventory/LayoutCache semantics where feasible.
- [ ] 4.3 Add deterministic snapshot tests for compiled IR from LexEntry detail layouts and selected override fixtures.
- [ ] 4.4 Add unsupported-construct diagnostics with layout part and node path.
- [ ] 4.5 Add cache key, invalidation, async compile, and cancellation tests.

## 5. Avalonia Control Slices

- [ ] 5.1 Replace/prove simple text and scalar editors with FieldWorks-owned Avalonia controls over IR nodes.
- [ ] 5.2 Implement writing-system-aware text editor behavior using project font settings, flow direction, culture/script metadata, and OpenType/HarfBuzz feature settings.
- [ ] 5.3 Implement popup/hover chooser controls using Avalonia flyouts/context menus and a service-backed chooser model.
- [ ] 5.4 Spike TreeView/tree-table rendering for multiple translations per sense/term, including compact multi-writing-system node templates.
- [ ] 5.5 Record any Avalonia package update or local/upstream control patch with parity justification and test evidence.

## 6. Tables, Slices, and Lexical Edit Migration

- [ ] 6.1 Build a virtualized Avalonia table/browse view path over typed view definitions.
- [ ] 6.2 Compare legacy XMLViews table semantics against typed IR and Avalonia table semantics.
- [ ] 6.3 Migrate one representative vertical slice: LexEntry identity + morph type + one nested sense/gloss + chooser path.
- [ ] 6.4 Expand to core P0/P1 parity checklist items from the migrated Speckit parity list.
- [ ] 6.5 Gate full Lexical Edit replacement on semantic parity, UIA2 legacy baselines, Avalonia.Headless tests, and render comparison evidence.

## 7. XML Retirement Planning

- [ ] 7.1 Design canonical post-XML view-definition authoring/storage format.
- [ ] 7.2 Build XML-to-typed-definition migration tooling and audit reports.
- [ ] 7.3 Prove migration on shipped LexEntry/LexSense layouts and selected user override fixtures.
- [ ] 7.4 Disable runtime XML for a gated migrated surface while retaining import/audit fallback.
- [ ] 7.5 Document remaining XML blockers, especially custom fields, ghost items, table views, choosers, TreeView-heavy views, and Graphite-only text behavior.

## 8. Validation

- [ ] 8.1 Run targeted managed tests for changed areas using `./test.ps1` filters or relevant VS Code tasks.
- [ ] 8.2 Run render/parity baseline tests for affected surfaces.
- [ ] 8.3 Run `./build.ps1` before implementation work is considered ready for review.
- [ ] 8.4 Run `CI: Full local check` before commit/push.