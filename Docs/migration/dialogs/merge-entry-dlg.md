# Merge Entry (`MergeEntryDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.MergeEntryDlg` (`Src/LexText/LexTextControls/MergeEntryDlg.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | search+list |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![merge-entry legacy](./images/merge-entry-before.png) | ![merge-entry avalonia](./images/merge-entry-after.png) |
## What it is
Search for and select the entry to merge the current entry into.

## Notes / gotchas
- Subclass of EntryGoDlg. State=coexist: Avalonia path is LcmMergeEntryDialogLauncher under UIMode=New. Merge is destructive - preserve confirmation/undo semantics.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

