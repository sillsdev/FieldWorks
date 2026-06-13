# Avalonia Migration Region Manifest

The manifest is the contract for what a migrated Lexical Edit region owns, what legacy services it may adapt, and what dependencies are forbidden from the new default path. It is not implemented yet; Phase 3 must introduce it behind a default-off switch and executable audits.

> **Branch note (2026-06-09).** The owning project is **`SIL.FieldWorks.Common.FwAvalonia`**
> (`Src/Common/FwAvalonia`). `AdvancedEntry.Avalonia` exists only on the prototype branch
> `010-advanced-entry-preview-prototype` and is a reference implementation, not shipped code. The
> **active-host contract** referenced by `forbiddenSymbols` (`DataTree`, `Slice`, …) is now partially
> enforced: see `ActiveHostContract` in `FwAvalonia/Seams` and the `RecordEditView` audit test
> (`RecordEditViewActiveHostContractTests`) proving the active Avalonia path does not drive a hidden
> legacy `DataTree`.

## 1. Manifest Shape

Each migrated region should declare:

| Field | Meaning |
|---|---|
| `regionId` | Stable identifier such as `lexical-edit.entry.identity`. |
| `ownerProject` | Owning project/module, for this change `FwAvalonia` (`SIL.FieldWorks.Common.FwAvalonia`). |
| `legacySurface` | Legacy host/slice/layout being replaced or wrapped. |
| `uiModeBehavior` | For each app-wide UI mode, declares whether this host is supported, explicitly falls back to legacy, or is blocked with a product-facing diagnostic. |
| `enabledByDefault` | `false` until all gates pass for that region. |
| `rollbackSurface` | Legacy view or command used when the migrated region is disabled or fails capability checks. |
| `allowedAdapters` | Narrow legacy services the region may call. |
| `forbiddenSymbols` | Symbols/packages prohibited from production default-path code. |
| `requiredCapabilities` | Rendering, IME, accessibility, validation, undo/redo, localization, and layout override capabilities required for enablement. |
| `testEvidence` | Test files, fixture IDs, audit commands, and last known result. |

Example draft:

```json
{
  "regionId": "lexical-edit.entry.identity",
  "ownerProject": "FwAvalonia",
  "legacySurface": "LexEntry-detail-Normal identity fields in DataTree",
  "uiModeBehavior": {
    "Legacy": "legacy-active",
    "New": "supported-avalonia"
  },
  "enabledByDefault": false,
  "rollbackSurface": "RecordEditView/DataTree",
  "allowedAdapters": [
    "LcmCache main-thread read/write through approved edit session",
    "metadata cache immutable snapshots",
    "XCore command bridge in shell phase only",
    "FieldWorks diagnostics/logging"
  ],
  "forbiddenSymbols": [
    "System.Windows.Forms.Control",
    "DataTree",
    "Slice",
    "RootSiteControl",
    "XmlView",
    "BrowseViewer",
    "IVwRootBox",
    "IVwEnv",
    "IRenderEngine",
    "GraphiteEngineClass",
    "UniscribeEngineClass",
    "GeckoWebBrowser",
    "XWebBrowser"
  ],
  "requiredCapabilities": [
    "undo-redo",
    "validation",
    "keyboard-focus",
    "accessibility-metadata",
    "localized-strings",
    "layout-overrides",
    "writing-system-fonts"
  ]
}
```

## 2. Allowed Legacy Adapters

Adapters must be explicit and testable.

| Adapter | Rule |
|---|---|
| `LcmCache` / LCModel | Allowed only through main-thread edit sessions or immutable snapshots. Do not read mutable cache objects on background threads. |
| Metadata cache | Allowed for class/flid/type lookup; snapshot values before background compilation. |
| Undo/redo | Must route through LCModel action handler semantics; local Avalonia commands cannot bypass global undo history. |
| XCore mediator/property table | Shell-phase adapter only. First-slice preview code must stay decoupled. |
| Diagnostics | Use FieldWorks `System.Diagnostics`/trace-switch pipeline. |
| Localization | User-visible production strings must use resource patterns; test-only strings can remain in tests. |

