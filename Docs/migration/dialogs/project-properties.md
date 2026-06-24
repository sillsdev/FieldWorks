# Project Properties (`FwProjPropertiesDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwProjPropertiesDlg` (`Src/FwCoreDlgs/FwProjPropertiesDlg.cs`) |
| **Area** | App-wide (project settings) |
| **Type** | dialog |
| **Primitive** | TABS |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | tabs→OptionsDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![fw-proj-properties legacy](./images/fw-proj-properties-before.png) | ![fw-proj-properties avalonia](./images/fw-proj-properties-after.png) |

Tabs (legacy):

![general](./images/fw-proj-properties-tab-general.png) ![linked-files](./images/fw-proj-properties-tab-linked-files.png) ![sharing](./images/fw-proj-properties-tab-sharing.png)
## What it is
The Project Properties dialog — edits project-level settings (name, description, writing systems, linked-files location, etc.).

## Notes / gotchas
- Views-coupled (references `IVwRootSite`/selection-based rendering for some fields).
- Multi-tab settings surface.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
