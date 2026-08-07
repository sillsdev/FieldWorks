# Generic Date Chooser (`GenDateChooserDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.GenDateChooserDlg` (`Src/Common/Controls/DetailControls/GenDateChooserDlg.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | nearest (small plain-form; OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
A chooser dialog for generic dates (`GenDate`) — lets the user pick an approximate/partial date (precision, era, possibly partial year/month/day). Launched by `GenDateLauncher`.

## Notes / gotchas
- Edits the FieldWorks `GenDate` type (supports imprecise dates: before/about/after, partial fields) — not a plain calendar date.
- Returns the chosen `GenDate` to the launching slice.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
