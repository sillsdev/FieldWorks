# Utilities (`UtilityDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.UtilityDlg` (`Src/FwCoreDlgs/UtilityDlg.cs`) |
| **Area** | App-wide (tools / utilities) |
| **Type** | dialog |
| **Primitive** | plain-form (list + description panes) |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | search+list→EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it is
Presents the list of utilities defined in `Language Explorer\Configuration\UtilityCatalogInclude.xml`. Each utility implements `IUtility` and can set explanatory labels describing when it is needed and what it does.

## Notes / gotchas
- Plugin-style: items come from `IUtility` implementations enumerated from XML config.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
