# Avalonia Migration -- Design Overview

The front door to the WinForms->Avalonia migration. This document maps the whole
replacement design -- what each subsystem is, which WinForms/Views technology it
replaces, and where the key classes live -- and links to the deeper references
and playbooks. Read this first; go deeper only where your task needs it.

Audience: developers doing or reviewing migration work. The effort is
human-in-the-loop: agents and skills do the mechanical work, a human owns every
behavioral decision and manual checkpoint.

## 1. Coexistence architecture

FieldWorks is a net48 WinForms application that hosts Avalonia **in-process**.
Both frameworks run in the same process against the same LCModel cache; there
is no IPC and no second process.

- **Hosting**: Avalonia controls render inside WinForms via
  `Avalonia.Win32.Interoperability.WinFormsAvaloniaControlHost`. Our reusable
  host base is `Src/Common/FwAvalonia/AvaloniaHostControlBase.cs` (the
  generic in-process plumbing); the concrete detail-surface host is
  `Src/Common/FwAvalonia/DetailHostControl.cs`.
  Keyboard-navigation keys that WinForms would swallow are claimed by
  `Src/Common/FwAvalonia/InputKeyClaimingAvaloniaHost.cs`.
- **DPI**: the process is deliberately **DPI-unaware** (a WPF dialog once
  triggered DPI awareness and blurred the whole app). DPI awareness arrives
  only after all WinForms is gone; expect shimming until then.
- **Crash guard**: Avalonia's MicroCom COM proxies post native Releases through
  the captured SynchronizationContext from finalizers;
  `Src/Common/FwAvalonia/FinalizerSafeSynchronizationContext.cs` is installed
  before Avalonia initializes so a post after WinForms teardown cannot kill the
  process. Its assumptions are pinned by tests (`SeamTests`).
- **Opt-in gate**: the new UI is **fail-closed**. `Src/Common/FwUtils/UIModeGates.cs`
  (no Avalonia type in it) decides whether the New UI ("New UI (preview)" in
  Tools -> Options) is active; the `EditSurface*` family  -
  `EditSurfaceResolver`, `EditSurfaceRegistry`, `EditSurfaceSelectionService`,
  `EditSurfaceKind` (all under `Src/Common/FwAvalonia/`) -- resolves per tool
  whether the Avalonia surface or the legacy surface renders. Unregistered
  tools always get Legacy.

## 2. Subsystem map

Each subsystem below names what it replaces, its key classes, and the deeper
reference that owns the detail.

### 2a. Dialog kit -- replaces WinForms modal dialogs

An Avalonia dialog is an MVVM set (`*Input` / `*View.axaml` / `*ViewModel` /
`*Payload`/`*Result`), shown as a WinForms-owned modal so z-order, centering,
and focus-return match legacy behavior.

| Concern | Class |
| --- | --- |
| Modal presentation (owner, sizing, icon, focus save/restore) | `Src/Common/FwAvalonia/AvaloniaDialogHost.cs` |
| ViewModel base + validation gating | `Src/Common/FwAvaloniaDialogs/DialogViewModelBase.cs` |
| Theme/density (concrete brushes only -- DynamicResource fails headless) | `Src/Common/FwAvaloniaDialogs/DialogTheme.axaml`, `Src/Common/FwAvalonia/CompactDialogStyles.cs` |
| Message boxes | `Src/Common/FwAvaloniaDialogs/FwMessageBox.cs` |
| Launcher edges (legacy call sites -> Avalonia dialogs, LCModel in/out) | `Src/LexText/LexTextControls/Avalonia/Lcm*DialogLauncher.cs` |

Converted dialogs reuse the **legacy class stem + role suffix**
(`InsertEntryDlgViewModel` <-> legacy `InsertEntryDlg`); reusable kits keep
general names (`ChooserDialog*`, `EntryGo*`). See section 5 Terminology.

Process: `migrate-a-dialog.md` (the playbook). Mechanics:
`.claude/skills/fieldworks-avalonia-ui/references/dialog-conversion.md`.

### 2b. Detail pipeline -- replaces DataTree + Slice + native Views rendering

The detail-pane editor. Legacy `DataTree` walked layout XML at runtime and
built a tree of stateful `Slice` controls rendered by native Views; the detail
pipeline splits that into data -> view -> row controls:

