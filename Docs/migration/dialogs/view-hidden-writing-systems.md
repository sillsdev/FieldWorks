# View Hidden Writing Systems (`ViewHiddenWritingSystemsDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.ViewHiddenWritingSystemsDlg` (`Src/FwCoreDlgs/ViewHiddenWritingSystemsDlg.cs`) |
| **Area** | App-wide (writing-system management) |
| **Type** | dialog |
| **Primitive** | plain-form (list) |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | search+list→EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it is
Lists writing systems that have text in the project but are not in the current type's list (may be in the opposite list); lets the user add a writing system or delete all text in one. Backed by `ViewHiddenWritingSystemsModel`.

## Notes / gotchas
- View/model split (`ViewHiddenWritingSystemsModel`).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
