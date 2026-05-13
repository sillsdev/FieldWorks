## Context

Lexical Edit currently depends on a WinForms/DataTree/DetailControls stack that interprets XML Parts/Layout into `Slice` controls, launchers, chooser dialogs, nested `ViewSlice` content, and Views-backed rendering. The Advanced Entry Speckit work under `specs/010-advanced-entry-view/` already proves several useful ideas: a net8 Avalonia module, Preview Host, Presentation IR, XML contract loading, caching, headless tests, and parity checklist. The new target is larger: migrate the real Lexical Edit surface while preserving user interaction, density, writing-system behavior, and customizability, then retire XML after the Avalonia switch is proven.

Important current constraints:
- `DataTree`, `Slice`, `SliceFactory`, launchers, `RecordEditView`, XMLViews browse/table views, and xCore mediator behavior are tightly coupled.
- XML Parts/Layout carries real customer customizations and behavior such as custom fields, ghost items, visibility rules, and chooser hints.
- Render verification exists for WinForms/DataTree pixel and timing baselines, but it needs semantic snapshots to compare legacy, IR, and Avalonia outputs.
- Native Views/C++ viewing/rendering remains a real dependency for legacy regions. Avalonia migration is only complete for a region after that region no longer instantiates or calls native viewing/rendering/editor code for display, layout, measurement, hit testing, selection, scrolling, or editor realization.
- Graphite is present in the native `graphite2` library, the Views `GraphiteEngine` COM renderer, `RenderEngineFactory`, `GraphiteFontFeatures`, writing-system UI/storage (`IsGraphiteEnabled`, `DefaultFontFeatures`, `FontEngines.Graphite`), render tests, `DistFiles/Graphite`, and build targets.
- Gecko/XULRunner is initialized during FieldWorks startup with `gfx.font_rendering.graphite.enabled = true`; `XWebBrowser` and `GeckofxHtmlToPdf` support XHTML preview, print, and PDF/export paths.
- Avalonia offers headless testing, `TextBox`, `TreeView`, `TreeDataGrid`, `ItemsRepeater`, `FlyoutBase`/context menus, styles, `FontFeatures`, and custom font hooks, but FieldWorks needs owned controls for dense, writing-system-aware editing.

## Goals / Non-Goals

**Goals:**
- Make Lexical Edit refactorable and testable before replacing major UI surfaces.
- Use XML Parts/Layout as an import/compatibility contract during transition, not as the final runtime abstraction.
- Introduce typed view-definition and Presentation IR interfaces suitable for dependency injection, semantic parity tests, and Avalonia rendering.
- Preserve interaction behavior, information density, writing-system fonts, OpenType/HarfBuzz shaping behavior, nested structures, popup choosers, table views, and TreeView-heavy views.
- Decommission Graphite from the default Lexical Edit path: Graphite support work starts when the migration starts, Graphite is never supported in Avalonia, and Avalonia does not become the default screen until Graphite dependencies are gone or converted to OpenType/HarfBuzz-only behavior.
- Decommission C++ viewing/rendering dependencies by migrated region so completed Avalonia regions do not use native Views, `RootSite`, `IVwEnv`, `ManagedVwWindow`, or equivalent C++ display/layout/editor infrastructure at runtime. Custom linguistics services may remain when they are exposed through explicit service seams and do not own Avalonia viewing or editing surfaces.
- Extend render verification to capture semantic output, not only pixels and timings.

**Non-Goals:**
- No one-shot rewrite of DataTree, XMLViews, and Lexical Edit.
- No immediate XML deletion. XML retirement waits for migration tooling and parity gates.
- No global native Views deletion before all consumers are migrated or explicitly retained. During transition, native Views can remain for non-migrated regions and baseline comparison, but not inside a completed Avalonia region.
- No Graphite compatibility layer in Avalonia. Graphite-only fonts and feature strings are migration inputs to audit, warn, convert where possible, or replace; they are not runtime targets.
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

### 7. Graphite is decommissioned before Avalonia becomes default

**Decision:** Graphite support begins decommissioning at the start of the Lexical Edit Avalonia migration. Avalonia will use OpenType/HarfBuzz font features only; no Graphite runtime, compatibility shim, or fallback renderer is part of the default Avalonia Lexical Edit screen.

**Rationale:** Avalonia text rendering exposes OpenType/HarfBuzz-style `FontFeatures` and custom font/fallback hooks. Graphite GDL behavior is not an Avalonia target, and keeping a Graphite path would preserve exactly the native render dependency the migration is meant to remove.

**Research map:** The decommissioning scope includes native `Lib/src/graphite2`, `Src/views/lib/GraphiteEngine.*`, `RenderEngineFactory`, `GraphiteFontFeatures`, `FontFeaturesButton`, `DefaultFontsControl`, `FwWritingSystemSetupModel`, persisted writing-system flags/features such as `IsGraphiteEnabled` and `DefaultFontFeatures`, Graphite-specific tests/docs/sample fonts, `Build/Windows.targets` graphite2 targets, Gecko startup preference `gfx.font_rendering.graphite.enabled`, `XWebBrowser` preview consumers, and `GeckofxHtmlToPdf`/`FieldWorksPdfMaker` print/PDF assumptions.