```
shipped layout XML
  \-- ViewDefinitionCompiler (Src/Common/FwAvalonia/ViewDefinition/) -- typed view definition
       +-- DetailModelProjector (FwAvalonia/Detail) -- thin, LCModel-free projector
       \-- DetailComposer (Src/xWorks/Avalonia/Composer/) -- LCModel-backed, full entry
            -> ComposedDetail { DetailModel, edit context }
                 \-- DetailModel = ordered DetailFields (+ DetailFieldKind)
                      \-- DataTree (FwAvalonia/Detail) -- the view
                           \-- SliceFactory (FwAvalonia/Detail) -- kind -> Fw*Field row control
```

Shared rules keep the two projectors from drifting: `DetailStructureRules`,
`EditorKindMap`, `DetailValueFactory`/`IDetailValueProvider`.

Row controls (owned code, no native Views): `FwMultiWsTextField` (per-WS
typography, RTL, keyboards), `FwStructuredTextField` (multi-paragraph StText),
`FwChooserField`, `FwReferenceVectorField`, `FwOptionChooser`, `FwPosChooser`
(all under `Src/Common/FwAvalonia/Detail/`). Managed cluster/bidi text
handling lives in `DetailTextGraphemeClusters` /
`DetailBidirectionalTextNavigation` (replacing native Views hit-test and
selection semantics).

Fallback: if full composition throws, the Lexicon tool degrades to the fixed
three-field first slice (`LexiconEditErrorFallback`, `LexiconFirstSlice`) and
logs the failure.

Process: `migrate-a-slice-type.md` (the playbook). Deep rationale: section 4 below.

### 2c. Edit sessions -- replaces direct Views/RootSite editing

All detail edits stage through `IDetailEditContext`
(`Src/Common/FwAvalonia/Detail/`), implemented over
`DetailEditContextBase` (`Src/xWorks/Avalonia/`) and the fenced
`LcmDetailEditSession` (`Src/xWorks/Avalonia/Composer/`): one LCModel undo
task per save, on the same global action-handler stack legacy uses -- so Ctrl+Z
works across frameworks by construction. Specialized edit operations live on
**sub-capability interfaces** (e.g. `IStructuredTextEditing`), not by widening
the core interface. Before the window's save-on-tool-switch commit, the
outgoing surface settles its open session via `IPrepareToGoAway`
(`Src/xWorks/Avalonia/`) -- named for the legacy idiom, but unlike legacy it
cannot veto; it always settles.

### 2d. Seams -- the coexistence boundary

A **seam** (Feathers, *Working Effectively with Legacy Code*) is a
substitution point: an interface whose implementations -- the product one
driving legacy infrastructure, and the test/preview doubles (`Fake*`,
`InMemory*`, `ImmediateUiScheduler`) -- swap without editing a call site.
`Src/Common/FwAvalonia/Seams/` holds the six seams (`IEditSession`,
`IDetailRefreshCoordinator`, `IUiScheduler`, `IDetailLifetime`,
`IXCoreCommandBridge`, `IRecordNavigationContext`), their default
implementations, the boundary data contracts (`IFwClipboard`/`FwClipboardText`,
`FwDragDropFormats`/`FwRecordKeyPayload`), and the boundary policy
`ActiveHostContract` ("may I drive the legacy DataTree right now?",
audit-tested).

Clipboard and drag-and-drop interchange between the two surfaces uses the
Windows clipboard data format registered as `"TsString"`, carrying the
TsString XML representation (`TsStringUtils.GetXmlRep`) -- the same format
native-Views copy/paste uses, so multi-WS rich text round-trips in both
directions. `TsString` remains the rich-text data model on both surfaces;
the migration replaces rendering and editing surfaces, not the data model.

Catalog: `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/seam-catalog.md`.

### 2e. Custom-slice plugins -- replaces dynamically-loaded custom slices

A legacy custom slice (`editor="Custom" class=...` in layout XML) renders via
an `ISlicePlugin` keyed by that **legacy class name** -- zero layout
edits per migration (`Src/xWorks/Avalonia/Plugins/`).
Resolution is two steps: plugin -> labeled **Unsupported** row. The visible
Unsupported rows are the conversion worklist; nothing silently mis-renders.
`ReversalIndexEntryPlugin.cs` is the exemplar.

