## Context

Lexical Edit currently depends on a WinForms/DataTree/DetailControls stack that interprets XML Parts/Layout into `Slice` controls, launchers, chooser dialogs, nested `ViewSlice` content, and Views-backed rendering. The Advanced Entry Speckit work under `specs/010-advanced-entry-view/` already proves several useful ideas: a net8 Avalonia module, Preview Host, Presentation IR, XML contract loading, caching, headless tests, and parity checklist. The new target is larger: migrate the real Lexical Edit surface while preserving user interaction, density, writing-system behavior, and customizability, then retire XML after the Avalonia switch is proven.

This branch is the current foundation-and-integration slice: it documents the architecture, keeps legacy characterization tests that protect Phase 3 refactors, and now includes the net48 `FwAvalonia` spike, typed view-definition/seam foundation code, a net48 `FwAvaloniaPreviewHost` + preview-host UIA smoke tests, and product-facing app-wide lexical-edit UI mode wiring through existing `RecordEditView` hosts. The older net8 Preview Host/AdvancedEntry prototype remains intentionally split to `010-advanced-entry-preview-prototype` as a separate prototype track. Branch scope should be reviewed against the branch-only diff from `main`, not inferred from same-day commit timestamps.

Important current constraints:
- `DataTree`, `Slice`, `SliceFactory`, launchers, `RecordEditView`, XMLViews browse/table views, and xCore mediator behavior are tightly coupled.
- XML Parts/Layout carries real customer customizations and behavior such as custom fields, ghost items, visibility rules, and chooser hints.
- Render verification exists for WinForms/DataTree pixel and timing baselines, but it needs semantic snapshots to compare legacy, IR, and Avalonia outputs.
- Native Views/C++ viewing/rendering remains a real dependency for legacy regions. Avalonia migration is only complete for a region after that region no longer instantiates or calls native viewing/rendering/editor code for display, layout, measurement, hit testing, selection, scrolling, or editor realization.
- Graphite is present in the native Graphite/Views rendering path (`GraphiteEngine`, `GraphiteSegment`, render-engine selection), writing-system UI/storage (`IsGraphiteEnabled`, `DefaultFontFeatures`, `FontEngines.Graphite`), render tests, sample/dist assets, and build/package artifacts.
- Gecko/XULRunner is initialized during FieldWorks startup with `gfx.font_rendering.graphite.enabled = true`; `XWebBrowser` and `GeckofxHtmlToPdf` support XHTML preview, print, and PDF/export paths.
- Avalonia offers headless testing, `TextBox`, `TreeView`, `TreeDataGrid`, `ItemsRepeater`, `FlyoutBase`/context menus, styles, `FontFeatures`, and custom font hooks, but FieldWorks needs owned controls for dense, writing-system-aware editing.

## Goals / Non-Goals

**Goals:**
- Make Lexical Edit refactorable and testable before replacing major UI surfaces.
- Use XML Parts/Layout as an import/compatibility contract during transition, not as the final runtime abstraction.
- Introduce typed view-definition and Presentation IR interfaces suitable for dependency injection, semantic parity tests, and Avalonia rendering.
- Make the lexical-edit UI mode an app-wide product switch while keeping each current `RecordEditView` consumer on an explicit contract: supported Avalonia surface, explicit legacy fallback, or explicit blocked state.
- Preserve interaction behavior, information density, writing-system fonts, OpenType/HarfBuzz shaping behavior, nested structures, popup choosers, table views, and TreeView-heavy views.
- Decommission native Graphite/rendering from the default Lexical Edit path: Graphite work starts when the migration starts, and Avalonia does not become the default screen until Graphite dependencies are classified and either replaced, retained behind legacy fallback/export boundaries, or blocked with explicit diagnostics and rollback.
- Decommission C++ viewing/rendering dependencies by migrated region so completed Avalonia regions do not use native Views, `RootSite`, `IVwEnv`, `ManagedVwWindow`, or equivalent C++ display/layout/editor infrastructure at runtime. Custom linguistics services may remain when they are exposed through explicit service seams and do not own Avalonia viewing or editing surfaces.
- Extend render verification to capture semantic output, not only pixels and timings.
- Keep Avalonia code and tests on the normal repo build/test path; build strategy must not become the way we select legacy vs Avalonia behavior.