## 3. Forbidden Default-Path Dependencies

The manifest audit must fail migrated production code that directly references:

- WinForms controls or hosts: `System.Windows.Forms`, `DataTree`, `Slice`, `RootSiteControl`, `RecordEditView` internals.
- XMLViews/native Views rendering: `XmlView`, `BrowseViewer`, `IVwRootBox`, `IVwEnv`, `IVwGraphics`.
- Native render engines: `IRenderEngine`, `IRenderEngineFactory`, `GraphiteEngineClass`, `UniscribeEngineClass`, `FwGrEngine`.
- Browser/PDF preview engines: `GeckoWebBrowser`, `XWebBrowser`, `GeckofxHtmlToPdf`, `FieldWorksPdfMaker`.
- Global COM registration, registry hacks, or direct native-boundary calls with unsanitized input.

Exceptions must be documented in the manifest with owner, reason, tests, and rollback behavior.

## 4. Gates

| Gate | Required Evidence |
|---|---|
| Switch contract gate | Every affected host declares `uiModeBehavior` for both app-wide UI modes, and no host relies on ambiguous best-effort routing. |
| Schema gate | Manifest validates against a checked-in schema and has an owner/rollback/test evidence entry. |
| Symbol audit gate | Automated search over migrated production code finds no forbidden symbols except approved exceptions. |
| Layout gate | Typed presentation snapshot matches selected DataTree/XML layout baselines for the region. |
| Edit gate | Save, cancel, nested session rejection, undo/redo, and refresh interaction tests pass. |
| Validation gate | Required fields, deterministic order, localized message metadata, severity, async stale-result handling, and accessibility exposure pass. |
| Accessibility gate | Controls expose stable automation IDs/names/roles where Avalonia supports them; keyboard-only navigation has headless or UI automation evidence. |
| Rendering gate | Writing-system/font/Graphite capability matrix is classified and default path blocks unsupported cases with rollback. |
| Performance gate | Provisional budgets are measured against named fixtures and hardware before becoming enablement criteria. |

## 5. Performance Budgets

### 5.1 Measured legacy DataTree baselines (task 2.13, 2026-06-09)

Measured via the existing characterization harness (`DataTreeRenderTests`/`DataTreeRenderHarness`), which times real `DataTree` initialization (mediator, inventories, form) and `ShowObject` slice population per fixture.

- **Machine profile:** 12th Gen Intel Core i7-12700, 64 GB RAM, Windows 11 Pro, 96 DPI (100%), Debug build.
- **Command:** `dotnet test Src/Common/Controls/DetailControls/DetailControlsTests/DetailControlsTests.csproj -c Debug --filter "FullyQualifiedName~DataTreeRenderTests"` with `FW_REPORT_TIMING_BASELINES=1`.
- **Artifacts:** raw per-run timings in `Output/RenderBenchmarks/datatree-timings.json`; enforced local thresholds (measured + ~50% headroom) committed as `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTimingBaselines.json`, checked by `DataTreeTimingBaselineCatalog` on every render-test run.

Representative measured numbers (Init / Populate / Total ms):

| Fixture (scenario id) | Slices | Init | Populate | Total |
|---|---|---|---|---|
| `simple` (LexEntry, 3 senses) | 14 | 162 | 47 | 358 |
| `multiws` (multi-WS alternatives) | 12 | 138 | 72 | 343 |
| `subsubsub-hidden-productionlike` (depth-4 production-like lexeme edit) | 68 | 77 | 26 | 864 |
| `timing-extreme` (depth-6, large fixture) | 253 | 66 | 93 | 2483 |

### 5.2 Avalonia budgets derived from the baselines

The Avalonia surface for an equivalent fixture must come in **within 20% of the legacy Total** above (or the delta is explicitly accepted in the manifest). Cold vs warm runs measured separately.

