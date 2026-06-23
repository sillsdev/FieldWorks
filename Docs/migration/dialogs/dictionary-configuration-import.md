# Dictionary Configuration Import (`DictionaryConfigurationImportDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.DictionaryConfigurationImportDlg` (`Src/xWorks/DictionaryConfigurationImportDlg.cs`) |
| **Area** | Dictionary-config |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Imports a dictionary configuration from a file: file-path text box + Browse, overwrite/don't-overwrite radio options, an explanation pane, and Import/Cancel/Help.

## Notes / gotchas
- Plain-form (TextBox + Browse button + RadioButtons + explanation TextBox); logic in `DictionaryConfigurationImportController`.
- Overwrite vs new-name radio options drive the import behaviour — preserve the gating/explanation text.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
