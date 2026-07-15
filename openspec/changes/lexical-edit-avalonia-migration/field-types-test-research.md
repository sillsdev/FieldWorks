# §19e — Detail-editor field-type completeness test-research note (T0)

The source for the §19e hardening tests (T1 unit, T2 integration, T3 edge, T4
workflow, T5 visual). It derives the behavior/edge/workflow matrix for the
remaining detail-editor field types from the legacy WinForms slices and maps each
behavior to the §19e managed surface (a dedicated `RegionFieldKind` + a
`RegionFieldControlFactory` editor, plus the `FullEntryRegionComposer` walk and
the `IRegionEditContext` seam). Each row names the test that covers it.

Legacy sources (`Src/Common/Controls/DetailControls/`):
- Enum closed-combo: `EnumComboSlice.cs` (closed `FwOverrideComboBox` over a
  `<stringList>`; `SelectedIndex` IS the stored enum int; boolean-backed enums via
  `IntBoolPropertyConverter`).
- Integer: `BasicTypeSlices.cs` `IntegerSlice` (a borderless `TextBox`;
  `Convert.ToInt32` on focus-loss; non-numeric — including empty — warns and
  restores the stored value, never commits).
- GenDate / exact date: `GenDateLauncher.cs` + `GenDateChooserDlg.cs` +
  `DateSlice` (precision Exact/Month/Year/Century/NoDate; era AD/BC;
  unknown-month 12; `GenDate.ToLongString()` display; `GenDate.TryParse`).
- jtview: `ViewSlice.cs` + `SliceFactory.cs` case `"jtview"` (an `XmlView` over a
  nested `layout`; recursive sub-view).
- literal/"lit": `SliceFactory.cs` case `"lit"` → `MessageSlice` (the slice label
  text IS the static content).
- field visibility: `DataTree.cs` `m_fShowAllFields` + `visibility=`
  `always`/`ifdata`/`never` (no `maybe` exists in legacy — see note below).
- per-field WS visibility: `MultiStringSlice.cs` / `StringSliceUtils.cs`
  `visibleWritingSystems` attribute (a comma list restricting the WSes a
  multistring row shows).

Tests live in:
- **U** = `FwAvaloniaTests/FieldTypeEditorTests.cs` (T1 unit, headless — the editor
  control per kind: builds, commits a value, rejects bad input).
- **UM** = `FwAvaloniaTests/RegionFieldControlFactoryTests.cs` (the dispatch arm
  produces the right control per new kind).
- **C** = `xWorksTests/FieldTypeComposerTests.cs` (T1 composer/adapter, real LCModel
  — the walk classifies the field as the right kind + the setter round-trips).
- **I** = `xWorksTests/FieldTypeIntegrationTests.cs` (T2 — enum + integer + date +
  a vector + a literal on ONE realized region surface; extends the §19a/§19d
  integration template).
- **E** = `FwAvaloniaTests/FieldTypeEdgeCaseTests.cs` + `xWorksTests/FieldTypeEdgeCaseTests.cs`
  (T3 — edges).
- **W** = `xWorksTests/FieldTypeWorkflowTests.cs` (T4 — end-to-end real-cache:
  edit enum + integer + date(+qualifier) + add a semantic-domain item → commit →
  reopen → verify round-tripped).
- **V** = `FwAvaloniaTests/Visual/FieldTypeVisualTests.cs` (T5 — PNG stages +
  `AssertNoCrowding`).

## 1. Field-type behaviors × coverage

