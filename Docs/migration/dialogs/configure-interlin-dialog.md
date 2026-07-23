# Configure Interlinear (`ConfigureInterlinDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.IText.ConfigureInterlinDialog` (`Src/LexText/Interlinear/ConfigureInterlinDialog.cs`) |
| **Area** | Texts&Words |
| **Type** | dialog |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![configure-interlin legacy](./images/configure-interlin-before.png) | ![configure-interlin avalonia](./images/configure-interlin-after.png) |
## What it is
Configures which interlinear lines/writing systems are shown and their order.

## Notes / gotchas
- Owned line-ordering control (multi-line picker).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

