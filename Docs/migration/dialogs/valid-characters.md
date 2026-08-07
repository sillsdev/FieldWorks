# Valid Characters (`ValidCharactersDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.ValidCharactersDlg` (`Src/FwCoreDlgs/ValidCharactersDlg.cs`) |
| **Area** | App-wide (writing-system management) |
| **Type** | dialog |
| **Primitive** | TABS |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | tabs→OptionsDialog |
| **JIRA** | LT-XXXXX |

## What it is
Dialog for specifying the valid characters for a FieldWorks writing system.

## Notes / gotchas
- Views-coupled (hosts `IVwRootSite`-based rendering).
- Hosts the owned `CharContextCtrl` (a `UserControl` with a `ContextGrid : DataGridView` and Views coupling) and uses `FwCharacterCategorizer`. Fold these into this dialog's migration.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
