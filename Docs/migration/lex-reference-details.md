# Lexical Reference Details (legacy `LexReferenceDetailsDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.LexReferenceDetailsDlg` (`Src/LexText/LexTextControls/LexReferenceDetailsDlg.cs`) |
| **Area / tool** | Lexicon › lexical-reference slice › "Edit Reference Set Details…" |
| **Primitive(s)** | plain-form (2 fields: Name + multi-line Comment) |
| **Canonical reference** | InsertEntryDialog (closest kept canonical for a small plain-form with text fields) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/LexReferenceDetailsDialogView.axaml(.cs)` + `LexReferenceDetailsDialogViewModel.cs` @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user edit a lexical reference set's name (its display label / "type" for this set) and an optional
comment/note. Opens from the lexical-reference slice's "edit details" command.

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![lex-reference-details legacy](./images/lex-reference-details-before.png) | ![lex-reference-details avalonia](./images/lex-reference-details-after.png) |
## Behaviour to preserve (parity checklist)
- [ ] Explanation / instructional text at top.
- [ ] Name text field (single-line) with its label.
- [ ] Comment text field (multi-line, accepts returns / wraps) with its label.
- [ ] OK is intentionally NOT gated — a reference set may legitimately carry an empty name or note.
- [ ] Name and Comment are trimmed before the launcher reads them.
- [ ] OK / Cancel buttons.

## Migration gotchas
- Stub header: "Phase-1 §19g".
- PARITY (stub): "OK is intentionally NOT gated — parity with the legacy dialog, where a reference set may
  legitimately carry an empty name or note (the slice display falls back to the reference type's own name)."
  Do not add an OK gate on Name.
- Undo-fencing: the edit runs inside the caller's undo action (`ksUndoEditRefSetDetails` /
  `ksRedoEditRefSetDetails`).

## Wiring
- Legacy call site(s): the Legacy edit-details path in `Src/LexText/LexTextControls/LexReferenceMultiSlice.cs`
  constructs the WinForms `LexReferenceDetailsDlg` (`Src/LexText/LexTextControls/LexReferenceDetailsDlg.cs`).
- The Avalonia path branched on `UIMode=New` here before back-out:
  `Src/LexText/LexTextControls/LexReferenceMultiSlice.cs:1041` — `LcmLexReferenceDetailsLauncher.Edit(...)`.
  Launcher: `LcmLexReferenceDetailsLauncher` (`Src/LexText/LexTextControls/LcmLexReferenceDetailsLauncher.cs`).
- Re-wiring target: `LexReferenceMultiSlice` edit-details path re-enters the Avalonia dialog behind
  `UIMode=New`; Legacy keeps `LexReferenceDetailsDlg`.
