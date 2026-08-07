# Record Go (Notebook) (`RecordGoDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.RecordGoDlg` (`Src/LexText/LexTextControls/RecordGoDlg.cs`) |
| **Area** | Notebook |
| **Type** | dialog |
| **Primitive** | search+list |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![record-go legacy](./images/record-go-before.png) | ![record-go avalonia](./images/record-go-after.png) |
## What it is
Go-to / find dialog for Data Notebook records (Notebook analog of GotoEntryDlg).

## Notes / gotchas
- Subclass of BaseGoDlg (base excluded).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

