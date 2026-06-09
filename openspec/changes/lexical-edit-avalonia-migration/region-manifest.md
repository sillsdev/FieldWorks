# Avalonia Migration Region Manifest

The manifest is the contract for what a migrated Lexical Edit region owns, what legacy services it may adapt, and what dependencies are forbidden from the new default path. It is not implemented yet; Phase 3 must introduce it behind a default-off switch and executable audits.

## 1. Manifest Shape

Each migrated region should declare:

| Field | Meaning |
|---|---|
| `regionId` | Stable identifier such as `lexical-edit.entry.identity`. |
| `ownerProject` | Owning project/module, for this change `AdvancedEntry.Avalonia`. |
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
  "ownerProject": "AdvancedEntry.Avalonia",
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

## 5. Provisional Performance Budgets

Budgets are placeholders until measured. They must not be used as pass/fail claims until each has fixture ID, machine profile, command, and artifact path.

| Metric | Provisional Target | Notes |
|---|---|---|
| First region load | Within 20 percent of legacy baseline or explicitly accepted | Measure cold and warm separately. |
| Layout compile | Deterministic and cacheable; target under 250 ms for selected first-slice fixture | Use immutable config snapshots. |
| Save/cancel command latency | No user-visible freeze for first editable slice | Measure UI-thread work and background work separately. |
| Validation pass | Linear in materialized node count | Lazy/unmaterialized sequences must be skipped or explicitly loaded. |

## 6. Phasing

| Phase | Manifest Work |
|---|---|
| Phase 1 | Define manifest schema, allowed/forbidden dependency policy, and audit command design. |
| Phase 2 | Attach current coverage report and identify blocked gates. |
| Phase 3 | Introduce default-off region manifest and symbol audit in tests or agent scripts. |
| Phase 4-6 | Add region evidence as typed layout import, edit sessions, validation, commands, and focus mature. |
| Phase 7-8 | Run accessibility/performance/rendering evidence against candidate default regions. |
| Phase 9 | Enable a region by default only when all gates pass and rollback remains available. |