**Non-Goals:**
- No one-shot rewrite of DataTree, XMLViews, and Lexical Edit.
- No immediate XML deletion. XML retirement waits for migration tooling and parity gates.
- No global native Views deletion before all consumers are migrated or explicitly retained. During transition, native Views can remain for non-migrated regions and baseline comparison, but not inside a completed Avalonia region.
- No unproven Graphite compatibility claim in Avalonia. Graphite-only fonts and feature strings are migration inputs to audit, warn, convert where possible, replace, or explicitly block; they are not assumed safe runtime targets.
- No promise of exact pixel parity with WinForms/C++ Views. The target is near-pixel parity with stable interaction semantics and density.

## Decisions

### 1. Refactor first, then Avalonia

**Decision:** Sequence the work as test coverage and seams, then simple controls/popups, then table views, then slices and full Lexical Edit views.

**Rationale:** DataTree refresh, slice creation, launcher behavior, and XML resolution are high-risk hidden dependencies. Avalonia work that starts by wrapping `DataTree` would preserve the wrong boundary and make regressions harder to identify.

**Alternatives considered:**
- Direct Avalonia rewrite: too risky because XML semantics, refresh behavior, and chooser logic would be reimplemented without baselines.
- Embed Avalonia inside existing slices: useful for isolated experiments, but not a migration architecture.

### 2. Typed view definition is the long-term contract

**Decision:** Introduce a managed typed view-definition model and Presentation IR. XML Parts/Layout is imported into this model during transition; Avalonia consumes the typed model, not XML or WinForms slices.

**Rationale:** This keeps customer customizations alive while creating a clean boundary for DI, tests, and eventual XML retirement. It also lets the render framework compare legacy XML-derived output with future non-XML definitions.

**Alternatives considered:**
- Keep XML as permanent contract: preserves compatibility but does not solve maintainability.
- Pure LCModel metadata-generated UI: attractive for 90% model-following, but insufficient for current grouping, ghost items, and chooser behavior without many overrides.

### 3. Owned Avalonia controls over permanent generic PropertyGrid dependence

**Decision:** Use the existing PropertyGrid path as a bootstrap, then move Lexical Edit to FieldWorks-owned dense controls over IR nodes.

**Rationale:** Stock property grids are poor fits for nested senses/examples, multi-writing-system alternatives, custom chooser flyouts, dense table rows, TreeView nodes with multiple translations, and FieldWorks-specific text behavior.

**Framework grounding:** Avalonia supports headless tests, `TextBox` input, `TreeView`/`TreeDataTemplate`, `TreeDataGrid` template columns, `ItemsRepeater`, flyouts/context menus, styles/classes, `FontFeatures`, and font-manager hooks. Those are enough for much of the UI, but the composition and editor registry should be FieldWorks-owned.

### 4. UIA2 for legacy smoke, Avalonia.Headless for new UI

**Decision:** Use UIA2/FlaUI-style automation to baseline WinForms workflow reachability and accessibility metadata. Use Avalonia.Headless for Avalonia control behavior, input, and selected screenshots. Put business logic in unit/integration tests.

**Rationale:** WinForms owner-drawn and Views-backed content is not deeply inspectable via UIA. UIA2 can still drive focus, menus, chooser launch, dialogs, and table headers. Avalonia.Headless gives invisible input tests and optional frame capture for the new UI.

### 5. Semantic parity is a first-class render artifact

**Decision:** Extend render verification with semantic snapshots: visible fields, labels, object/flid binding, editor kind, ghost state, expansion state, focus order, accessibility identity, writing-system metadata, and timing buckets.

**Rationale:** Pixel comparisons catch visual regressions but do not explain whether a missing field is an XML compiler issue, slice filtering issue, editor registry issue, or text rendering difference.

### 6. C++ viewing/rendering decommissioning is a regional completion gate

**Decision:** A migrated Avalonia region is not complete until it has no runtime dependency on native Views/C++ code that owns viewing, display, layout, measurement, hit testing, selection, scrolling, or editor realization. Legacy C++ viewing/rendering may remain temporarily for non-migrated regions and for baseline comparison, but completed Avalonia regions must render and edit through Avalonia-managed controls and text services.

**Rationale:** This keeps the end state honest. If Avalonia renders a surface but still relies on `RootSite`, `IVwEnv`, `ManagedVwWindow`, or the native Views box/render pipeline for core display, the migration has only wrapped the old system rather than replaced it.