| # | Field type | Legacy behavior | §19e managed surface | Covered by |
|---|------------|-----------------|----------------------|------------|
| E1 | **Enum closed-combo** | closed combo over `<stringList>` labels; `SelectedIndex` is the stored int; never free-form | `RegionFieldKind.EnumCombo` → closed `ComboBox` editor; commits the option index through `TrySetOption`; rejects any non-option key | UM `EnumComboKind_BuildsClosedCombo`; U `EnumCombo_Commit_StagesIndex_RejectsFreeText`; C `WalkEnumCombo_ClassifiesEnumComboKind_AndSetterRoundTrips` |
| E2 | **Integer** | borderless TextBox; `Convert.ToInt32` on focus-loss; non-numeric/empty rejected (restore) | `RegionFieldKind.Integer` → numeric TextBox editor that rejects non-numeric keystrokes AND restores on a bad commit; setter parses int | UM `IntegerKind_BuildsNumericTextBox`; U `Integer_RejectsNonNumericKeystroke_AndCommitsValidInt`; C `Integer_SetterRejectsNonNumeric_AcceptsValidInt` |
| E3 | **GenDate qualifiers** | precision Exact/Month/Year/Century; era AD/BC; circa/approx; `ToLongString` | `RegionFieldKind.Date` with `DateKind=GenDate` → structured editor (year + precision + era + circa toggle) that composes the long-string the setter parses; rejects unparseable | UM `GenDateKind_BuildsStructuredEditor`; U `GenDate_PrecisionEraCirca_ComposeParseableString`; C `GenDate_Setter_RoundTripsQualifiers` |
| E4 | **Exact-date calendar picker** | DateSlice MonthCalendar | `RegionFieldKind.Date` with `DateKind=Date` → text box + calendar picker; picking a day commits an exact date | UM `ExactDateKind_BuildsCalendarPicker`; U `ExactDate_PickDay_CommitsDate_RejectsBadText` |
| E5 | **Semantic-domain 2-level** | indented possibility tree chooser | `ReferenceVector` with `RegionChoiceOption.Depth` (already; pinned here) | C `SemanticDomainVector_OptionsCarryDepthHierarchy`; W (add a semantic-domain item) |
| E6 | **Ghost vector** | create object on first add (`AssociateWithNotebook`/`LexEntryRef`) | ghost `ReferenceVector` whose add-setter find-or-creates (already; pinned here) | C `GhostVector_AddCreatesObjectAndReference` (where reachable) |
| E7 | **jtview embedded view** | `XmlView` recursively renders a nested `layout` | EmbeddedView recurses the nested `layout` (composes its fields inline at depth+1); deep recursion guarded by the visited-set | C `JtView_ComposesNestedLayoutFields`; E `JtView_EmptyOrMissingLayout_DegradesToReadOnly` |
| E8 | **literal/"lit"** | `MessageSlice`: the label text is the static content (gray, read-only) | `RegionFieldKind.Literal` → static `TextBlock` renderer (the label/message text), no value column | UM `LiteralKind_BuildsStaticText`; C `Literal_ComposesLiteralKind_WithLabelAsContent` |
| E9 | **Field-visibility toggle** | `m_fShowAllFields` reveals `never` + keeps empty `ifdata` rows | composer `showHiddenFields` flag honors `Never`/`IfData` (already); pinned here + the host toggle | C `ShowHidden_RevealsNeverAndEmptyIfData`; C `HideHidden_OmitsNeverAndEmptyIfData` |
| E10 | **Per-field WS visibility** | `visibleWritingSystems` restricts a multistring row's WSes | importer parses `visibleWritingSystems`; composer limits the row's `RegionWsValue`s to that subset (intersected with valid) | C `PerFieldWs_LimitsDisplayedWritingSystems`; E `PerFieldWs_OneVsManyWs` |

## 2. Edge cases × coverage

| # | Edge case | Expectation | Covered by |
|---|-----------|-------------|------------|
| C1 | Enum: stored value out of range | chooser selects nothing (blank); no crash; never mis-points | C `WalkEnumCombo_OutOfRangeValue_SelectsNothing` |
| C2 | Enum: empty / index 0 with HideWhenEmpty | hidden unless show-hidden (legacy `ifdata`) | C `WalkEnumCombo_ZeroValue_HiddenWhenIfData` |
| C3 | Integer: non-numeric, negative, overflow, empty | non-numeric/empty rejected; negative accepted; overflow rejected (no corruption) | U `Integer_RejectsNonNumeric_Negative_Overflow_Empty`; C `Integer_SetterRejectsOverflowAndEmpty_AcceptsNegative` |
| C4 | GenDate: BCE / decade / century / circa boundaries | each round-trips through `GenDate.TryParse`/`ToLongString` | C `GenDate_BceDecadeCenturyCirca_RoundTrip` |
| C5 | Date/GenDate: invalid text | rejected; box restores committed value; field never corrupted | U `Date_InvalidText_RestoresCommitted`; C `Date_SetterRejectsGarbage` |
| C6 | Vector: empty / large | empty offers add slot; large composes all items + options | C `Vector_EmptyOffersAdd_LargeComposesAll` (reuses §-vector path) |
| C7 | jtview: empty / missing / nested | empty or unresolvable layout degrades to a read-only row (no crash); nested composes | E `JtView_EmptyOrMissingLayout_DegradesToReadOnly`; C `JtView_ComposesNestedLayoutFields` |
| C8 | visibility toggle with no `maybe`/hidden fields | toggling changes nothing; no crash | C `ShowHidden_NoHiddenFields_NoChange` |
| C9 | per-field WS with one vs many WSes | one-WS subset shows exactly one row; many shows the subset in order | E `PerFieldWs_OneVsManyWs` |
| C10 | RTL / complex-script values in an enum label / literal / date | render + commit without corruption | E `RtlComplexScript_InLiteralAndEnum` |
| C11 | cancel vs commit of an enum/integer/date gesture | cancel rolls back; commit persists (one undo step per gesture) | W (cancel/commit journey); I integration undo asserts |

