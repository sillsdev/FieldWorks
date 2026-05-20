# Tasks

## 1. Migration Baseline and Spec Audit

- [x] 1.1 Review Speckit artifacts against this OpenSpec change and keep `migration-map.md` current.
- [x] 1.2 Inventory current Lexical Edit view entry points: `RecordEditView`, `DataTree`, `SliceFactory`, XMLViews browse/table views, popup choosers, and AdvancedEntry Avalonia spike.
- [x] 1.3 Build a coverage map for DataTree refresh, SliceFactory/editor selection, launchers, popup choosers, XML table views, and render verification.
- [x] 1.4 Identify customer/user override XML fixtures that must be included before XML retirement.
- [x] 1.5 Start Graphite decommissioning inventory for writing-system settings, fonts, native render engines, Gecko/browser/PDF paths, tests, docs, sample assets, and build/package artifacts.
- [x] 1.6 Define migrated-region manifest format: entry points, allowed legacy adapters, forbidden symbols/call paths, custom linguistics service dependencies, parity fixtures, performance budgets, accessibility IDs, and rollback/default-switch gates.
- [x] 1.7 Freeze and maintain the seam capability docs `avalonia-edit-sessions`, `avalonia-undo-redo`, `avalonia-validation`, `avalonia-command-focus`, `avalonia-ui-scheduler`, `avalonia-lifetime`, and `seam-recommendations.md` as the reference playbook for both this change and the later shell change.

## 2. Test Coverage Before Refactor

- [x] 2.1 Add or extend unit/integration tests for DataTree refresh state transitions and postponed `PropChanged` behavior.
- [x] 2.2 Add or extend launcher pure-logic tests, prioritizing morph type swap/data-loss logic and chooser decision paths.
- [x] 2.3 Add semantic baseline capture for DataTree/Slice output: sections, labels, object/flid bindings, editor kind, visibility, ghost state, expansion, focus order, and accessibility identity.
- [x] 2.4 Add focused UIA2 smoke baselines for WinForms launcher/chooser workflows and XMLViews table header/filter reachability.
- [x] 2.5 Add failure artifact bundling to render/parity tests where missing.
- [x] 2.6 Add undo/redo and LCModel transaction characterization tests for editor replacement candidates.
- [x] 2.7 Add keyboard/IME, focus restoration, accessibility metadata, localization, and disposal/unsubscribe characterization tests for first-slice candidates.
- [x] 2.8 Add snapshot normalization rules so semantic baselines key on stable node IDs, class/flid/object binding, editor kind, writing-system metadata, ghost state, focus order, and accessibility identity instead of incidental layout noise.

## 3. Refactor Seams First

- [ ] 3.1 Introduce narrow DataTree service interfaces without changing behavior: `ILexicalRefreshCoordinator`, `IXCoreCommandBridge`, `IPropertyStateStore`, `IRecordNavigationContext`, diagnostics, writing-system access, and LCModel access.
- [ ] 3.2 Extract refresh coordination into a testable service or state object while preserving current behavior.
- [ ] 3.3 Put an `ILexicalEditorRegistry` boundary in front of `SliceFactory` so editor keys can resolve to legacy slices now and Avalonia editors later.
- [ ] 3.4 Extract at least one launcher humble object path, using morph type swap as the first target.
- [ ] 3.5 Define host/surface interfaces around `RecordEditView`/`DataTree` initialization, focus, context menus, and view replacement.
- [ ] 3.6 Extract edit-session and transaction seams for staged values, validation, cancellation, dirty state, undo/redo grouping, and LCModel commit behavior, following `avalonia-edit-sessions` and `avalonia-undo-redo`.
- [ ] 3.7 Extract UI scheduling, focus navigation, command routing, and region lifetime/disposal seams before introducing editable Avalonia controls, following `avalonia-command-focus`, `avalonia-ui-scheduler`, and `avalonia-lifetime`.
- [ ] 3.8 Inventory dynamic editor strings and custom editor constructs (`custom`, `customwithparams`, `autocustom`, loader-based editors, fallback slices) with diagnostics requirements.

## 4. Typed View Definition and XML Import