| Metric | Target | Notes |
|---|---|---|
| First region load | Within 20% of the measured legacy Total for the matching fixture | E.g. production-like depth-4 ≤ ~1040 ms on the profile above. |
| Layout compile | Deterministic and cacheable; under 250 ms for the first-slice fixture | Use immutable config snapshots (`ViewDefinitionCompiler` caches by fingerprint). |
| Save/cancel command latency | No user-visible freeze for first editable slice | Measure UI-thread work and background work separately. |
| Validation pass | Linear in materialized node count | Lazy/unmaterialized sequences must be skipped or explicitly loaded. |

### 5.3 Refresh-after-edit (measured 2026-06-09)

`DataTreeReshowTimingTests` measures the live-tree refresh path (`DataTree.RefreshList(false)` — the
slice-reuse rebuild legacy refresh drives; a same-root `ShowObject` early-outs at `DataTree.cs:1073`):
**5.6 ms for a 5-slice entry** after a citation-form edit (same machine profile as §5.1; artifact
key `timing-reshow-after-edit` in `datatree-timings.json`). Avalonia budget: region re-resolve +
re-show after an external edit within 20% of the matching fixture's legacy refresh.

### 5.4 Still unmeasured (open)

Scroll/expand latency and typing latency need dedicated harness scenarios; 150% DPI numbers need a
non-headless run on a scaled display. These remain open under task 2.13 and must be measured before
the corresponding gates become pass/fail claims.

## 6. Gate Evaluation — Lexical Edit region (2026-06-09, tasks 7.4/7.5)

The full-replacement gate is **established and enforced** (the UI mode defaults to Legacy until it
passes) and was evaluated against the composed full-entry view:

| Gate | Status | Evidence |
|---|---|---|
| Switch contract | **Pass** | `LexicalEditSurfaceSelectionService` + `RecordEditViewSwitchTests` (every consumer declared, both modes) |
| Symbol audit | **Pass** | `EngineIsolationAuditTests` + `RecordEditViewActiveHostContractTests` |
| Layout gate (semantic) | **Partial** | Typed snapshots deterministic; full-tree semantic comparison vs legacy DataTree output for the composed view still to run |
| Edit gate | **Pass** (current scope) | Fenced session save/cancel/one-global-undo-step/refresh interaction (`LexicalEditRegionEditingTests`, `FullEntryRegionComposerTests`) |
| Validation gate | **Partial** | Required-field rule + deterministic localized messages; severity/async lanes pend richer rules |
| Accessibility gate | **Partial** | Stable ids everywhere; UIA names/order parity proven on the realized surface (`PreviewHostUiaTests`); keyboard-traversal assistive smoke pends chooser-dialog work |
| Rendering gate | **Pass** (policy) | Per `graphite-transition-support`: classification+warning coverage replaces the block; native-engine audit green |
| Performance gate | **Partial** | Open + refresh-after-edit measured with enforced thresholds; scroll/typing/memory lanes open (§5.4) |
| **Verdict** | **Default stays Legacy.** | Core parity items (entry identity, citation, morph type, senses/glosses/definitions structure, ifdata hiding, editing) are composed and proven; the gate blocks default until the Partial rows close — exactly what 7.5 requires. |

P0 parity status (7.4): lexeme form ✔ editable multi-WS · citation form ✔ · morph type ✔ chooser ·
senses structure ✔ headers/indent per sense · gloss ✔ per-sense editable · definition ✔ (ifdata) ·
bibliography/restrictions/etc ✔ via metadata walk · variants/complex-form sections ✔ structure
(reference rows read-only) · custom fields ✘ (9.x B1) · ghost lines ✘ (B2) · rich TsString runs ✘
(6.13 gate, now tracked in `openspec/changes/avalonia-multi-writing-system-text-foundation/`).

## 7. Phasing

| Phase | Manifest Work |
|---|---|
| Phase 1 | Define manifest schema, allowed/forbidden dependency policy, and audit command design. |
| Phase 2 | Attach current coverage report and identify blocked gates. |
| Phase 3 | Introduce default-off region manifest and symbol audit in tests or agent scripts. |
| Phase 4-6 | Add region evidence as typed layout import, edit sessions, validation, commands, and focus mature. |
| Phase 7-8 | Run accessibility/performance/rendering evidence against candidate default regions. |
| Phase 9 | Enable a region by default only when all gates pass and rollback remains available. |