**Feasibility:** This is feasible by region if we treat C++ viewing/rendering removal as a phased dependency audit rather than a single repo-wide deletion. With Graphite decommissioned instead of supported, the meaningful choices move to font replacement, OpenType feature storage, and shared native Views consumers. Custom linguistics engines such as XAmple, spelling, parser/conversion tools, ICU, or Encoding Converters can remain when wrapped as services outside the Avalonia render/editor path. Physical deletion of shared native Views code can happen only after every consumer outside the region is migrated or intentionally retained.

**Alternatives considered:**
- Keep native Views under Avalonia for hard text/layout cases: faster short term, but violates the migration goal and keeps the C++ viewing/rendering dependency alive.
- Delete native Views globally first: not feasible because other FieldWorks regions still depend on it and would lose functionality before replacements exist.

### 7. Graphite and native rendering are evidence-gated before Avalonia becomes default

**Decision:** Graphite/native-rendering decommissioning begins at the start of the Lexical Edit Avalonia migration. The default Avalonia Lexical Edit path must not depend on native Graphite render engines, Gecko Graphite rendering, or unclassified Graphite-only feature settings. Legacy fallback/export consumers may remain only when explicitly classified outside the migrated default path.

**Rationale:** Avalonia documents custom TrueType/OpenType fonts and OpenType `FontFeatures`, but that does not prove FieldWorks Graphite parity. HarfBuzz Graphite2 shaping requires HarfBuzz to be built with Graphite2 enabled, and HarfBuzz documentation says that support is not enabled by default. Graphite behavior therefore needs fixture evidence, not assumption.

**Research map:** The decommissioning scope includes `Src/views/lib/GraphiteEngine.*`, `Src/views/lib/GraphiteSegment.*`, `RenderEngineFactory`, Graphite feature UI/storage, persisted writing-system flags/features such as `IsGraphiteEnabled` and `DefaultFontFeatures`, Graphite-specific tests/docs/sample fonts, build/package artifacts, Gecko startup preference `gfx.font_rendering.graphite.enabled`, `XWebBrowser` preview consumers, and `GeckofxHtmlToPdf`/`FieldWorksPdfMaker` print/PDF assumptions.

**Feasibility:** Feasible by region, but intentionally disruptive for Graphite-only fonts. There is no automatic lossless Graphite-to-OpenType conversion. The migration must identify affected projects/fonts, provide replacement OpenType fonts or explicit user-facing compatibility warnings, and block default enablement for unsupported cases.

**Alternatives considered:**
- Assume Avalonia/HarfBuzz covers Graphite: rejected because official docs make Graphite2 shaping build-dependent.
- Keep Gecko only for Graphite previews/PDFs in the default workflow: rejected for migrated default Lexical Edit, but possible as a classified legacy/export boundary.

### 8. Region manifests and hard gates define completion

**Decision:** Every migrated Avalonia region must have a region manifest before implementation: entry points, typed view-definition sources, allowed legacy adapters, forbidden native viewing/rendering and Graphite symbols, retained custom linguistics service dependencies, parity fixtures, customer override fixtures, accessibility IDs, performance budgets, default-switch gates, and rollback behavior.

**Rationale:** “Migrated region” needs to be measurable. A manifest turns architecture intent into a testable contract and prevents accidental native Views, Graphite, WinForms, Gecko, or runtime XML dependencies from slipping through a narrow visible slice.

### 9. Avalonia platform services are explicit ports

**Decision:** Avalonia regions use explicit platform-facing ports for UI dispatch, region lifetime/disposal, focus navigation, command routing, edit sessions, validation, undo/redo grouping, design/preview data, styling resources, and accessibility metadata. These ports are introduced while WinForms remains the default so legacy behavior can be characterized first.

**Rationale:** Avalonia has different threading, focus, command, validation, popup, and lifetime behavior than WinForms. If those seams remain implicit, the first “simple” editor will inherit DataTree and xCore assumptions through the side door.

### 10. Full shell/window replacement is a separate phase-two change

**Decision:** This change remains scoped to Lexical Edit regional migration. Replacement of FieldWorks startup, main windows, shell composition, menus, toolbars, navigation, dialogs, and all main screens is tracked separately by `fieldworks-avalonia-shell-migration`.

