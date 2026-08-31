# Control & Behavior → Exemplar Map

Lookup tables for migrators: you have a WinForms control or dialog behavior
in front of you — this file tells you which converted exemplar to copy, or
(§3) that none exists yet and what to do about it.

Usage signals come from a repository census (2026-07): stock-control counts
are `new System.Windows.Forms.<Type>(` instantiations in `*.Designer.cs`;
custom-control and behavior counts are files under `Src/` referencing the
symbol. They are point-in-time priority signals, not live numbers — re-run
the greps before citing them elsewhere. Baseline scale: ~164 Form-derived
dialog classes (no shared FwDialog base), ~97 UserControls, ~46
RootSite-derived views.

## 1. Control map

Stock WinForms controls first (trivial 1:1 maps are listed once, not
per-control), then the FieldWorks custom controls that carry the real
migration burden.

| WinForms control (usage signal) | Avalonia counterpart | Exemplar |
| --- | --- | --- |
| Button (374) / Label (315) / CheckBox (75) / RadioButton (51) | `Button` / `TextBlock` / `CheckBox` / `RadioButton` — 1:1; use the shared style tokens, never literals | any converted dialog view, e.g. `Src/Common/FwAvaloniaDialogs/EntryGoDialogView.axaml` |
| TextBox (116), plain string | `TextBox` | `Src/Common/FwAvaloniaDialogs/CreateFeatureDialogView.axaml` — but FIRST check whether the legacy box carries a writing system; if so it is NOT plain (see §2 "WS typography") |
| ComboBox (47), FwOverrideComboBox (22 files), FwComboBox (14 files) | `FwOptionChooser` (dialog rule: never ad-hoc ComboBox) | `Src/Common/FwAvalonia/Detail/FwOptionChooser.cs`; consumed in `InsertEntryDlgView.axaml` |
| ListBox (17) / CheckedListBox (11) | `ListBox`; multi-select with per-node checkboxes | `Src/Common/FwAvaloniaDialogs/ChooserDialogView.axaml` (flat + multi-select modes) |
| TreeView (4) + chooser dialogs | virtualizing `TreeView` + `TreeDataTemplate` | `ChooserDialogView.axaml` / `ChooserDialogViewModel.cs` (hierarchy, expand/collapse, filter-swaps-to-flat) |
| TabControl (6) | `TabControl`, two-way `SelectedTabIndex` | `Src/Common/FwAvaloniaDialogs/LexOptionsDlgView.axaml` |
| GroupBox (37) | headered composite control | `Src/Common/FwAvaloniaDialogs/MSAGroupBox.cs` |
| TableLayoutPanel (33) / FlowLayoutPanel (20) / Panel (40) | `Grid` / `StackPanel` / `WrapPanel` — translate layout *semantics*, not widget-for-widget | any converted dialog view; spacing rules in dialog-conversion.md §2a-bis |
| ToolTip (12) | `ToolTip.Tip` attached property | converted dialog views |
| ContextMenuStrip built in code (22 files) | `MenuFlyout` populated from data | `Src/Common/FwAvalonia/Detail/DetailMenuFlyout.cs` |
| PictureBox (21) | — (picture editing dropped from the region; a picture slice composes a labeled Unsupported row until a native picture editor is added) | **conversion worklist** (see architecture-patterns.md §5) |
| ProgressBar / ProgressDialogWithTask | — | **GAP:** [async / threaded progress (GAP-PROGRESS)](#gap-progress) |
| WizardDialog family | — | **GAP:** [wizard lifecycle (GAP-WIZARD)](#gap-wizard) |
| DataGridView (3) / BrowseViewer (28 files) / RecordBrowseView (19 files) | — | **DEFERRED:** [browse/table grid + bulk edit (GAP-BROWSE-GRID)](#gap-browse-grid) |

| FieldWorks custom control (files referencing) | Avalonia counterpart | Exemplar |
| --- | --- | --- |
| FwTextBox (41), LabeledMultiStringControl (5), MultiStringSlice (13) | `FwMultiWsTextField` — per-WS font, RTL, keyboard, abbreviation gutter, staged edits | `Src/Common/FwAvalonia/Detail/FwFieldControls.cs`; dialog usage: `InsertEntryDlgView.axaml` (lexeme form + gloss) |
| FwMultiParaTextBox (1) / StTextSlice | `FwStructuredTextField` (multi-paragraph StText) | `Src/Common/FwAvalonia/Detail/FwStructuredTextField.cs` |
| TreeCombo (27) + PopupTree/PopupTreeManager (36) | popup tree picker | `Src/Common/FwAvalonia/FwPosChooser.cs` |
| FeatureStructureTreeView (3) + `MsaInflectionFeatureListDlg` / `PhonologicalFeatureChooserDlg` | `FwFeatureStructureEditor` — LCModel-free `FsFeatStruc` tree editor: complex features expand to nested features, closed features to symbolic-value radios (one per feature) + a "None of the above" unspecified radio, plus inline create-feature / add-value affordances | `Src/Common/FwAvaloniaDialogs/FwFeatureStructureEditor.cs`; composed in `MSAGroupBox.cs` and the feature chooser (`FeatureChooserDialog*`) |
| SimpleListChooser (26) / ReallySimpleListChooser (22) | `ChooserDialogView`/`ViewModel` (input/result DTOs, single- and multi-select) | `Src/Common/FwAvaloniaDialogs/ChooserDialog*` |
| BaseGoDlg/EntryGoDlg family (Go, Merge, Link*, AddAllomorph); its embedded MatchingObjectsBrowser | `EntryGoDialogView`/`ViewModel` + per-consumer launcher; persistent multi-column matching list (column spec `EntryGoResultColumn`, arrow-key selection from the search box) | `Src/Common/FwAvaloniaDialogs/EntryGoDialog*`; matching list + dependent auxiliary picker: dialog-conversion.md §2c; WS-aware search box: §2d |
| MessageBox / one-off confirmation Forms | `FwMessageBox` (owner-parented, Yes/No/OK/Cancel) | `Src/Common/FwAvaloniaDialogs/FwMessageBox.cs`; injectable-seam usage: `LcmAddAllomorphDialogLauncher.PerformAddAllomorph` |
| DataTree + Slice subclasses (88/61) | detail view: composer → region model → owned field controls (Text / StructuredText / Chooser / ReferenceVector / Literal). Custom slices resolve plugin registry → labeled Unsupported row (the conversion worklist); the sole native plugin exemplar is `ReversalIndexEntryPlugin`. | `Src/xWorks/Avalonia/Composer/DetailComposer.cs`, `Src/Common/FwAvalonia/Detail/DataTree.cs`, `Src/xWorks/Avalonia/Plugins/SlicePlugins.cs`, `Src/xWorks/Avalonia/Plugins/ReversalIndexEntryPlugin.cs` (architecture-patterns.md §2, §5) |
| FwHelpButton (5) + per-dialog Help | ViewModel `HelpRequested` event → launcher calls `ShowHelp.ShowHelpTopic` | `EntryGoDialogViewModel.cs` + any `Lcm*Launcher` `OnHelpRequested` |
| TriStateTreeView (6) | — | **GAP:** [tri-state checkbox tree (GAP-TRISTATE)](#gap-tristate) |
| SimpleRootSite / RootSiteControl embedded views (72/26) | — | **DEFERRED:** [RootSite-embedded views (GAP-ROOTSITE-VIEWS)](#gap-rootsite-views) |

## 2. Behavior map

| Dialog behavior (usage signal) | Exemplar |
| --- | --- |
| Modal lifecycle: ShowDialog/DialogResult/owner parenting (170/334/73 files) | `Src/Common/FwAvalonia/AvaloniaDialogHost.cs` (`ShowModal`, `ResolveEffectiveOwner`, result mapping) |
| Window geometry persistence (44 files) | `AvaloniaDialogHost.ApplySizing` + size get/set hooks |
| Accept/Cancel + Enter/Escape (122 files) | `IsDefault`/`IsCancel` buttons in every converted dialog view |
| Commit-on-select vs explicit OK | EntryGo single-stage (no OK button) vs two-stage (dialog-conversion.md §2c); Chooser single-select |
| Dependent two-stage selection | dialog-conversion.md §2c (`EntryGoAuxiliaryOption`, LinkMSA/LinkAllomorph launchers) |
| WS typography + keyboard switch on text input (255 files set WS on controls) | editing: `FwMultiWsTextField`; search/entry boxes: `EntryGoSearchFieldSpec` + `EntryGoLauncherShared.BuildVernacularSearchFieldSpec` (dialog-conversion.md §2d) |
| RTL from writing system (99 files) | `FlowDirection` from `ws.RightToLeftScript` — `FwFieldControls.cs`, §2d spec |
| Fenced LCModel edits / one undo step | region: `IDetailEditContext`/`LcmDetailEditSession`; launcher on-OK: `UndoableUnitOfWorkHelper.Do` in `LcmInsertEntryDialogLauncher.Apply` |
| Validation gating + inline error display | gating: `DialogViewModelBase.GetValidationErrors`; the inline-display exemplar is `CreateFeatureDialogView.axaml` (`!IsValid` → visible message) — copy it; do not invent a new error surface |
| Confirmation prompt behind a testable seam | `LcmAddAllomorphDialogLauncher` (`Func<string,string,bool>` defaulting to `FwMessageBox`) |
| Help button (130 call sites, ~1/dialog) | ViewModel `HelpRequested` → launcher `ShowHelp.ShowHelpTopic(helpProvider, topic)` |
| Localization | `FwAvaloniaDialogsStrings` accessor + neutral resx (dialog-conversion.md §5) |
| Clipboard in text fields | `IFwClipboard` seam (`Src/Common/FwAvalonia/Seams/`) |
| Headless ViewModel/view tests | `FwAvaloniaDialogsTests/EntryGoDialogTests.cs` shapes; launcher-over-real-cache: `LcmLinkMsaDialogLauncherTests.cs` |

## 3. Gap register — the first implementation becomes the exemplar

These have **no exemplar**. Do not improvise a local pattern and do not
copy a WinForms design literally: the first migration that needs one of
these BUILDS it as the exemplar, shaped like the other converted dialogs (LCModel-free ViewModel/spec in
FwAvaloniaDialogs or FwAvalonia, launcher edge in the consuming assembly,
headless tests), and then PROMOTES it:

1. Move its row out of this register into the §1/§2 tables with file
   citations.
2. Add a numbered subsection to dialog-conversion.md (the §2c/§2d shape:
   what the legacy behavior was, the shared design, the test names).
3. Record any surprise in `lessons-learned.md` per its update
   protocol.

Until a gap's exemplar exists, conversions that need it stay on the Legacy
view -- that is what the fail-closed resolver is for.

<a id="gap-progress"></a>
### GAP-PROGRESS: Async / threaded progress (highest exposure)
- **Evidence:** `ProgressDialogWithTask`/`IThreadedProgress` in ~74 files;
  every import/export/backup flow.
- **First implementation must cover:** a cancellable background task with
  UI-thread marshaling, determinate and indeterminate progress, error
  propagation back to the caller, and modal gating (owner disabled while
  running). Acceptance: headless ViewModel tests for progress updates, cancel,
  and failure; no `Thread.Sleep`/polling in tests.
- **Likely first consumer:** an export dialog (`Src/xWorks/ExportDialog.cs`
  is the heaviest legacy user).

<a id="gap-wizard"></a>
### GAP-WIZARD: Wizard lifecycle
- **Evidence:** 3 `WizardDialog` subclasses (`LexImportWizard`,
  `NotebookImportWiz`, `InterlinearSfmImportWizard`); NotebookImportWiz
  alone has 17 buttons / 31 labels.
- **First implementation must cover:** Next/Back/Finish state machine,
  per-page validation gating Next, page-visited state, Cancel-with-dirty
  confirmation. Prefer a small shared base (`WizardViewModelBase`) over
  per-wizard copies — three known consumers justify it.
- **Likely first consumer:** the smallest wizard, not NotebookImportWiz.

<a id="gap-ws-selector"></a>
### GAP-WS-SELECTOR: Writing-system selector on search fields
- **Evidence:** legacy `BaseGoDlg.m_cbWritingSystems` lets the user choose
  which WS to search; the §2d spec carries only the default vernacular WS
  (`EntryGoLauncherShared.BuildVernacularSearchFieldSpec`).
- **First implementation must cover:** extending `EntryGoSearchFieldSpec`
  with a WS option list + selection callback that re-runs the search and
  re-derives typography/keyboard.

<a id="gap-tristate"></a>
### GAP-TRISTATE: Tri-state checkbox tree
- **Evidence:** `TriStateTreeView` in 6 files (export/filter dialogs).
- **First implementation must cover:** parent/child check propagation and
  the indeterminate state on `ChooserTreeNode`-style rows; extend the
  `ChooserDialogView`/`ViewModel` rather than building a parallel tree.

<a id="gap-f1-help"></a>
### GAP-F1-HELP: Per-control F1 help strings
- **Evidence:** `HelpProvider.SetHelpString` is high-volume in Designer
  files; distinct from the (covered) per-dialog Help button.
- **First implementation must cover:** decide the Avalonia F1 story
  (tooltip vs help pane) once a migrated dialog actually loses per-control
  help a user depends on; do not pre-build.

<a id="gap-browse-grid"></a>
### GAP-BROWSE-GRID: Browse/table grid + bulk edit (deferred by design)
- The browse table (BrowseViewer/RecordBrowseView/XMLViews grid, filter
  bar, bulk edit) is intentionally absent; browse tools stay Legacy via
  the fail-closed resolver. Do not hand-roll ad-hoc `DataGrid` usage — a
  grid exemplar must come with virtualization, clerk integration, and
  parity evidence in its own migration.

<a id="gap-rootsite-views"></a>
### GAP-ROOTSITE-VIEWS: RootSite-embedded views (deferred by design)
- `SimpleRootSite`/`RootSiteControl` views (interlinear, rule formula,
  print previews) stay Legacy; their tools are deliberately unregistered
  (see `UIFrameworkRegistry`). The activation recipe lives in
  SKILL.md "Inert follow-up tools".
