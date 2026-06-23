# Conflicting Save (`ConflictingSaveDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.ConflictingSaveDlg` (`Src/FdoUi/Dialogs/ConflictingSaveDlg.cs`) |
| **Area** | App-wide |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Message-box-like dialog warning of a conflicting save, offering OK and "Refresh Now"; clicking Refresh Now yields `DialogResult.Yes`.

## Notes / gotchas
- Trivial two-button message form; the non-standard `DialogResult.Yes` for "Refresh Now" must be preserved for callers.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