### 2f. Hosting into xWorks -- replaces RecordEditView's DataTree hosting

`Src/xWorks/Avalonia/Hosting/RecordEditView.Avalonia.cs` (a partial of the
legacy `RecordEditView`) shows the detail view on the Avalonia surface;
`AvaloniaDetailRefreshController` propagates cross-surface `PropChanged`
refreshes (suspend/pending while an edit session is open);
`XCoreMenuBridge` and `RecordClerkNavigationContext` bridge xCore commands and
record navigation.

### 2g. Preview host + testing -- how migrated UI is verified

Three verification surfaces, in the order a migrator reaches for them:

- **Headless tests** (`[AvaloniaTest]` in FwAvaloniaTests /
  FwAvaloniaDialogsTests) are the inner loop: prove field wiring, keyboard
  flows, validation, and edit/commit behavior without launching anything.
  Every playbook build step lands its evidence here first.
- **The preview host** (`Src/Common/FwAvaloniaPreviewHost/`) runs a surface
  standalone, outside FieldWorks: iterate on look and interaction without
  opening a project, and use it to isolate defects -- a bug that reproduces
  in the preview host is in our code, not in the WinForms hosting or
  coexistence plumbing.
- **Render snapshots** (`FwAvaloniaTests/Visual/`) pin appearance; update
  them deliberately when a change is supposed to look different. Mechanics:
  `.claude/skills/fieldworks-avalonia-ui/references/visual-snapshot-testing.md`.

What automation cannot cover: pointer/mouse interaction (headless runs no
hit-testing). That is exactly what the playbooks' scripted manual
checkpoints in live FieldWorks exist for -- they are part of the process,
not a fallback.

## 3. Legacy <-> new correspondence

| Legacy (WinForms/Views) | New (Avalonia) |
| --- | --- |
| layout XML walked at runtime | typed view definition (`ViewDefinitionCompiler`) |
| `DataTree` + `SliceFactory` composing XML state | `DetailComposer` / `DetailModelProjector` -> `DetailModel` |
| `Slice` -- the field's state/layout role | `DetailField` + `DetailFieldKind` |
| `Slice` -- the interactive row control | `Fw*Field` control built by `SliceFactory` |
| custom `Slice` subclass (`class=`) | `ISlicePlugin` (keyed by that class name) |
| native Views rendering/selection | Avalonia controls + managed cluster/bidi navigation |
| direct Views/RootSite edits | `IDetailEditContext` over one fenced undo task |
| `DataTree` the view control | `DataTree` |
| `DataTree.PrepareToGoAway()` | `IPrepareToGoAway` (cannot veto -- always settles) |
| WinForms modal dialogs | dialog kit, WinForms-owned via `AvaloniaDialogHost` |

## 4. DataTree and Slice: what changed from legacy

The new stack reuses the names DataTree and Slice deliberately -- but each
now covers only half of what its legacy namesake did. If you know the legacy
DetailControls, these are the assumptions to update:

| Legacy assumption | New reality |
| --- | --- |
| `DataTree` walks layout XML at runtime and builds live `Slice` controls on demand | Composition is a separate, testable step: `DetailComposer` (or the thin `DetailModelProjector`) produces a `DetailModel` snapshot; the view only renders it |
| A slice is a stateful control; new behavior means a new `Slice` subclass | A row is data (`DetailField` + `DetailFieldKind`) rendered by a factory-built `Fw*Field` control; new behavior means classifying to an existing kind, adding a kind + `SliceFactory` case, or writing a plugin -- never subclassing a row control (`migrate-a-slice-type.md`) |
| Slices write into LCModel as you type, via Views/RootSite | Rows STAGE edits through `IDetailEditContext`; a fenced session commits them as ONE undo step on focus loss, and validation gates the commit |
| Rows update in place when the data changes | Rows are a compose-time snapshot; changes arrive by re-compose/re-show through the refresh coordinator, which suspends while an edit session is open |
| A custom slice is a dynamically loaded class named in the layout's `class=` | The same `class=` key resolves to a plugin (`ISlicePlugin`); an unclaimed custom slice renders a labeled Unsupported worklist row -- never silently wrong |

