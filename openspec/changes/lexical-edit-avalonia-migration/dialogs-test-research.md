# §19g Remaining Functional Dialogs — T0 Test Research / Traceability Matrix

(§19.0 rubric, Phase-1 §19g.) Maps the remaining FUNCTIONAL dialogs reachable
from lexical-edit / browse — each dialog's behavior × edges × workflows — to the
headless test that covers it. Tests live in
`Src/Common/FwAvaloniaDialogs/FwAvaloniaDialogsTests/` (VM/view tier) and
`Src/LexText/LexTextControls/LexTextControlsTests/` (launcher / real-cache tier).

## Scope decision (what ships FULLY vs. what is a precise PARITY note)

| # | Dialog | WinForms truth | Disposition | Why |
| --- | --- | --- | --- | --- |
| 1 | **Delete-confirmation** (entry/sense/reference + orphan summary) | `ConfirmDeleteObjectDlg` (`Src/FdoUi/Dialogs`) | **SHIP FULLY** | Bounded: reuses the FwMessageBox-style modal; the affected-object summary is `obj.Object.DeletionTextTSS`, delete gated by `obj.Object.CanDelete`. |
| 4 | **LexReferenceDetails** (edit a reference's name/type + note) | `LexReferenceDetailsDlg` (`Src/LexText/LexTextControls`) | **SHIP FULLY** | Plain `Form`: a name `TextBox` + a comment `TextBox`. Tiny. |
| 6 | **Character / special-char insert** (Unicode picker into a field) | *No FW dialog* — `FwXWindow.OnShowCharMap` shells out to `charmap.exe`/`gucharmap` | **SHIP FUNCTIONAL CORE** | No WinForms truth dialog exists to port; build a net-new Avalonia Unicode picker (filter + grid + insert). Legacy OS shellout preserved. |
| 3 | **Writing System properties / Add-WS** | `FwWritingSystemSetupDlg` + `FwWritingSystemSetupModel` (1200+ lines: SLDR sharing, encoding converters, merge, advanced script/region/variant) | **SHIP BOUNDED CORE** | Full model is far out of a reasonable slice. Ship the managed Add/Edit core (name, abbreviation, font, RTL direction, sort/keyboard label); SLDR/converters/merge/advanced = PARITY note. |
| 5 | **Lex Options tool-specific tabs** | `LexOptionsDlg` (`Form, IFwExtension`) | **EXTEND OptionsDialog** | The existing migrated Options dialog already covers the four real tabs (General/Plugins/Privacy/Updates) that `LexOptionsDlg` exposes; verify parity and note any residual tab. |
| 2 | **Styles dialog** (manage + apply char/para styles) | `FwStylesDlg(IVwRootSite, …)` (`Src/FwCoreDlgs`) | **PARITY §19g (DEFER → Stage 9)** | `FwStylesDlg` hosts `IVwRootSite`/`SimpleRootSite` (Views engine). The dialog-kit hard rule (dialog-conversion.md §0) explicitly routes Views-engine-coupled dialogs to Stage 9, NOT this kit. §19c already ships named-style APPLY over host-supplied style lists; managing/authoring styles needs the document engine. |
| 7 | **Reversal-entry editor** | `ReversalEntryGoDlg : BaseGoDlg` | **PARITY §19g** | Go/navigation dialog atop the shared `BaseGoDlg` infra (EntryGo lane); reversal-index editing is its own tool surface, larger than a bounded slice. |
| 8 | **Occurrence navigation** | `OccurrenceDlg` | **PARITY §19g** | Inline min/max occurrence picker used only inside morphology rule / interlinear concordance editors — not on the lexical-edit/browse path; low frequency. |
| 9 | **Import/Export** (LIFT/SFM/CSV) | `LiftImportDlg`, `LexImportWizard`, `CombineImportDlg`, `ExportDialog` | **PARITY §19g** | Multi-page wizards + OS-file flows (managed via `IStorageProvider`); each is a project-sized slice. Deferred with a note. |

**Product wiring status (xcut-review-2026-06-21.json).** "SHIP FULLY"/"SHIP BOUNDED
CORE" above describe the VM/view/launcher tier, not necessarily a live product caller:
- Row 1 (Delete-confirmation): `LcmDeleteObjectLauncher.Confirm` is wired ONLY for
  `LexReference` relation deletes (`LexReferenceMultiSlice.cs`). The primary
  entry/sense delete paths (`RecordClerk.DeleteRecord`, `FdoUiCore.DeleteUnderlyingObject`)
  still build the legacy `ConfirmDeleteObjectDlg` with no UIMode gate — the Avalonia
  dialog is not reachable from a primary delete. Tracked as tasks.md 19i.5.
- Row 3 (Writing System properties) and Row 6 (Character/special-char insert): both
  have zero product callers today — `FwXWindow.cs` still unconditionally builds the
  legacy `FwWritingSystemSetupDlg` / shells `charmap.exe`, with no gate onto the
  Avalonia dialogs. Tracked as tasks.md 19i.4.

## Dialogs shipped fully — behavior matrices

### #1 Delete-confirmation (`DeleteConfirmation*` VM/view; `LcmDeleteObjectLauncher`)

WinForms truth: `ConfirmDeleteObjectDlg.SetDlgInfo` —
`obj.Object.DeletionTextTSS` (top summary), an optional secondary note
(`tssNote`, used for sequence/collection/tree orphan wording), the bottom
question, and `m_deleteButton.Enabled = obj.Object.CanDelete` (with `label2`
"Do you want to continue…?" hidden when delete is impossible).

| # | WinForms behavior | New behavior | Test (tier) |
| --- | --- | --- | --- |
| B1 | Top box shows `DeletionTextTSS` summary | VM `Summary` carries the summary text; view shows it | `Renders_Summary` (T1) |
| B2 | Optional secondary note (orphan / sequence wording) | VM `Note`; shown only when non-empty | `Note_ShownWhenPresent` (T1) |
| B3 | Delete button enabled only when `CanDelete` | VM `CanDelete=false` ⇒ OK/Delete disabled; question hidden | `DeleteDisabled_WhenCannotDelete` (T1/T3) |
| B4 | `DialogResult.Yes` ⇒ caller runs the UOW remove | Launcher returns Accepted ⇒ runs the supplied remove in one UOW | `Launcher_Accepted_RemovesInUow` (T1, real cache) |
| B5 | Cancel ⇒ no change | Cancel ⇒ launcher returns not-accepted; no UOW | `Launcher_Cancel_NoChange` (T1/T3) |
| B6 | Window title = "Delete {class}" | VM `Title`; passed to ShowModal | `Title_NamesClass` (T1) |

Edges (T3): cannot-delete (B3); empty/whitespace note (hidden); deleting the
last sense / orphan-cascade wording renders without crowding; cancel-vs-commit.

### #4 LexReferenceDetails (`LexReferenceDetails*` VM/view; `LcmLexReferenceDetailsLauncher`)

WinForms truth: `LexReferenceDetailsDlg` (Name `TextBox`, Comment `TextBox`),
applied in `LexReferenceMultiSlice.EditReferenceDetails` —
`lr.Name.SetAnalysisDefaultWritingSystem` / `lr.Comment.Set…` in one UOW on OK.

| # | WinForms behavior | New behavior | Test (tier) |
| --- | --- | --- | --- |
| B1 | Name box seeded from `lr.Name.AnalysisDefaultWritingSystem` | VM `ReferenceName` seeded; two-way bound | `Seeds_NameAndComment` (T1) |
| B2 | Comment box seeded similarly | VM `ReferenceComment` seeded; two-way bound | `Seeds_NameAndComment` (T1) |
| B3 | OK ⇒ write name + comment in one UOW | Launcher writes both in one UOW on Accepted | `Launcher_Accepted_WritesNameAndNote` (T1, real cache) |
| B4 | Cancel ⇒ no write | Launcher Cancel ⇒ no write | `Launcher_Cancel_NoWrite` (T3) |
| B5 | Always-enabled OK (a reference may have an empty note/name) | OK enabled even with empty fields | `Ok_EnabledWithEmptyFields` (T3) |

Edges (T3): empty reference note (B5), whitespace-only name round-trips,
cancel-vs-commit.

### #6 Character / special-char insert (`SpecialCharacter*` VM/view)

No WinForms truth dialog (OS shellout). Functional-core behavior:

| # | Behavior | Test (tier) |
| --- | --- | --- |
| B1 | Lists a curated set of insertable Unicode characters (code + name) | `Lists_Characters` (T1) |
| B2 | Filter narrows by name / hex code substring | `Filter_NarrowsList` (T1) |
| B3 | Selecting a char + OK yields the chosen character (`ChosenCharacter`) | `Selecting_YieldsCharacter` (T1) |
| B4 | OK gated on a selection | `Ok_GatedOnSelection` (T1) |
| B5 | Empty filter ⇒ shows all / no crash | `EmptyFilter_ShowsAll` (T3) |
| B6 | Filter with no matches ⇒ empty list, OK disabled | `NoMatchFilter_DisablesOk` (T3) |

### #3 Writing System Add/Edit core (`WritingSystemProperties*` VM/view)

WinForms truth (bounded subset): name, abbreviation, font, right-to-left
direction, sort/keyboard label. Full `FwWritingSystemSetupModel` is the PARITY
boundary.

| # | Behavior | Test (tier) |
| --- | --- | --- |
| B1 | Seeds name/abbr/font/RTL/sort from input | `Seeds_Properties` (T1) |
| B2 | OK gated on non-empty name + abbreviation | `Ok_GatedOnNameAndAbbr` (T1) |
| B3 | Two-way edits round-trip to the result payload | `Edits_RoundTrip` (T1) |
| B4 | RTL toggle persists | `Rtl_Toggles` (T1) |
| B5 | Duplicate/invalid tag rejected (validation message) | `DuplicateOrInvalidTag_Rejected` (T3) |
| B6 | Empty name ⇒ OK disabled | `EmptyName_DisablesOk` (T3) |

## Integration (T2)
- I1: Apply a paragraph+character style (the §19c host-supplied style list) to a
  field via the existing apply path, then confirm the run carries the style
  (existing §19c surface — this §19g slice does not re-implement the Styles
  manager; it verifies the apply lane the deferred manager would feed).
- I2: Create a writing system via the Add-WS core, then confirm the produced
  WS payload appears in a field's WS option list.

## Workflow (T4, real cache)
- W1: `EditReferenceDetails` round-trip — open the reference-details launcher
  against a real `ILexReference`, change name+note, commit, re-read the
  reference → values round-tripped.
- W2: Delete a reference via `LcmDeleteObjectLauncher` against a real cache →
  the target is removed (and CanDelete-gated when it cannot be).

## Visual (T5)
Per-dialog PNGs captured before `AssertNoCrowding`, READ and the six
subjective-quality questions answered:
`DeleteConfirmation-01-deletable`, `-02-not-deletable`, `-03-with-note`;
`LexReferenceDetails-01-seeded`, `-02-empty`;
`SpecialCharacter-01-initial`, `-02-filtered`, `-03-no-match`;
`WritingSystemProperties-01-seeded`, `-02-invalid`. Full-project run for
order-dependence.