**`visibility='maybe'` note.** The §19e prompt names `visibility='maybe'`, but the
legacy WinForms `DataTree` recognizes only `always` / `ifdata` / `never`
(`DataTree.cs` asserts exactly those three; there is no `maybe`). The faithful
parity behavior — and what this work pins — is the `m_fShowAllFields` toggle that
(a) reveals `never` fields and (b) keeps empty `ifdata` fields visible. The
composer's `showHiddenFields` flag (`IsHidden`/`HideWhenEmpty`) is that toggle. No
`maybe` value is introduced (it would be unfaithful); the importer continues to map
`ifdata`→`IfData`, `never`→`Never`, everything else→`Always`.

## 3. User workflows × coverage

| # | Workflow | Covered by |
|---|----------|------------|
| WF1 | One realized surface with enum + integer + date + a reference vector + a literal: edit each, assert they compose and stage, undo grouping per gesture, refresh intact | I (whole fixture) |
| WF2 | Real cache: edit an enum + an integer + a date(+GenDate qualifier) + add a semantic-domain item → commit → reopen → verify all round-tripped | W `Workflow_EditEnumIntegerDateAndAddSemanticDomain_RoundTrips` |
| WF3 | Cancel a staged enum/integer/date edit → reopen shows the original | W `Workflow_CancelLeavesEverythingUnchanged` |

## 4. Visual (T5) stages

PNG stages captured by **V** (then `AssertNoCrowding`, full-project run for
order-dependence):
- `FieldType-01-enum-combo` — the closed enum combo (collapsed + a value selected).
- `FieldType-02-integer` — the numeric integer editor.
- `FieldType-03-gendate` — the GenDate qualifier editor (precision/era/circa).
- `FieldType-04-exact-date` — the exact-date calendar editor.
- `FieldType-05-literal` — the static literal row.
- `FieldType-06-integration` — the integration surface (enum + integer + date +
  vector + literal together).

## 5. Product issues found / decisions

- **Enum & integer previously routed to `Chooser`/`Text` kinds.** Functionally
  safe (the closed-chooser already rejected free text; the int setter already
  rejected non-numeric), but the §19e ask is a *dedicated* editor per kind so the
  rejection is visible at the editor and the dispatch is explicit. Added
  `RegionFieldKind.EnumCombo` / `Integer` / `Literal` and their editors; the
  composer now routes to them. The old read-only/degraded fallbacks are preserved.
- **jtview** was a read-only `ShortName` row. Added bounded nested-layout
  recursion (the common single-level case) reusing the proven
  `CompileForObjectWithOverrides`/`EnterModel`/`Walk` path the picture/sense walks
  use; the visited-set already guards against cyclic deep recursion. A
  `// PARITY §19e` note marks where arbitrarily deep custom jtview nests are not
  exhaustively reproduced.
- **Per-field WS visibility** (`visibleWritingSystems`) was not imported. Added the
  importer attribute + the composer subset filter.
- **[HIGH / URGENT] `FwGenDateField` corrupts the year of a GenDate with
  month/day set** (`xcut-review-2026-06-21.json`, tasks.md 19i.1). Composer puts
  the value's `ToLongString()` (e.g. `"Saturday, June 15, 1985"`) in the value
  slot; `FwGenDateField.TryParseLongString` seeds the year from the FIRST digit
  run in that string, which for a day-precise date is the DAY ("15"), not the
  year. Editing any qualifier then recomposes and commits from the wrong seed —
  a 1985 day-precise date can silently become year 15. Every existing GenDate
  test uses month=0/day=0 (year-only) values, so the suite never exercised a
  month/day string and the bug shipped green. tasks.md 19i.1 records a fix
  (compose from the GenDate model's structured parts, never `ToLongString()`);
  confirm the fix is covered by a month/day-bearing regression test, not just
  year-only fixtures, before treating this as closed.