**Rationale:** Lexical Edit proves the hardest regional rendering/editor path. The app shell touches different risks: application lifetime, multi-window behavior, xCore command routing, project startup/shutdown, dialog ownership, persisted layout, global services, installer/runtime packaging, and remaining main screens. Splitting keeps both plans reviewable and gives the shell phase concrete prerequisites.

### 11. Seam recommendations are fixed in dedicated capability specs with explicit phase timing

**Decision:** This change records seam recommendations in dedicated capability specs: `avalonia-edit-sessions`, `avalonia-undo-redo`, `avalonia-validation`, `avalonia-command-focus`, `avalonia-ui-scheduler`, and `avalonia-lifetime`. Detailed comparison notes, alternatives considered, current/proposed status, and source references are tracked in `seam-recommendations.md`.

**Rationale:** Edit sessions, undo/redo, validation, command/focus, scheduler, and lifetime are the places where a migration can quietly hard-code a wrong abstraction. Freezing the recommendation and the pivot options in dedicated specs makes those choices reviewable, testable, and reusable by the later shell change.

**Phase map:**
- Up front and before non-view Avalonia code spreads: introduce `avalonia-ui-scheduler` and `avalonia-lifetime` seams only where tests need UI-thread marshalling, cancellation, disposal, or late-callback control.
- First editable slice: apply `avalonia-edit-sessions`, `avalonia-undo-redo`, `avalonia-validation`, and the screen-local phase of `avalonia-command-focus` before scaling to broader editable regions.
- Phase-two shell migration: invoke the shell-global phase of `avalonia-command-focus`, `avalonia-ui-scheduler`, and `avalonia-lifetime` through `fieldworks-avalonia-shell-migration` instead of redefining them there.
- Deferred or separate-track options: package-first edit sessions, package-first undo/redo, and heavy region-lifetime frameworks remain available only if the pivot triggers documented in `seam-recommendations.md` are met.

### 12. The lexical-edit UI mode is global, but host behavior is explicit per consumer

**Decision:** The lexical-edit UI mode is app-wide. Changing it affects every host that routes through `RecordEditView` or a later replacement, but each host must declare its behavior under both modes: supported Avalonia surface, explicit legacy fallback, or explicit blocked state with a deliberate product-facing message.

**Rationale:** A single product switch keeps user behavior simple and prevents hidden feature islands, but a global setting cannot imply that every consumer is equally migrated. The contract must therefore stay explicit per host so unsupported surfaces do not drift into ambiguous best-effort routing.

**Alternatives considered:**
- Per-screen or preview-only flags: rejected because they obscure the product contract and make it harder to audit what the user actually gets.
- Implicit fallback decided inside each host without a manifest: rejected because it hides migration status and encourages silent lossy routing.

### 13. Avalonia build and test coverage stays on the normal repo workflow

**Decision:** Avalonia projects and tests participate in the normal repo build and test flow. `./build.ps1` and `./test.ps1` remain the integration entry points; runtime UI mode selects behavior after build, not which code is built or validated.

**Rationale:** Branch-local or optional build lanes are useful as temporary implementation details but should not define product confidence. Reviewers need one build/test story for both legacy and Avalonia code paths.

**Alternatives considered:**
- Separate `BuildAvalonia` or preview-only validation lanes as the main evidence path: rejected because they make product validation depend on opt-in behavior rather than the normal repo workflow.

## Native Dependency Classification

The classification rule is based on the role of the native code, not the implementation language alone. If native code owns what the user is viewing or editing, it is not brought into completed Avalonia regions. If native code supplies custom linguistics capability that supports FieldWorks' role in documenting many languages, it may remain behind an explicit service seam.

- **Native Views layout/render/editing:** `VwRootBox`, `IVwEnv`, `IRenderEngine`, selections, hit testing, `OnTyping`, `OnExtendedKey`, table layout, interlinear layout, and RootSite editing are not mere windowing. They are the render/editor pipeline and remain a hard removal gate for migrated regions.
- **Custom linguistics services:** XAmple, spelling, parser/conversion engines, ICU, Encoding Converters, and similar language-documentation capabilities are allowed to remain in C++ or native/external form when invoked through managed service boundaries. Avalonia may consume their results, but it must not depend on their UI, rendering, or RootBox integration.
- **Spell-check interop:** `RootSite` wires `SetSpellingRepository(IGetSpellChecker)` into `VwRootBox`, while managed helpers build spelling context menus. Avalonia can keep spelling as a service, but any dependency on RootBox spell integration must be replaced for migrated regions.
- **Parser/conversion/native utility tools:** `pcpatr64.exe`, `TonePars64.exe`, `xample.dll`, Encoding Converter native files, ICU artifacts, Expat/ParserObject, and reg-free COM/proxy/stub build infrastructure are real native dependencies. They are not default Lexical Edit viewing dependencies, but migrated workflows that invoke them must wrap them as services and keep them outside Avalonia rendering/editor completion gates.

