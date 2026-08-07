# Configure Custom List (`ConfigureListDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.ConfigureListDlg` (`Src/xWorks/CustomListDlg.cs`) |
| **Area** | Lists |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Edits the properties (name, description, hierarchy/sort/duplicate/display-by options) of an existing custom list and reports whether changes were made.

## Notes / gotchas
- Concrete subclass of the shared base `CustomListDlg` (`Src/xWorks/CustomListDlg.cs`); shares all controls with `AddListDlg` — migrate the base once.
- Newed at `Src/xWorks/XWorksViewBase.cs:776`. Tracks a "changed existing list" flag for caller refresh.
- Uses `LabeledMultiStringControl` (owned multilingual control).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
