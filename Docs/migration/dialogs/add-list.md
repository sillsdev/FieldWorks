# Add Custom List (`AddListDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.AddListDlg` (`Src/xWorks/CustomListDlg.cs`) |
| **Area** | Lists |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Creates a new TopicListEditor-style custom list: collects multilingual name/description plus hierarchy, sort-by, duplicate and display-by options, then creates the list.

## Notes / gotchas
- Concrete subclass of the shared base `CustomListDlg` (`Src/xWorks/CustomListDlg.cs`); the base hosts the actual controls (multistring name/description via `LabeledMultiStringControl`, WS combo, display-by combo, hierarchy/sort/duplicate checkboxes). Migrate the base once and parameterise for Add vs Configure.
- Newed at `Src/xWorks/XWorksViewBase.cs:812`.
- Uses `LabeledMultiStringControl` (owned multilingual control) — needs an owned-control equivalent.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