- [ ] 4.1 Define the typed view-definition model for sections, fields, sequences, tables, tree nodes, editor descriptors, visibility, ghost behavior, stable IDs, writing-system metadata, command affordances, validation hints, virtualization hints, localization/resource keys, and accessibility metadata.
- [ ] 4.2 Implement or extend XML Parts/Layout import into typed view-definition/Presentation IR using existing Inventory/LayoutCache semantics where feasible.
- [ ] 4.3 Add deterministic snapshot tests for compiled IR from LexEntry detail layouts and selected override fixtures.
- [ ] 4.4 Add unsupported-construct diagnostics with layout part and node path.
- [ ] 4.5 Add cache key, invalidation, async compile, and cancellation tests.
- [ ] 4.6 Ensure off-thread compilation uses immutable layout, metadata, writing-system, custom-field, and override snapshots rather than live WinForms controls, `PropertyTable`, or cache mutation state.

## 5. Graphite and Font Decommissioning

- [ ] 5.1 Inventory and classify Graphite/native rendering code/assets: `Src/views/lib/GraphiteEngine.*`, `Src/views/lib/GraphiteSegment.*`, render-engine selection, Graphite feature UI/storage, sample/dist assets, package/build artifacts, and Graphite-specific tests/docs.
- [ ] 5.2 Inventory writing-system Graphite settings and persistence: `IsGraphiteEnabled`, `DefaultFontFeatures`, `FontEngines.Graphite`, import/export formats, project fixtures, and user-visible settings.
- [ ] 5.3 Classify Graphite feature UI/storage in writing-system dialogs and define OpenType/HarfBuzz replacements where supported, plus explicit diagnostics/rollback for unsupported Graphite-only settings.
- [ ] 5.4 Define replacement font/fallback policy for Graphite-only fonts, including project diagnostics and user-facing migration evidence.
- [ ] 5.5 Prove the migrated Avalonia default path has no unapproved runtime dependency on native Graphite render engines, Graphite-enabled legacy render selection, Gecko Graphite rendering, or unclassified Graphite-only feature settings.
- [ ] 5.6 Audit Gecko/XULRunner preview, print, and PDF paths: startup Graphite preference, `XWebBrowser` consumers, dictionary/interlinear/configuration previews, `GeckofxHtmlToPdf`, and `FieldWorksPdfMaker` packaging.
- [ ] 5.7 Select and validate a non-Graphite browser/PDF strategy for default Avalonia workflows, or explicitly leave affected paths outside the default Lexical Edit boundary.
- [ ] 5.8 Add validation proving Avalonia default readiness is blocked while any unapproved default-path Graphite/native-rendering dependency or unsupported Graphite-only setting remains.

## 6. Avalonia Control Slices

- [ ] 6.1 Replace/prove writing-system text display/editor foundation and simple scalar editors with FieldWorks-owned Avalonia controls over IR nodes.
- [ ] 6.2 Implement writing-system-aware text editor behavior using project font settings, flow direction, culture/script metadata, supported OpenType/HarfBuzz feature settings, and diagnostics for unsupported Graphite-only settings.
- [ ] 6.3 Implement popup/hover chooser controls using Avalonia flyouts/context menus and a service-backed chooser model.
- [ ] 6.4 Spike TreeView/tree-table rendering for multiple translations per sense/term, including compact multi-writing-system node templates.
- [ ] 6.5 Record any Avalonia package update or local/upstream control patch with parity justification and test evidence.
- [ ] 6.6 Add Avalonia.Headless tests for command shortcuts, popup focus return, validation errors, edit commit/cancel, keyboard/IME behavior, accessibility metadata, and disposal cleanup.
- [ ] 6.7 Add styling/resource and density token gates for shared `FwAvalonia` resources before broad editor rollout.
- [ ] 6.8 Make the first editable Avalonia slice satisfy `avalonia-edit-sessions`, `avalonia-validation`, `avalonia-undo-redo`, and the local screen phase of `avalonia-command-focus` before expanding to more editors.

## 7. Tables, Slices, and Lexical Edit Migration

