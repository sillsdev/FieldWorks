## Context

Lexical Edit currently depends on a WinForms/DataTree/DetailControls stack that interprets XML Parts/Layout into `Slice` controls, launchers, chooser dialogs, nested `ViewSlice` content, and Views-backed rendering. The Advanced Entry Speckit work under `specs/010-advanced-entry-view/` already proves several useful ideas: a net8 Avalonia module, Preview Host, Presentation IR, XML contract loading, caching, headless tests, and parity checklist. The new target is larger: migrate the real Lexical Edit surface while preserving user interaction, density, writing-system behavior, and customizability, then retire XML after the Avalonia switch is proven.

Important current constraints:
- `DataTree`, `Slice`, `SliceFactory`, launchers, `RecordEditView`, XMLViews browse/table views, and xCore mediator behavior are tightly coupled.
- XML Parts/Layout carries real customer customizations and behavior such as custom fields, ghost items, visibility rules, and chooser hints.
- Render verification exists for WinForms/DataTree pixel and timing baselines, but it needs semantic snapshots to compare legacy, IR, and Avalonia outputs.
- Avalonia offers headless testing, `TextBox`, `TreeView`, `TreeDataGrid`, `ItemsRepeater`, `FlyoutBase`/context menus, styles, `FontFeatures`, and custom font hooks, but FieldWorks needs owned controls for dense, writing-system-aware editing.

## Goals / Non-Goals

**Goals:**
- Make Lexical Edit refactorable and testable before replacing major UI surfaces.
- Use XML Parts/Layout as an import/compatibility contract during transition, not as the final runtime abstraction.
- Introduce typed view-definition and Presentation IR interfaces suitable for dependency injection, semantic parity tests, and Avalonia rendering.
- Preserve interaction behavior, information density, writing-system fonts, OpenType/HarfBuzz shaping behavior, nested structures, popup choosers, table views, and TreeView-heavy views.
- Extend render verification to capture semantic output, not only pixels and timings.

**Non-Goals:**
- No one-shot rewrite of DataTree, XMLViews, and Lexical Edit.
- No immediate XML deletion. XML retirement waits for migration tooling and parity gates.
- No new native UI dependency. Existing native Views remains a legacy baseline until replaced.
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
- Graphite-only behavior -> Make OpenType/HarfBuzz the Avalonia path, but explicitly identify Graphite-only writing systems and provide fallback or migration guidance before release.
- PropertyGrid limits -> Treat it as a prototype path; do not let it define the final IR or UI shape.
- Automation flakiness -> Keep UIA2 tests thin; use model/semantic assertions for deep behavior.
- XML retirement too early -> Gate deletion on migration tooling, custom-field coverage, user overrides, ghost behavior, chooser parity, and fallback ability.

## Migration Plan

1. Freeze current behavior with targeted unit/integration/render/UIA2 baselines.
2. Introduce DI-friendly services around DataTree refresh, context, LCModel access, editor selection, and launcher logic.
3. Extend render verification with semantic snapshots and failure bundles.
4. Build typed view-definition and XML import as the compatibility compiler.
5. Replace simple controls and hover/popups in Avalonia using owned editor controls.
6. Replace table/browse views with virtualized Avalonia table/tree structures.
7. Replace slices and full Lexical Edit views with Avalonia surfaces over the typed contract.
8. Add managed canonical view-definition authoring and migration tooling.
9. Retire runtime XML only after parity gates pass for production layouts, custom fields, and user overrides.

## Open Questions

1. Should the canonical post-XML view-definition format be C# builders, JSON/YAML, resources, database-backed project settings, or a hybrid?
2. How much Graphite-only behavior must remain supported in the Avalonia UI, and where will fallback shaping live?
3. Is `TreeDataGrid` acceptable for any Lexical Edit surface given package/licensing/version constraints, or should FieldWorks own all dense tree/table rows?
4. Which customer layout override fixtures should become mandatory migration tests?
