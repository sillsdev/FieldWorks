# Simple Integer Match (`SimpleIntegerMatchDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Controls.SimpleIntegerMatchDlg` (`Src/Common/Controls/XMLViews/SimpleIntegerMatchDlg.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | nearest (OptionsDialog; sibling of FilterFor/DateRangeFilter) |
| **JIRA** | LT-XXXXX |

## What it is
Browse-view integer column filter dialog: pick a comparison (greater than / less than / equal / not equal / <= / >= / between) and one or two integer values; used from a numeric column's "Filter for…" menu. Integer sibling of `SimpleMatchDlg` (FilterFor) and `SimpleDateMatchDlg` (DateRangeFilter).

## Notes / gotchas
- "Between" mode enables a second `NumericUpDown` and an "and" label; other comparisons use a single value — gate UI on the selected comparison.
- Comparison index order is hard-coded (GreaterThan=0 … Between=6); preserve the mapping when porting.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