**Feasibility:** Feasible, but intentionally disruptive for Graphite-only fonts. There is no automatic lossless Graphite-to-OpenType conversion. The migration must identify affected projects/fonts, provide replacement OpenType fonts or explicit user-facing compatibility warnings, and normalize any remaining feature settings to HarfBuzz/OpenType syntax before default switch.

**Alternatives considered:**
- Support Graphite in Avalonia: rejected because Graphite will never be supported in Avalonia.
- Keep Gecko only for Graphite previews/PDFs: rejected for the default Lexical Edit path because Gecko currently enables Graphite and would keep a parallel rendering story alive.

## Native Dependency Classification

The classification rule is based on the role of the native code, not the implementation language alone. If native code owns what the user is viewing or editing, it is not brought into completed Avalonia regions. If native code supplies custom linguistics capability that supports FieldWorks' role in documenting many languages, it may remain behind an explicit service seam.

- **Native Views layout/render/editing:** `VwRootBox`, `IVwEnv`, `IRenderEngine`, selections, hit testing, `OnTyping`, `OnExtendedKey`, table layout, interlinear layout, and RootSite editing are not mere windowing. They are the render/editor pipeline and remain a hard removal gate for migrated regions.
- **Custom linguistics services:** XAmple, spelling, parser/conversion engines, ICU, Encoding Converters, and similar language-documentation capabilities are allowed to remain in C++ or native/external form when invoked through managed service boundaries. Avalonia may consume their results, but it must not depend on their UI, rendering, or RootBox integration.
- **Spell-check interop:** `RootSite` wires `SetSpellingRepository(IGetSpellChecker)` into `VwRootBox`, while managed helpers build spelling context menus. Avalonia can keep spelling as a service, but any dependency on RootBox spell integration must be replaced for migrated regions.
- **Parser/conversion/native utility tools:** `pcpatr64.exe`, `TonePars64.exe`, `xample.dll`, Encoding Converter native files, ICU artifacts, Expat/ParserObject, and reg-free COM/proxy/stub build infrastructure are real native dependencies. They are not default Lexical Edit viewing dependencies, but migrated workflows that invoke them must wrap them as services and keep them outside Avalonia rendering/editor completion gates.

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
- Graphite decommissioning -> Begin the inventory at migration start and block Avalonia default until Graphite engines, feature UI/storage, sample fonts, Gecko Graphite prefs, PDF/export assumptions, and tests/docs are retired or converted to OpenType/HarfBuzz-only behavior.
- Gecko/browser rendering -> `XWebBrowser`, dictionary/configuration previews, interlinear configuration previews, print, and `GeckofxHtmlToPdf` need a non-Graphite replacement or an explicit non-default legacy boundary.
- PropertyGrid limits -> Treat it as a prototype path; do not let it define the final IR or UI shape.
- Automation flakiness -> Keep UIA2 tests thin; use model/semantic assertions for deep behavior.
- XML retirement too early -> Gate deletion on migration tooling, custom-field coverage, user overrides, ghost behavior, chooser parity, and fallback ability.
- C++ viewing/rendering removal exposes text/layout gaps -> Gate each region on dependency audits and replacement services for text shaping, selection, measurement, hit testing, scrolling, and printing/export behaviors where applicable. Do not count custom linguistics service calls as blockers unless they own UI viewing/editing behavior.

## Migration Plan

1. Freeze current behavior with targeted unit/integration/render/UIA2 baselines.
2. Introduce DI-friendly services around DataTree refresh, context, LCModel access, editor selection, and launcher logic.
3. Start Graphite decommissioning: inventory affected project settings, fonts, render engines, Gecko/PDF paths, tests, docs, and build artifacts.
4. Extend render verification with semantic snapshots and failure bundles.
5. Build typed view-definition and XML import as the compatibility compiler.
6. Replace simple controls and hover/popups in Avalonia using owned editor controls.
7. Replace table/browse views with virtualized Avalonia table/tree structures.
8. Replace slices and full Lexical Edit views with Avalonia surfaces over the typed contract.
9. Audit the migrated region's runtime call graph and remove/disable native viewing/rendering/editor dependencies for that region, while classifying custom linguistics engines as service seams when they do not own the Avalonia UI surface.
10. Add managed canonical view-definition authoring and migration tooling.
11. Retire runtime XML only after parity gates pass for production layouts, custom fields, and user overrides.

## Open Questions

1. Should the canonical post-XML view-definition format be C# builders, JSON/YAML, resources, database-backed project settings, or a hybrid?
2. Which shipped/sample/customer fonts and writing systems require replacement or migration because they depend on Graphite-only shaping or feature IDs?
3. Is `TreeDataGrid` acceptable for any Lexical Edit surface given package/licensing/version constraints, or should FieldWorks own all dense tree/table rows?
4. Which customer layout override fixtures should become mandatory migration tests?
5. Which non-Lexical Edit consumers keep native Views alive after Lexical Edit regions are migrated, and what is the repo-wide deletion plan once those consumers are addressed?
6. Which non-Gecko browser/PDF engine will own XHTML preview, print, and PDF behavior after Graphite and Geckofx are removed from the default path?
