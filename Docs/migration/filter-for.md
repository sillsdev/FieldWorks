# Filter For (legacy `SimpleMatchDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Controls.SimpleMatchDlg` (`Src/Common/Controls/XMLViews/SimpleMatchDlg.cs`) |
| **Area / tool** | Any browse view › column header filter › "Filter for…" |
| **Primitive(s)** | plain-form (text + position radios + regex/case checkboxes) |
| **Canonical reference** | OptionsDialog (closest kept canonical for a small plain-form with controls) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/FilterForDialogView.axaml(.cs)` + `FilterForDialogViewModel.cs` (+ `FilterForPattern.cs`) @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user enter a text match for a browse-view column filter: the match string plus where it must
match (anywhere / start / end / whole item), with regex and case options. Opens from a browse column's
"Filter for…" command. Returns a `FilterForPattern`.

## What it looks like
<!-- CAPTURE: launch legacy FLEx (UIMode=Legacy), open a browse view, on a column's filter
     menu choose "Filter for…". See .claude/skills/fieldworks-winapp. -->
![Filter For – initial](./images/filter-for-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Match-text field (required).
- [ ] Position radios (mutually exclusive): anywhere (default) / at start / at end / whole item.
- [ ] "Use regular expressions" checkbox: disables the position radios when on.
- [ ] "Match case" checkbox.
- [ ] OK gated: empty match text blocks OK; in regex mode an invalid regex blocks OK.

## Migration gotchas
- Stub header: "the Avalonia counterpart of the legacy `SimpleMatchDlg`"; its Behaviors section documents
  the position-radio disabling under regex and the OK-gating rules.
- Note: the legacy `SimpleMatchDlg` also exposes a "Match diacritics" option; verify whether the Avalonia
  `FilterForPattern` carries it (the stub's documented radios are anywhere/start/end/whole + regex + case).
- WS/RTL: the match text is plain text against a single column's display string.

## Wiring
- Legacy call site(s): the Legacy branch of the filter-for path in `Src/xWorks/RecordBrowseView.cs`
  constructs the WinForms `SimpleMatchDlg` (`Src/Common/Controls/XMLViews/SimpleMatchDlg.cs`).
- The Avalonia path branched on `UIMode=New` here before back-out (direct, no launcher):
  `Src/xWorks/RecordBrowseView.cs:662` — `new FwAvaloniaDialogs.FilterForDialogViewModel();`
  (View constructed at `:663`).
- Re-wiring target: `RecordBrowseView` filter-for path re-enters the Avalonia dialog behind `UIMode=New`;
  Legacy keeps `SimpleMatchDlg`.