The one-line version: state was pulled out of the rows, composition was
pulled out of the tree, and writes were pulled out of the keystroke path.
The familiar names survive on the halves they still fit.

## 5. Terminology

- **detail** -- the record-detail editing mode (the layout XML's own
  `type="detail"`, home of the legacy DetailControls). The new stack names
  its pieces from that word: `Detail*` for the projected data and its
  services, `DataTree` for the view, `Slice*` for the row factory and
  plugins. Adjacent but app-wide: `EditSurface*` names the choice of which
  framework renders a tool's surface.
- **"Lexicon"** only for genuinely Lexicon-Editor-area types
  (`LexiconFirstSlice`, `LexiconEditErrorFallback`, `LexiconFeature*`).
  Domain terms (`LexicalRelation`, `LexicalEntry`, `LexicalReference`, ...)
  are LCModel / linguistics concepts and keep their names.
- **Dialogs**: 1:1 conversions take the legacy stem + role suffix
  (`InsertEntryDlg*`, `AddNewSenseDlg*`, `MsaCreatorDlg*`, `LexOptionsDlg*`,
  `MSAGroupBox`); kits keep general names (`ChooserDialog*` <->
  SimpleListChooser family, `EntryGo*` <-> BaseGoDlg family,
  `FwFeatureStructureEditor`, `FwMultiWsTextField`, `FwPosChooser`).
- **Cross-namespace twins** are deliberate (`InsertEntryDlg`, `MSAGroupBox`,
  `SliceFactory`, `DataTree` exist in both a legacy namespace and the
  Avalonia set): disambiguate at the consumer with a `using` alias or full
  qualification; never rename the type to dodge the collision.
- **seam** -- a substitution point at the coexistence boundary (section 2d).
- **fenced edit session** -- a bracketed LCModel undo task: opened on the first
  staged edit, committed/cancelled as one undoable step.
- **worklist row** -- a labeled Unsupported row in the detail view: visible,
  honest, and the unit of remaining conversion work.
- **chooser** -- the FieldWorks word for pick-from-list/tree controls
  (`FwOptionChooser`, `FwPosChooser`, `ChooserDialog`); "picker" is not used.

## 6. Folder conventions

- **Dedicated Avalonia projects stay flat** with meaningful names
  (`Src/Common/FwAvalonia/` with thin topical subfolders `Detail/`, `Seams/`,
  `ViewDefinition/`, `Preview/`; `Src/Common/FwAvaloniaDialogs/` flat for
  dialogs, with a `Controls/` subfolder for dialog-owned composites).
- **The control/dialog layering mirrors legacy FwCoreDlgs/FwCoreDlgControls
  at the assembly boundary**: `FwAvalonia` is the controls layer,
  `FwAvaloniaDialogs` the dialogs layer. A new owned control lands in
  `FwAvaloniaDialogs/Controls/` when only dialogs use it, and sinks to
  `FwAvalonia` when the detail view or another surface needs it.
- **Avalonia injected into a legacy project is corralled under `Avalonia/`**:
  crosscutting files at that folder's root, a few coarse functional groups
  beneath -- `Src/xWorks/Avalonia/{Composer,Plugins,Hosting}`,
  `Src/LexText/LexTextControls/Avalonia/` (flat launchers).
- **Tests mirror their SUT's folders**: `Src/xWorks/xWorksTests/Avalonia/...`,
  `Src/LexText/LexTextControls/LexTextControlsTests/Avalonia/`.

## 7. Where to go next

| Task | Document |
| --- | --- |
| Convert a dialog | `migrate-a-dialog.md` |
| Convert a slice type / Unsupported row | `migrate-a-slice-type.md` |
| Find the right pattern/exemplar for a control | `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/control-exemplar-map.md` |
| Boundary contracts | `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/seam-catalog.md` |
| Dialog build mechanics | `.claude/skills/fieldworks-avalonia-ui/references/dialog-conversion.md` |
| Idiom & style rules | `.claude/skills/fieldworks-avalonia-ui/references/style-system.md`, `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/architecture-patterns.md` |