## Interface Direction

Early seams should stay narrow and name the FieldWorks domain they protect:

- `ILexicalRefreshCoordinator` for refresh/postponed `PropChanged` behavior.
- `IViewDefinitionSource`, `IXmlViewDefinitionImporter`, `IViewDefinitionCompiler`, `IViewDefinitionCache`, and `IViewDefinitionDiagnostics` for the XML-to-typed transition.
- Proposed `ILexicalEditorRegistry`, `EditorDescriptor`, and `ILexicalEditorFactory` boundaries for resolving legacy slices now and future Avalonia editors later.
- Proposed `IEditSession` or `IEditTransactionCoordinator` boundaries for LCModel transactions, validation, cancellation, undo/redo grouping, and dirty-state command enablement. The current AdvancedEntry code uses a concrete fenced edit session.
- Proposed `IXCoreCommandBridge`, `IPropertyStateStore`, `IRecordNavigationContext`, `IUiScheduler`, `IFocusNavigationService`, and `IRegionLifetime` boundaries for current xCore/DataTree behaviors that must not be hidden inside a single broad context object.
- Feature-specific custom linguistics ports such as `ISpellingService`, `IMorphParserService`, and `IEncodingConversionService` only when a migrated editor actually needs them.

## Architecture Diagrams

See [architecture-diagrams.md](architecture-diagrams.md) for Mermaid diagrams covering the current WinForms/DataTree architecture, MVC pressure, dependency-inversion seams, testing layers, optional first Avalonia slices, table/full Lexical Edit slices, and the final audited Avalonia default architecture.

See [seam-recommendations.md](seam-recommendations.md) for the accepted seam recommendations, the three options compared for each seam, references used, and the pivot triggers that would justify changing direction later.

## Refactoring Split Options

### Option A: Safety-first legacy seams

Split by existing risk: test coverage and docs, refresh/DataTree services, launcher humble objects, editor registry, semantic render capture, then Avalonia controls.

**Best for:** Regression safety and small PRs.
**Trade-off:** Slower time to visible Avalonia progress.

### Option B: Contract-first migration

Split by the new architecture: view-definition schema, XML importer, IR compiler/cache, semantic parity harness, then Avalonia renderer/editor registry.

**Best for:** Fast progress on XML retirement and future non-XML definitions.
**Trade-off:** More risk if DataTree refresh/launcher seams are not stabilized first.

### Option C: Vertical thin slice

Pick a representative lexical path, such as LexEntry morph type plus nested sense gloss and one popup chooser: baseline legacy, compile XML to IR, render/edit in Avalonia Headless, compare semantic/pixel artifacts.

**Best for:** Proving the end state early and exposing framework gaps.
**Trade-off:** Leaves broad legacy debt in place and can tempt ad hoc special cases.

**Recommendation:** Use Option A for the first two refactor PRs, then Option C for the first full Avalonia slice, while building the typed view-definition pieces from Option B as shared infrastructure.

## Risks / Trade-offs