- [ ] 7.1 Build a virtualized Avalonia table/browse view path over typed view definitions.
- [ ] 7.2 Compare legacy XMLViews table semantics against typed IR and Avalonia table semantics.
- [ ] 7.3 Migrate one representative vertical slice: LexEntry identity + morph type + one nested sense/gloss + chooser path.
- [ ] 7.4 Expand to core P0/P1 parity checklist items from the migrated Speckit parity list.
- [ ] 7.5 Gate full Lexical Edit replacement on semantic parity, UIA2 legacy baselines, Avalonia.Headless tests, render comparison evidence, native viewing/render seam audit evidence, and no unapproved Graphite/native-rendering default-path dependency.
- [ ] 7.6 Add a control-selection decision matrix for `TreeView`, `TreeDataGrid`, `ItemsRepeater`, and owned virtualized controls using density, virtualization, selection, accessibility, licensing/version, and multi-writing-system criteria.
- [ ] 7.7 Add large-fixture performance budgets for open time, scroll/expand latency, typing latency, realized control count, memory, and cache invalidation.

## 8. C++ Viewing/Render Seam Decommissioning

- [ ] 8.1 Inventory all native Views/C++ viewing/rendering/editor dependencies reachable from the targeted Lexical Edit region, including `RootSite`, `IVwEnv`, RootBox/ViewSlice paths, `ManagedVwWindow`, measurement, selection, hit testing, scrolling, editor realization, and text rendering adapters.
- [ ] 8.2 Classify dependencies as baseline-only, non-migrated-region-only, custom linguistics service dependency, or blocker for the targeted migrated region.
- [ ] 8.3 Replace region-local C++ viewing/rendering/editor usage with managed/Avalonia services for text shaping, measurement, selection metadata, hit testing, scrolling, rendering, and editor realization.
- [ ] 8.4 Add tests or instrumentation proving the migrated region does not instantiate or call native Views/C++ viewing/rendering/editor infrastructure at runtime.
- [ ] 8.5 Remove or disable region-local native viewing/render adapters after replacement tests pass, while leaving shared native Views code available for non-migrated consumers.
- [ ] 8.6 Track any repo-wide native Views deletion blockers that remain outside the migrated Lexical Edit region.
- [ ] 8.7 Classify non-viewing native dependencies such as spell-check interop, parser tools, XAmple, Encoding Converters, ICU, Expat/ParserObject, and reg-free COM tooling as custom linguistics/service/tool dependencies unless they own display, layout, hit testing, selection, editor realization, or other Avalonia viewing behavior.
- [ ] 8.8 Define service seams for retained custom linguistics engines so Avalonia consumes results through managed contracts and never hosts their UI/render/editor infrastructure.

## 9. XML Retirement Planning

- [ ] 9.1 Design canonical post-XML view-definition authoring/storage format.
- [ ] 9.2 Build XML-to-typed-definition migration tooling and audit reports.
- [ ] 9.3 Prove migration on shipped LexEntry/LexSense layouts and selected user override fixtures.
- [ ] 9.4 Disable runtime XML for a gated migrated surface while retaining import/audit fallback.
- [ ] 9.5 Document remaining XML blockers, especially custom fields, ghost items, table views, choosers, TreeView-heavy views, and any remaining native viewing/render coupling.

## 10. Validation

- [ ] 10.1 Run targeted managed tests for changed areas using `./test.ps1` filters or relevant VS Code tasks.
- [ ] 10.2 Run render/parity baseline tests for affected surfaces.
- [ ] 10.3 Run native viewing/render seam audit tests/instrumentation for any region claimed as migrated.
- [ ] 10.4 Run Graphite/native-rendering default-path validation for any region proposed as default Avalonia UI.
- [ ] 10.5 Run browser/PDF replacement validation for default-path XHTML preview, print, or PDF flows.
- [ ] 10.6 Run `./build.ps1` before implementation work is considered ready for review.
- [ ] 10.7 Run `CI: Full local check` before commit/push.
- [ ] 10.8 Verify every migrated-region manifest has passing evidence for native-call instrumentation, no unapproved Graphite/native-rendering default-path dependency, undo/redo, accessibility, localization, keyboard/IME, customer override fixtures, performance budgets, and rollback behavior.
- [ ] 10.9 Invoke the shell-global phase of `avalonia-command-focus`, `avalonia-ui-scheduler`, and `avalonia-lifetime` through `fieldworks-avalonia-shell-migration` instead of redefining those seams ad hoc during shell work.
