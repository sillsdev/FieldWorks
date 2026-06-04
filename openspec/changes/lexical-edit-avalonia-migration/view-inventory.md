# Lexical Edit View Inventory

This inventory maps the current standard Lexical Edit stack and the AdvancedEntry Avalonia spike. It is intentionally source-backed: proposed seams are called out as proposed, not as existing implementation. AdvancedEntry prototype implementation files are split to `010-advanced-entry-preview-prototype`; this foundation branch keeps only the inventory and seam expectations.

## 1. Legacy Edit Host

| Component | Source | Role | Migration Risk |
|---|---|---|---|
| `RecordEditView` | [Src/xWorks/RecordEditView.cs](Src/xWorks/RecordEditView.cs) | xWorks edit pane that hosts the detail tree, participates in mediator/property-table routing, and handles refresh/selection context. | Shell integration, XCore commands, focus ownership, and record navigation must remain stable while a migrated region is hosted. |
| `DataTree` | [Src/Common/Controls/DetailControls/DataTree.cs](Src/Common/Controls/DetailControls/DataTree.cs) | WinForms slice host that expands XML layouts into row controls, listens for `PropChanged`, manages refresh, selection, scroll, and slice lifecycle. | High. It combines layout interpretation, refresh coordination, focus, WinForms controls, and LCModel notifications. |
| `Slice` and subclasses | [Src/Common/Controls/DetailControls/Slice.cs](Src/Common/Controls/DetailControls/Slice.cs) and sibling files | Base row abstraction for editors, launchers, labels, accessibility names, and chooser launch routing. | High. Editor realization and launcher behavior are distributed across many subclasses. |
| `SliceFactory` | [Src/Common/Controls/DetailControls/SliceFactory.cs](Src/Common/Controls/DetailControls/SliceFactory.cs) | Static factory that maps XML part/editor attributes and field metadata to concrete legacy slices. | Registry extraction must preserve fallback diagnostics, custom editors, and reuse-map behavior. |

## 2. Legacy Layout and Override Sources

| Component | Source | Role | Migration Risk |
|---|---|---|---|
| Parts/layout XML | `DistFiles/Language Explorer/Configuration/Parts` | Shipped `.fwlayout` and `*Parts.xml` definitions for LexEntry, LexSense, Morphology, lists, and custom field placeholders. | Typed IR import must preserve labels, field/flid binding, visibility, ghost behavior, writing-system metadata, and custom-field insertion. |
| `Inventory` / layout cache usage | `LayoutCache`, `Inventory`, and xWorks/XMLViews callers | Runtime merge and lookup of shipped and project-specific layout definitions. | Project override precedence and conflict resolution must be tested before XML retirement. |
| Dictionary/reversal configs | [Src/xWorks/DictionaryConfigurationMigrator.cs](Src/xWorks/DictionaryConfigurationMigrator.cs) and `DictionaryConfigurationMigrators` | Migrates legacy dictionary/reversal models and preserves user customizations. | Existing migration behavior is broad and customer-sensitive; selected fixtures must be carried into typed-definition parity tests. |
| CSS overrides | [Src/xWorks/CssGenerator.cs](Src/xWorks/CssGenerator.cs) | Generates and locates `ProjectDictionaryOverrides.css` / `ProjectReversalOverrides.css` for legacy preview/export styling. | Decide whether migrated Lexical Edit ignores, translates, or leaves CSS to legacy preview/export paths. |

## 3. Browse and XMLViews Table Surfaces

| Component | Source | Role | Migration Risk |
|---|---|---|---|
| `RecordBrowseView` | [Src/xWorks/RecordBrowseView.cs](Src/xWorks/RecordBrowseView.cs) | xWorks wrapper for browse/table views next to edit views. | Shell and list selection interactions can affect edit-view navigation. |
| `BrowseViewer` | [Src/Common/Controls/XMLViews/BrowseViewer.cs](Src/Common/Controls/XMLViews/BrowseViewer.cs) | Tabular browse controller for columns, filters, sorts, and selection. | Requires semantic and UI automation baselines before replacing with Avalonia table controls. |
| `XmlView` | [Src/Common/Controls/XMLViews/XmlView.cs](Src/Common/Controls/XMLViews/XmlView.cs) | Native Views-backed XML rendering site for table and preview surfaces. | Native Views dependency must be classified as baseline-only or blocker for any migrated default path. |

