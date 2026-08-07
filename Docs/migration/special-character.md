# Special Character / Unicode Insert (net-new — replaces OS charmap shellout)

| | |
|---|---|
| **Legacy class** | _None._ The legacy `Format › Special character` command shells out to the OS character map (`charmap.exe` / `gucharmap`); there is no WinForms truth dialog to port. |
| **Area / tool** | Any editable field › Format › Special character (insert a Unicode character) |
| **Primitive(s)** | plain-form + list (filterable character picker) |
| **Canonical reference** | EntryGoDialog (closest kept canonical: a filter text box + a results list) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/SpecialCharacterDialogView.axaml(.cs)` + `SpecialCharacterDialogViewModel.cs` @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
A net-new in-app Avalonia Unicode picker: a filterable list of insertable characters over a gated
Insert + Cancel. It replaces the legacy `Format › Special character` shellout to the OS character map.

## What it looks like
<!-- CAPTURE: this is net-new (no legacy in-app dialog). Capture the Avalonia surface from the
     headless preview / test host. See .claude/skills/fieldworks-winapp. -->
![Special Character – initial](./images/special-character-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Filter text box (watermark prompt) does a case-insensitive search across character name OR code label OR exact character.
- [ ] Character list shows `{Character}  {CodeLabel}  {Name}` (e.g. "é  U+00E9  Latin Small Letter E…").
- [ ] When the current selection filters out, it is cleared.
- [ ] A curated default set (combining diacritics, IPA, punctuation, arrows — ~25 chars) is shown initially.
- [ ] Insert gated on a selection (`GetValidationErrors` yields `MustSelectMessage` when none selected).
- [ ] In-line validation message shown below the list when nothing is selected.

## Migration gotchas
- Net-new, so there is no pixel parity baseline against a WinForms dialog — parity is against the OS charmap
  *capability* (pick a character and insert it), not its appearance.
- Stub header: "Phase-1 §19g … Unlike the other §19g dialogs there is no WinForms truth dialog to port —
  the legacy `Format > Special character` command shells out to the OS character map
  (`charmap.exe`/`gucharmap`). This is a net-new in-app Avalonia picker…".
- The default character set is curated in the ViewModel; a future ticket may broaden it to a full Unicode
  catalog.

## Wiring
- **UNWIRED (test-only).** There is no product call site. The only references are the stub definition
  (`SpecialCharacterDialogView.axaml.cs`, `SpecialCharacterDialogViewModel.cs`), the tests under
  `Src/Common/FwAvaloniaDialogs/FwAvaloniaDialogsTests/`, and a comment in `Src/xWorks/FwXWindow.cs:955`
  ("§19g … ships a NET-NEW in-app Avalonia Unicode picker … this legacy OS-charmap shellout is preserved
  unchanged"). The legacy charmap shellout is untouched, so there is no call site to revert.
- Re-wiring target (future): the New-UI "insert into field" affordance opens this picker behind `UIMode=New`;
  Legacy keeps the OS-charmap shellout.
