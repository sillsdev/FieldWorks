# Date Range Filter (legacy `SimpleDateMatchDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Controls.SimpleDateMatchDlg` (`Src/Common/Controls/XMLViews/SimpleDateMatchDlg.cs`) |
| **Area / tool** | Any browse view › date column header filter › "Filter for…" (date) |
| **Primitive(s)** | plain-form (relation combo + CalendarDatePickers) |
| **Canonical reference** | OptionsDialog (closest kept canonical for a small plain-form with controls) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/DateRangeFilterDialogView.axaml(.cs)` + `DateRangeFilterDialogViewModel.cs` (+ `DateRangeFilterPattern.cs`) @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user filter a date browse column by a relation (on / not on / on-or-before / on-or-after /
between) and one or two dates. Opens from a date column's filter "Filter for…" command. Returns a
`DateRangeFilterPattern`.

## What it looks like
<!-- CAPTURE: launch legacy FLEx (UIMode=Legacy), open a browse view with a date column,
     on its filter menu choose "Filter for…". See .claude/skills/fieldworks-winapp. -->
![Date Range Filter – initial](./images/date-range-filter-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Relation combo with 5 relations: on / not on / on-or-before / on-or-after / between.
- [ ] Start-date picker (required).
- [ ] End-date picker shown only when the relation is "between".
- [ ] Date semantics: start = midnight; end = last instant of the day (23:59:59.9999999) for inclusive matching.
- [ ] OK gated: missing start date blocks OK; for "between", the end day must be on or after the start day.

## Migration gotchas
- Stub header: "the Avalonia counterpart of the legacy `SimpleDateMatchDlg`"; the header notes "Date
  semantics mirror the legacy dialog" with the MIDNIGHT / LAST-INSTANT normalisation and the
  "SelectionEnd extends to the very end of the day" rule.
- Legacy uses `DateTimePicker` controls; the Avalonia stub uses `CalendarDatePicker` — verify date round-trip
  and the inclusive end-of-day boundary.
- The launcher receives a `handleGenDate` callback (constructor takes `(null, handleGenDate)`); preserve the
  GenDate handling on re-wiring.

## Wiring
- Legacy call site(s): the Legacy branch of the date-filter path in `Src/xWorks/RecordBrowseView.cs`
  constructs the WinForms `SimpleDateMatchDlg` (`Src/Common/Controls/XMLViews/SimpleDateMatchDlg.cs`).
- The Avalonia path branched on `UIMode=New` here before back-out (direct, no launcher):
  `Src/xWorks/RecordBrowseView.cs:717` — `new FwAvaloniaDialogs.DateRangeFilterDialogViewModel(null, handleGenDate);`
  (View at `:718`).
- Re-wiring target: `RecordBrowseView` date-filter path re-enters the Avalonia dialog behind `UIMode=New`;
  Legacy keeps `SimpleDateMatchDlg`.
