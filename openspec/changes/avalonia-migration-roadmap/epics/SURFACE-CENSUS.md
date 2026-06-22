# FieldWorks UI surface census (living artifact)

> Stage 2.2 deliverable: the single inventory the migration tracks burn-down against, and the backstop
> Stage 8's "straggler sweep" reconciles against. Update the **State** column as surfaces migrate. State
> values: `legacy` (WinForms only), `coexist` (Avalonia behind the flag), `migrated` (Avalonia default),
> `retired` (legacy deleted). This is a tracking artifact, not a spec — the per-surface plans live in the
> stage epics and `reviews/`.

## Shell & framework
| Surface | Project / key files | Stage | State |
| --- | --- | --- | --- |
| App lifetime / startup | `Src/Common/FieldWorks/FieldWorks.cs`, `Src/Common/Framework/` | 11a | legacy |
| Main window | `Src/XCore/xWindow.cs` (2,498 lines, `: Form`), `Src/xWorks/FwXWindow.cs` | 11a | legacy |
| Mediator / PropertyTable | `Src/XCore/xCoreInterfaces/` | 11c (bridge: 2.3) | legacy (bridge seam exists) |
| Sidebar / navigation | `Src/XCore/SilSidePane/`, OutlookBar | 11d | legacy |
| Panes / splitters | `Src/XCore/CollapsingSplitContainer.cs`, `MultiPane.cs`, `PaneBarContainer.cs` | 11d | legacy |
| Menus / toolbars / status | `Src/XCore/FlexUIAdapter/`, `Inventory.cs`, `Main.xml` | 11b/11f | legacy |
| Surface selection switch | `Src/Common/FwAvalonia/LexicalEditSurfaceResolver.cs` + `LexicalEditSurfaceRegistry.cs` | 2.2 | **registry done** |
| Coexistence host bridge | `Src/Common/FwAvalonia/LexicalEditHostControl.cs` | 2.1 | coexist (lexical-edit only; generalization open) |

## Detail / browse frameworks
| Surface | Project / key files | Stage | State |
| --- | --- | --- | --- |
| DataTree / slices | `Src/Common/Controls/DetailControls/` (73 files) | superseded by region path | legacy (frozen) |
| Lexical/Advanced Entry region | `Src/xWorks/FullEntryRegionComposer.cs`, `Src/Common/FwAvalonia/Region/` | 4 | coexist (manifest §6 Partial) |
| XMLViews browse / bulk-edit | `Src/Common/Controls/XMLViews/` (`BrowseViewer`, `BulkEditBar`, `FilterBar`) | 3 | legacy (Avalonia table unbuilt) |
| Owned field controls | `Src/Common/FwAvalonia/Region/FwFieldControls.cs`, `FwOptionPicker.cs` | done | coexist |
| View-definition IR + override system | `Src/Common/FwAvalonia/ViewDefinition/` (importer, compiler, JSON, **override differ/applier/migrator/loader**) | 9.2/9.4 | **pure-logic core done** |

## Tool areas (RecordEditView/BrowseView/DocView consumers)
| Area | Project | Stage | State |
| --- | --- | --- | --- |
| Lexicon edit | `Src/LexText/Lexicon/`, `LexTextControls/` (~132 files) | 4/6a | coexist (entry); rest legacy |
| Grammar / Morphology | `Src/LexText/Morphology/`, `Src/FdoUi/` editors | 6b (→9), 6c (→8) | legacy |
| Texts & Words / Interlinear | `Src/LexText/Interlinear/` (~98 files), Sandbox | 7A (→9) | legacy |
| Discourse / constituent charts | `Src/LexText/Discourse/` | 7B | legacy |
| Notebook / Lists / bulk-edit | `Src/xWorks/`, `Src/LexText/` | 8a | legacy |
| Dictionary configuration | `Src/xWorks/` `DictionaryConfiguration*`, `DictionaryDetailsView/` | 8b | legacy (MVP-ready) |

## Native UI / rendering (decommission targets)
| Component | Path | Stage | State |
| --- | --- | --- | --- |
| Views engine (VwRootBox/VwSelection/VwTextBoxes) | `Src/views/` | 9 (replace) / 13 (delete) | legacy |
| Managed Views host | `Src/Common/SimpleRootSite/`, `Src/Common/RootSite/` | 9/13 | legacy |
| Buffered draw | `Src/ManagedVwDrawRootBuffered/` | 13 | legacy |
| Gecko/PDF preview | `GeckoWebBrowser`, `GeckofxHtmlToPdf` (dictionary preview/export) | 10A | legacy |
| Graphite engine | `GraphiteEngineClass`, `Src/views/lib/GraphiteEngine.*` | 10B (path) / 13 (delete) | legacy — **removal accepted (doc+notify Awami Nastaliq)** |

## Dialogs & choosers
| Group | Project | Stage | State |
| --- | --- | --- | --- |
| Core dialogs (~30+) | `Src/FwCoreDlgs/`, `FwCoreDlgControls/`, `BackupRestore/` | 5A/5B | legacy |
| Views-coupled dialogs (Find/Replace, Styles) | `FwFindReplaceDlg`, `FwStylesDlg` (host `IVwRootSite`) | 5C → 9 | legacy |
| Domain dialogs / launchers | `xWorks`, `LexText`, `FdoUi` | with owning area | legacy |

> ~200 dialogs total (~123 Forms + ~78 UserControls). FwCoreDlgs: ~46 Designer.cs.

## How to keep this current
- When a surface changes State, update its row and add the proof (manifest/PR link).
- Stage 8's straggler sweep = "every row is `migrated`/`retired`, or has a named owning stage." Any WinForms
  surface not in this table is a census gap — add it.
