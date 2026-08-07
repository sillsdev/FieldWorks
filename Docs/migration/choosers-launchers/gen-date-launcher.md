# Generic Date Launcher (`GenDateLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.GenDateLauncher` (`Src/Common/Controls/DetailControls/GenDateLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwOptionPicker (atomic owned control) |
| **JIRA** | LT-XXXXX |

## What it is
A button launcher (subclass of `ButtonLauncher`) that opens the generic-date chooser (`GenDateChooserDlg`) to edit a `GenDate` field shown in a slice.

## Notes / gotchas
- Pairs with `GenDateChooserDlg`; edits the FieldWorks `GenDate` (imprecise/partial date) type, not a plain date.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
