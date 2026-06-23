# Find / Replace — bulk-replace subset (legacy `FwFindReplaceDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwFindReplaceDlg` (`Src/FwCoreDlgs/FwFindReplaceDlg.cs`) — app-wide modeless find/replace; this is a Phase-1 **subset** for bulk replace only |
| **Area / tool** | Browse view › bulk edit › Find/Replace pattern setup |
| **Primitive(s)** | plain-form (find/replace text + match-option checkboxes) |
| **Canonical reference** | OptionsDialog (closest kept canonical for a small plain-form with controls) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/FindReplaceDialogView.axaml(.cs)` + `FindReplaceDialogViewModel.cs` (+ `FindReplacePattern.cs`) @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
A spec-only modal that lets the user author a Find/Replace pattern for a bulk replace over a browse
column. OK snapshots the edited fields into a `FindReplacePattern`. There is NO find engine and NO
modeless app-wide find/replace here — that is deferred to Phase 2.

## What it looks like
<!-- CAPTURE: launch legacy FLEx (UIMode=Legacy), open a browse view bulk-edit Find/Replace tab
     and open the find/replace setup. See .claude/skills/fieldworks-winapp. -->
![Find / Replace – initial](./images/find-replace-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Find-text field (required) and Replace-text field.
- [ ] "Use regular expressions" checkbox: disables/clears the literal-only options (match case, whole word).
- [ ] "Match case" and "Match whole word" checkboxes (literal mode only; disabled when regex is on).
- [ ] "Match diacritics" and "Match writing system" checkboxes are present but grayed (P1 no-ops).
- [ ] OK gated: empty find text blocks OK; in regex mode an invalid regex blocks OK.

## Migration gotchas
- DEFERRED P2 (stub `FindReplaceDialogViewModel.cs`): "There is NO find engine and NO modeless app-wide
  Find/Replace here (deferred P2); OK simply snapshots the edited fields into" the pattern.
- P1 SCOPE (stub `FindReplacePattern.cs`): the producer applies the pattern over single-WS plain-text cells
  in managed code (System.Text.RegularExpressions, else literal case/whole-word replace).
- P1 NO-OPS (stub): "`MatchDiacritics` and `MatchWritingSystem` are CARRIED but are P1 no-ops (a faithful
  diacritic/WS-collation match needs the full `IVwPattern` + TsString round-trip, deferred to P2)"; the
  dialog grays them "so the user is not misled."
- View comment: "Spec-only modal (OK snapshots the FindReplacePattern into Result) — there is NO find engine
  and NO modeless app-wide dialog here (deferred P2)."

## Wiring
- Legacy call site(s): the legacy bulk-replace path opens the WinForms find/replace surface
  (`FwFindReplaceDlg`, `Src/FwCoreDlgs/FwFindReplaceDlg.cs`); the modeless app-wide dialog stays on this path.
- The Avalonia path branched on `UIMode=New` here before back-out (direct, no launcher):
  `Src/xWorks/RecordBrowseView.cs:1641` — `new FwAvaloniaDialogs.FindReplaceDialogViewModel(pattern);`
  (View at `:1642`).
- Re-wiring target: `RecordBrowseView` bulk find/replace path re-enters the Avalonia dialog behind
  `UIMode=New`; the modeless app-wide `FwFindReplaceDlg` remains on the legacy path and is the Phase-2 target.
