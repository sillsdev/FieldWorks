# Dictionary Configuration Manager (`DictionaryConfigurationManagerDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.DictionaryConfigurationManagerDlg` (`Src/xWorks/DictionaryConfigurationManagerDlg.cs`) |
| **Area** | Dictionary-config |
| **Type** | dialog |
| **Primitive** | TABLE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (list of configurations with rename/copy/delete/import/export) |
| **JIRA** | LT-XXXXX |

## What it is
Manages the set of dictionary configurations (rename, copy, delete, import, export) shown in a `ListView` (`configurationsListView`). The "Manage Views" entry point from `DictionaryConfigurationDlg`.

## Notes / gotchas
- The CURRENT manager (newer than `DictionaryConfigMgrDlg`); newed from `Src/xWorks/DictionaryConfigurationController.cs:367`. Logic lives in `DictionaryConfigurationManagerController`.
- Manipulates per-item fonts (bold = current) and focus handlers on the `ListView`.
- Import/export flows reach `DictionaryConfigurationImportDlg` and controller export code.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
