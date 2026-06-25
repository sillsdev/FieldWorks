# §19a — Editable StText test-research note (T0)

The source for the §19a hardening tests (T2 integration, T3 edge, T4 workflow). It
derives the behavior/edge/workflow matrix from the legacy WinForms
`StTextSlice`/`StTextView` (`Src/Common/Controls/DetailControls/StTextSlice.cs`),
the StText data model (`IStText.ParagraphsOS` of `IStTxtPara` with `Contents`
[`ITsString`] + `StyleName`), and the §19a managed replacement
(`FwStructuredTextField` + `IRegionEditContext` paragraph CRUD +
`FullEntryRegionComposer.AddStructuredText`). Each row names the test that covers
it (traceability). Tests live in:

- **U** = `FwAvaloniaTests/StructuredTextFieldTests.cs` (existing T1 unit, headless)
- **A** = `xWorksTests/StructuredTextAdapterTests.cs` (existing T1 adapter, real LCModel)
- **I** = `FwAvaloniaTests/StructuredTextIntegrationTests.cs` (NEW T2 — combined realized surface)
- **E** = `FwAvaloniaTests/StructuredTextEdgeCaseTests.cs` (NEW T3 — edge cases)
- **W** = `xWorksTests/StructuredTextWorkflowTests.cs` (NEW T4 — end-to-end real-cache journeys)

## 1. StTextSlice behaviors × coverage

| # | Legacy StTextSlice / StText behavior | §19a managed surface | Covered by |
|---|--------------------------------------|----------------------|------------|
| B1 | RootSite edits the text of each paragraph (`StTxtPara.Contents`), one undo step per edit | per-paragraph `TextBox` → `TrySetParagraphText`, rides focus-loss autosave | U `Editable_RendersEditableRows_AndStagesAParagraphTextEdit`; A `ParagraphTextEdit_RoundTripsToLcm_AsOneUndoStep`; I `EditParagraphText_AndSiblingMultistring_AreEachOneUndoStep`; W `Workflow_EditDefinition_AddSecondPara_ApplyStyle_RoundTrips` |
| B2 | Create paragraphs (Enter splits/inserts; `StTextView` adds StTxtPara) | Enter at row end + per-row "+" → `TryInsertParagraph(after)`, immediate commit + re-show | U `EnterAtParagraph_StagesAnInsertAfterIt`; A `InsertParagraph_AddsAnEmptyParagraphAfterTheIndex_OneUndoStep`; I `AddParagraph_CommitsImmediately_AndRefreshDoesNotDisturbSibling`; W workflow |
| B3 | Destroy paragraphs (Backspace-merge; remove StTxtPara) | Backspace in empty row + per-row "×" → `TryDeleteParagraph(index)` | U `BackspaceInEmptyParagraph_StagesADelete_WhenMoreThanOneRemains`; A `DeleteParagraph_RemovesIt_OneUndoStep_ButNeverTheLastOne`; W `Workflow_DeleteParagraph_ThenUndoRestoresIt` |
| B4 | Per-paragraph named style (`StPara.StyleName`; legacy seeds "Normal") | per-row "Paragraph Style" picker → `TrySetParagraphStyle`; "Default" clears to Normal | U `ParagraphStylePicker_AppliesAStyle`; A `ParagraphStyle_AppliedAndCleared_OneUndoStep`; E `ClearStyle_MapsToNormal`; W workflow |
| B5 | `OnEnter` materializes a missing StText (MakeNewObject StText + one empty StTxtPara) inside a UOW | composer shows ≥1 empty editable row; first keystroke materializes via index-0 setter | E `EmptyStText_ShowsOneEditableRow`; A text-setter `while (count <= index) InsertNewTextPara` path (`ParagraphTextEdit...`) |
| B6 | StText always keeps ≥1 paragraph (the last cannot be deleted to nothing) | delete affordance omitted on the only row; setter + edit-context guard reject last delete | U `OnlyParagraph_HasNoDeleteAffordance_AndBackspaceDoesNotDelete`; A `DeleteParagraph_...ButNeverTheLastOne`; E `OnlyParagraph_CannotBeDeleted_ByButtonOrBackspace` |
| B7 | One global undo step shared with legacy surfaces; cancel rolls everything back | `Stage()` opens one fenced `LcmRegionEditSession`; Commit = one step, Cancel rolls back | A `Cancel_RollsBackEveryStagedParagraphEdit`; E `Cancel_RollsBack...` / `Commit_Persists...`; I undo-grouping asserts; W undo journey |
| B8 | Full TsString fidelity (runs, per-run WS, named char styles) preserved | `RegionRichTextAdapter.From/ToTsString` lossless round-trip; run metadata preserved | A `MultiParagraphRoundTrip_ProjectsEveryParagraphFromLcm`; E `RtlAndComplexScript_ParagraphStagesAndRoundTrips`; W round-trip |
| B9 | ORC / embedded-object content (§19c.3 — out of scope for editing) | ORC/lossy paragraph held read-only with tooltip; preserved losslessly | U `OrcParagraph_StaysReadOnly_AndEditableNeighborStaysEditable`; E `OrcParagraph_StaysReadOnly_WhileEditableParagraphsStillEdit` |
| B10 | Handlers torn down on slice recycle (RootSite dispose) | `IDisposable` detaches every wired handler + drops style flyout closures | U `Dispose_DetachesEveryWiredHandler` |