## 4. Choosers and Launchers

| Component | Source | Role | Migration Risk |
|---|---|---|---|
| `ReallySimpleListChooser` / `LeafChooser` | [Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs](Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs) | Modal chooser forms for flat and hierarchical selections. | Needs a testable chooser model and dialog service before Avalonia popup parity can be claimed. |
| `ChooserCommand` | [Src/Common/Controls/XMLViews/ChooserCommandBase.cs](Src/Common/Controls/XMLViews/ChooserCommandBase.cs) | Encapsulates chooser commit behavior. | Transaction and rollback semantics must be characterized. |
| `MorphTypeAtomicLauncher` | [Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs](Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs) | Complex morph-type swap workflow, data-loss prompts, form swaps, MSA replacement, refresh/focus side effects. | First humble-object extraction target; modal prompts currently block full pure-logic coverage. |

## 5. AdvancedEntry Avalonia Spike

| Component | Source | Current Role | Gaps Before Production Migration |
|---|---|---|---|
| Project root | Split branch | net8 Avalonia module and preview target. | Must stay preview-host friendly and detached from full app shell until shell migration. |
| Presentation IR | Split branch | Immutable node model for fields, objects, sequences, sections, visibility, and ghost metadata. | Needs first-class editor kind, writing-system metadata, stable accessibility IDs, and class/flid/object binding for full semantic normalization. |
| Layout compiler | Split branch | Compiles resolved XML layout contracts into Presentation IR. | Needs override fixtures, unsupported-construct diagnostics, cache invalidation, cancellation, and immutable metadata snapshots. |
| Parts loader | Split branch | Loads shipped parts/layout XML from selected directories. | Must consume merged default + project override inputs before XML retirement. |
| Edit session | Split branch | Prototype fenced LCModel undo-task session with `Save` and `Cancel`. | Docs and tests must not assume staged draft semantics until that implementation exists in the product migration path. |
| Property-grid prototype | Split branch | First-slice candidate for descriptors, lazy sequences, and staged views. | Needs accessibility, localization, focus, keyboard/IME, and validation presentation gates before production editing. |

## 6. Hidden Dependency Checklist

Before declaring any migrated Lexical Edit region default-ready, search and/or instrument for these dependencies:

- WinForms controls: `DataTree`, `Slice`, `ViewSlice`, `RootSiteControl`, `BrowseViewer`, `XmlView`.
- Native Views/rendering: `IVwRootBox`, `IVwEnv`, `IVwGraphics`, `IRenderEngine`, `GraphiteEngineClass`, `UniscribeEngineClass`.
- Browser/PDF preview/export: `GeckoWebBrowser`, `XWebBrowser`, `GeckofxHtmlToPdf`, `FieldWorksPdfMaker`, PDF `--graphite` flags.
- Global COM/registration: FieldWorks must preserve registration-free COM; no migrated path may add global COM registration or registry hacks.
- Writing systems and fonts: `IsGraphiteEnabled`, `DefaultFontFeatures`, `FontEngines.Graphite`, Graphite-only font feature settings, custom font fallback.

## 7. Inventory Acceptance Criteria

An inventory entry is trustworthy only when it has:

1. A source path that exists in the current branch, or an explicit split-branch marker when the source has been moved out of this branch.
2. Current/proposed status clearly marked.
3. Known callers or consumers searched when the surface is structural.
4. Tests or planned tests listed by behavior, not only by file name.
5. A risk classification: baseline-only, first-slice blocker, shell-phase blocker, or repo-wide cleanup.