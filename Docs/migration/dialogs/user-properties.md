# User Properties (`FwUserProperties`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwUserProperties` (`Src/FwCoreDlgs/FwUserProperties.cs`) |
| **Area** | App-wide (user management) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form→nearest |
| **JIRA** | LT-XXXXX |

## What it is
User Properties / logons dialog. Per the class comment, this is reached only by a menu command not currently configured in any XML, and the feature was never fully implemented — likely obsolete.

## Notes / gotchas
- Largely dead code; verify whether it is still reachable before investing in a migration.
- Parent of `AddNewUserDlg` (Add button).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
