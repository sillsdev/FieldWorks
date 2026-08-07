# Configure Columns (legacy `ColumnConfigureDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Controls.ColumnConfigureDialog` (`Src/Common/Controls/XMLViews/ColumnConfigureDialog.cs`) |
| **Area / tool** | Any browse view › column header context menu › "Choose Columns…" / "More Column Choices…" |
| **Primitive(s)** | dual-list reorder (available ↔ shown ListBoxes + Add/Remove/MoveUp/MoveDown) |
| **Canonical reference** | none of the kept canonicals is a dual-list; closest patterns: OptionsDialog (list + buttons), ChooserDialog (multi-select). Build fresh against the MVVM kit. |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/ConfigureColumnsDialogView.axaml(.cs)` + `ConfigureColumnsDialogViewModel.cs` (recover from git history on this branch) |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user choose which columns appear in a browse table, in what order. Opened from the
browse column-header menu. Returns the ordered set of visible column specs.

## What it looks like
<!-- CAPTURE: launch legacy FLEx (UIMode=Legacy), open a browse view, right-click a column
     header → "More Column Choices…", screenshot. See .claude/skills/fieldworks-winapp. -->
![Configure Columns – initial](./images/configure-columns-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Two lists: "available" (catalog, grouped) and "shown" (ordered).
- [ ] Add / Remove move items between lists; duplicates in "shown" are guarded.
- [ ] Move Up / Move Down reorder the "shown" list; disabled at ends.
- [ ] The last remaining column cannot be removed.
- [ ] OK returns the ordered visible set; Cancel discards.
- [ ] Some columns appear multiple times legitimately (e.g. per-WS) — not deduped by label.

## Migration gotchas
- The catalog comes from the browse view's column spec XML; ordering and WS-variants matter.
- The backed-out Avalonia stub implemented the dual-list + guards but was wired only for the
  `UIMode=New` browse path; verify it preserved multi-instance columns.
- Re-wiring must round-trip the same column-spec objects the legacy `BrowseViewer` consumes.

## Wiring
- Legacy invocation: browse column-header menu → `BrowseViewer` column-config command.
- Avalonia path before back-out: `Src/xWorks/RecordBrowseView.cs:726`
  (`OnConfigureColumnsRequested` instantiated `ConfigureColumnsDialogViewModel`/`View`).
- Re-wiring target: `RecordBrowseView.OnConfigureColumnsRequested` re-enters the Avalonia
  dialog behind `UIMode=New`; Legacy keeps `ColumnConfigureDialog`.