## 2. Edge cases × coverage

| # | Edge case | Expectation | Covered by |
|---|-----------|-------------|------------|
| C1 | Empty StText: zero paragraphs handed by composer | one empty EDITABLE row rendered; no crash | E `EmptyStText_ShowsOneEditableRow` |
| C2 | One-paragraph StText | renders one row; no delete affordance | E `OnlyParagraph_CannotBeDeleted_ByButtonOrBackspace` |
| C3 | Delete the only paragraph (button + Backspace) | rejected both ways; ≥1 paragraph kept | E `OnlyParagraph_CannotBeDeleted_ByButtonOrBackspace`; A `DeleteParagraph_...ButNeverTheLastOne` |
| C4 | RTL (Arabic) + complex-script (Khmer) paragraph content | stages + round-trips losslessly through the run model (FlowDirection note below) | E `RtlAndComplexScript_ParagraphStagesAndRoundTrips`; W (km lexeme baseline already in `Build_RichLexemeForm_...`) |
| C5 | Rapid interleaved insert/delete gestures | no orphaned undo step, no crash; each gesture isolated | E `RapidInterleavedInsertDelete_DoNotCrashOrOrphanUndo` |
| C6 | Cancel vs commit of a structural gesture | cancel rolls add/delete/style back; commit persists | E `Cancel_RollsBackParagraphAddDeleteStyle`; E `Commit_PersistsParagraphAddDeleteStyle` |
| C7 | ORC/lossy paragraph interleaved with editable paragraphs | ORC row read-only; editable neighbors still edit/stage | E `OrcParagraph_StaysReadOnly_WhileEditableParagraphsStillEdit` |
| C8 | Clear-style → Normal mapping (LCModel forbids null/empty StyleName) | "Default" picker entry / null style → `StyleServices.NormalStyleName` | E `ClearStyle_MapsToNormal`; A `ParagraphStyle_AppliedAndCleared_OneUndoStep` |
| C9 | Index one past the end (editor always shows ≥1 row over an unmaterialized StText) | text setter creates empty paragraphs up to the index | E `EmptyStText_FirstKeystrokeMaterializesParagraph` |

**FlowDirection note.** `FwStructuredTextField` does not itself set
`FlowDirection` on the per-paragraph `TextBox` (the same as the single-WS
`FwMultiWsTextField`, which leaves the box's flow to the framework/host). The RTL
test therefore asserts the load-bearing contract — RTL + complex-script content
**stages and round-trips losslessly** (no reordering/normalization corruption) —
and documents that explicit per-paragraph FlowDirection mirroring of the legacy
StVc paragraph direction remains a rendering-polish follow-up (not a data-safety
gap). This matches the established run-aware text path. No product change made.

## 3. User workflows × coverage

| # | Workflow | Covered by |
|---|----------|------------|
| WF1 | Edit a sibling multistring field AND a paragraph AND add a paragraph AND apply a paragraph style on ONE realized surface; assert each staged/committed, undo grouping correct, refresh after add/delete doesn't disturb the sibling | I (whole fixture) |
| WF2 | Edit Definition text → add a 2nd paragraph → apply a paragraph style to it → commit → re-show/reopen → verify two paragraphs + style round-tripped | W `Workflow_EditDefinition_AddSecondPara_ApplyStyle_RoundTrips` |
| WF3 | Delete a paragraph → undo restores it | W `Workflow_DeleteParagraph_ThenUndoRestoresIt` |

## Product issues found

See the final report. Any genuine product defect is reported precisely; only an
obvious one-line defect would be fixed under this TEST-only task, with a note.