- XML import drift from legacy behavior -> Mitigate with semantic snapshots and parity tests against production layouts and user-override fixtures.
- Refresh protocol regressions -> Extract/cover refresh coordination before UI replacement.
- TreeView/table complexity -> Spike dense custom item templates, TreeDataGrid license/version implications, and owned virtualized row templates early.
- Graphite/native rendering decommissioning -> Begin the inventory at migration start and block Avalonia default until Graphite engines, feature UI/storage, sample fonts, Gecko Graphite prefs, PDF/export assumptions, and tests/docs are classified, replaced, moved behind legacy boundaries, or blocked with diagnostics.
- Gecko/browser rendering -> `XWebBrowser`, dictionary/configuration previews, interlinear configuration previews, print, and `GeckofxHtmlToPdf` need a non-Graphite replacement or an explicit non-default legacy boundary.
- PropertyGrid limits -> Treat it as a prototype path; do not let it define the final IR or UI shape.
- Automation flakiness -> Keep UIA2 tests thin; use model/semantic assertions for deep behavior.
- XML retirement too early -> Gate deletion on migration tooling, custom-field coverage, user overrides, ghost behavior, chooser parity, and fallback ability.
- C++ viewing/rendering removal exposes text/layout gaps -> Gate each region on dependency audits and replacement services for text shaping, selection, measurement, hit testing, scrolling, and printing/export behaviors where applicable. Do not count custom linguistics service calls as blockers unless they own UI viewing/editing behavior.
- Over-broad interfaces -> Prefer small domain ports proven by legacy characterization tests; avoid a single “context” service that preserves DataTree/Mediator/PropertyTable coupling.
- Undo/redo, focus, keyboard/IME, and lifecycle regressions -> Treat edit sessions, command routing, UI dispatch, focus restoration, and disposal/unsubscribe behavior as first-slice gates, not cleanup work.
- Typed IR becomes a second UI framework -> Version the core view definition, keep instance presentation state separate, and add diagnostics for behavior the IR cannot express yet.

## Migration Plan

1. Freeze current behavior with targeted unit/integration/render/UIA2 baselines, including undo/redo, focus, keyboard/IME, accessibility, localization, customer overrides, and disposal behavior.
2. Define the app-wide lexical-edit UI mode contract and explicit per-host behavior matrix before expanding product wiring beyond the first hosts.
3. Introduce DI-friendly services around DataTree refresh, view-definition source/import/compile/cache, editor selection, command/property/navigation state, edit sessions, UI dispatch, lifetime, LCModel access, and launcher logic, following `avalonia-ui-scheduler`, `avalonia-lifetime`, and the local phase of `avalonia-command-focus`.
4. Keep Avalonia build/test integration on the normal repo scripts while the runtime UI mode remains the only product selection mechanism.
5. Start Graphite/native rendering decommissioning: inventory affected project settings, fonts, render engines, Gecko/PDF paths, tests, docs, and build artifacts; prove no default-path claim depends on unverified Graphite behavior.
6. Define migrated-region manifests and hard gates for each proposed Avalonia region.
7. Extend render verification with normalized semantic snapshots, visual/timing evidence, performance budgets, and failure bundles.
8. Build typed view-definition and XML import as the compatibility compiler.
9. Replace text foundation, simple controls, edit sessions, validation, undo/redo routing, and hover/popups in Avalonia using owned editor controls, following `avalonia-edit-sessions`, `avalonia-validation`, `avalonia-undo-redo`, and the local phase of `avalonia-command-focus`.
10. Replace table/browse views with virtualized Avalonia table/tree structures.
11. Replace slices and full Lexical Edit views with Avalonia surfaces over the typed contract.
12. Audit the migrated region's runtime call graph and remove/disable native viewing/rendering/editor dependencies for that region, while classifying custom linguistics engines as service seams when they do not own the Avalonia UI surface.
13. Add managed canonical view-definition authoring and migration tooling.
14. Retire runtime XML only after parity gates pass for production layouts, custom fields, user overrides, dynamic editors, unsupported constructs, and fallback behavior.
15. Invoke the shell-global phase of `avalonia-command-focus`, `avalonia-ui-scheduler`, and `avalonia-lifetime` through `fieldworks-avalonia-shell-migration` once Lexical Edit regional seams are proven.

## Open Questions

1. Should the canonical post-XML view-definition format be C# builders, JSON/YAML, resources, database-backed project settings, or a hybrid?
2. Which shipped/sample/customer fonts and writing systems require replacement or migration because they depend on Graphite-only shaping or feature IDs?
3. Is `TreeDataGrid` acceptable for any Lexical Edit surface given package/licensing/version constraints, or should FieldWorks own all dense tree/table rows?
4. Which customer layout override fixtures should become mandatory migration tests?
5. Which non-Lexical Edit consumers keep native Views alive after Lexical Edit regions are migrated, and what is the repo-wide deletion plan once those consumers are addressed?
6. Which browser/PDF engine or legacy boundary will own XHTML preview, print, and PDF behavior after default Lexical Edit moves away from Gecko/Graphite assumptions?
