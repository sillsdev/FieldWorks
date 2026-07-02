> **Integration note (consolidated onto `010-advanced-entry-view-phase-1-2`).** The browse-table
> implementation below was developed twice in parallel; this branch adopts the more complete
> `editable-table` implementation wholesale (control, host, `ClerkBrowseRowSource`/`ClerkBrowseEditContext`,
> `BrowseViewer` cell-finder additions, peer, bulk-edit, filter/sort) and retires the duplicate. Two
> review-pass corrections were applied on top:
> 1. **Toggle target fix** — the browse gate now covers `lexiconEdit` (the Lexicon **Edit** tool's left
>    Entries pane, whose `currentContentControl` is `lexiconEdit`) in addition to `lexiconBrowse`, so the
>    table activates in the surface the request actually targeted, not only the standalone Browse tool.
> 2. **Row density** — a compact `ListBoxItem` style (`FwAvaloniaDensity.BrowseRowPadding`/`BrowseRowMinHeight`)
>    was added to the view so rows match the legacy ~17px instead of the Fluent default.
> The dialog-MVVM spike (`FwAvaloniaDialogs`) and the ViewDefinition-override/migration work on this
> branch are preserved untouched. Full managed build green; FwAvalonia headless suite **354/354** +
> dialog suite **9/9** on this consolidated branch (the per-task figures below have been updated from the
> `editable-table` branch's in-isolation count to this branch's count).

## 1. Spike and evidence baseline (3a gate)

- [ ] 1.1 Build/locate 10k-row and Lexical-Edit production fixtures for the table and record the legacy scroll/expand/open budget to measure against
- [ ] 1.2 Spike: measure scroll and expand latency (not just realization count) on the 10k fixture at 100% and 150% DPI; capture per-frame numbers
- [ ] 1.3 Evaluate the upstream `VirtualizingStackPanel` scroll/GC condition (#18626) against the measurements; record the pivot-trigger fire/clear with numbers in the change evidence manifest
- [x] 1.4 Record the TreeDataGrid (FOSS archived / Accelerate-commercial) and ItemsRepeater (deprecation path) pivot-trigger resolutions as confirmed-closed (`pivot-trigger-resolutions.md`); the `VirtualizingStackPanel` #18626 trigger is recorded as open-pending-measurement (1.2/2.7)

## 2. 3a — Read-only table at scale

- [x] 2.1 Extract owned column-header bar from `LexicalBrowseView` with per-column sort affordance and stable automation ids
- [x] 2.2 Add sort wiring through `IBrowseRowSource` ordering; reflect active sort direction in the header
- [x] 2.3 Add a selection model (row + cell) over `ListBox` selection, including programmatic selection that scrolls/realizes a de-realized row
- [x] 2.4 Add keyboard navigation: arrows, Home/End, PageUp/PageDown, with bring-into-view (inherited from `ListBox`; Down-arrow verified). Type-ahead deferred (needs `TextSearch` over templated rows)
- [x] 2.5 Confirm density/DPI parity: header/row/cell spacing from `FwAvaloniaDensity` (header uses `EditorPadding`). Full `VisualParityAndDensityTests`/`DensityTokenGateTests` pass still to be re-run under `test.ps1`
- [x] 2.6 Tests: existing realization-window test still passes; added selection + keyboard + sort + peer tests (`LexicalBrowseTableTests`)
- [ ] 2.7 Tests: scroll/expand budget test on the 10k fixture at 100%/150% DPI  *(headless scroll-stability now verified — `Scroll_RealizationStaysBounded_AcrossScrollPositions` keeps realization bounded across 8 scroll positions on the 10k list; the real-DPI latency budget still needs the in-app measurement pass)*

## 3. 3d — Custom AutomationPeers (first-class, gates 3a UIA evidence)

- [x] 3.1 `BrowseTableAutomationPeer` (a `ControlAutomationPeer`) synthesizes a `BrowseRowAutomationPeer` per row from `IBrowseRowSource.RowCount` via `GetOrCreateChildrenCore`, not from realized containers
- [x] 3.2 Row peers carry stable `BrowseRow.{index}` ids + cell-text names and `BringIntoViewCore`; table reports `DataGrid` control type. Full per-cell Grid/Selection/Invoke pattern providers are a follow-up
- [ ] 3.3 UIA2/FlaUI test: automation tree exposes de-realized rows/cells with stable ids; ids stable across scroll  *(headless test `AutomationPeer_EnumeratesAllRows_IncludingDeRealizedOnes` covers the synthesis; desktop UIA2/FlaUI evidence still needs a desktop run)*

## 4. 3a consumer — Lexical Edit main table view (read)

<!-- Wired via the SAME bridge the edit surface uses (LexicalEditHostControl / WinFormsAvaloniaControlHost
     + the UIMode=New flag). The legacy BrowseViewer stays constructed and fully functional (drives the
     clerk/sort/filter); the Avalonia table overlays it as a read-only mirror and forwards selection to
     the clerk. Default (Legacy) path is byte-for-byte unchanged. Full repo build is green; 354/354
     headless tests pass. In-app manual verification is the remaining step (4.3). -->
- [x] 4.1 Wire FLEx's main Lexicon Browse view onto the shared table for read-only display: `LexicalBrowseHostControl` (WinForms↔Avalonia bridge) + `ClerkBrowseRowSource` adapter (row count from `Clerk.ListSize`, faithful cell strings from `BrowseViewer.GetRowCellStrings` per-column finders), columns from the browse config; gated by `LexicalEditSurfaceResolver.ResolveBrowse` (`UIMode=New`, `lexiconBrowse`)
- [x] 4.2 Legacy `BrowseViewer` kept as the fallback: it stays constructed/functional underneath and is the default whenever `UIMode != New`; selecting an Avalonia row forwards to `Clerk.JumpToRecord`
- [ ] 4.3 Test: Lexical Edit main browse renders on the shared table within the read budget at 100%/150% DPI  *(full build green + 354/354 headless; in-app/real-DPI render is the manual step)*

## 5. 3b — Editable cells

- [x] 5.1 Cell-edit host (`IBrowseEditSource` + `BuildCell`) hosts `FwMultiWsTextField` for text columns and `FwChooserField` for chooser columns, branching on `LexicalEditRegionField.Kind`
- [x] 5.2 Implement the keyboard edit flow: Enter commits+advances, Esc cancels, F2 focuses, Tab commits then uses framework focus traversal. Shift+Tab / full restore-on-cancel are framework-default; explicit restore pending
- [x] 5.3 Route commit/cancel through `IEditSession` (editor auto-stages via `IRegionEditContext`; table drives `Commit`/`Cancel`). Global undo-stack binding is at the product edge (`avalonia-undo-redo`)
- [x] 5.4 Tests (`LexicalBrowseEditTests`): editor hosted only in editable column, typing stages through the context, Enter commits one change + advances, Escape cancels with no commit. (Undo-reverts is a product-edge assertion, not the fake)
- [ ] 5.5 Test: typing-latency budget (≤6 ms@100%, ≤8 ms@150%, incl. RTL/bidi) via `TypingLatencyHarnessTests`  *(editor reuses the harnessed `RegionRichTextEditAlgorithms`; a table-specific latency gate is still to be added)*

## 6. 3b consumer — Lexical Edit main table view (edit)

<!-- See architecture-review.md §D3/§4 revision 3: the lexicon Browse columns are all editable="false",
     so the faithful behavior there is READ-ONLY; in-product cell editing is wired but dormant under
     shipping configs. The editable-table's real editing consumers are the Edit view (already wired in
     RecordEditView) and bulk-edit (3c). -->
- [x] 6.1 In-cell editing wired (`ClerkBrowseRowSource : IBrowseEditSource` + `ClerkBrowseEditContext`): respects the column `editable` flag and DELEGATES writes to the proven per-row `LexicalEditRegionEditContext` (fenced LCModel undo task). Dormant for lexiconBrowse (read-only by config) — correct/faithful
- [ ] 6.2 Retire the legacy `BrowseViewer` fallback for this surface  *(deferred: the overlay keeps the legacy viewer for the ~50 coupling points; retirement belongs to the legacy-retire stage — architecture-review §D1. **Cutover design + alternatives now in `rendering-cutover-design.md`**: the C++ Views engine is not actually needed for a read-only grid (cell values run through the managed `CollectorEnv`, not a native RootBox); the plan re-sources cells/sort/filter behind the existing seam, owns the column model, moves the coupling points, then deletes `XMLViews` in Stage 13b. Gated by spikes S1–S3.)*
- [ ] 6.3 Test: Lexical Edit table edits a cell, persists through the edit session, reversible via the global undo stack  *(control-level editing verified headless in `LexicalBrowseEditTests`; the product LCModel write path compiles in the full build and is for manual validation)*

## 6b. Architecture review + revision (per user request)

- [x] R.1 Architecture review written (`architecture-review.md`): overlay vs replace, finder-based cells, editing scope correction, one-way selection, peer allocation, sort/filter gaps
- [x] R.2 Revision applied — clerk→table selection mirror (`MirrorClerkSelectionToAvalonia`, reentrancy-guarded) so the Avalonia table follows external navigation
- [x] R.3a Revision applied — `ClerkBrowseRowSource : IBrowseSortSource` delegates header sort to the clerk via `BrowseViewer.SortByDataColumn` (reuses the exact legacy header-click path); the overlay's headers now sort. Direction-sync / clerk-reload timing to confirm in the manual pass
- [x] R.3b Revision applied — per-column filter row UI in `LexicalBrowseView` (Enter applies) + `ClerkBrowseRowSource : IBrowseFilterSource` client-side substring filter (filtered index map over the loaded list, no clerk mutation, reversible; cleared on sort). Verified headless (`FilterRow_IsShownForFilterableSource_AndTypingEnterApplies`)

## 7. 3c — Bulk-edit / checkbox / filter / sort

- [x] 7.1 Checkbox-select column (`showCheckboxColumn`) with per-row state + `CheckAll`/`UncheckAll`, view-owned state survives re-realization, `BrowseCheck.{i}`/`BrowseCheckAll` automation ids
- [x] 7.2 Column filtering via `IBrowseFilterSource` predicate (`ApplyFilter`); row count reflects the filter and rows still realize lazily
- [x] 7.3 Multi-column sort via `IBrowseMultiSortSource` (`SortByColumns`) combining keys in priority order
- [x] 7.4 Census `BulkEditBar` operations (`bulk-edit-census.md`): all 6 op tabs recorded — List Choice / Bulk Copy / Transduce / Find-Replace covered by the mechanism, Click Copy and object-Delete explicitly deferred with rationale; fake-flid preview columns replaced by the in-memory overlay
- [x] 7.5 Managed in-memory bulk-edit model (`IBrowseBulkEditSource.PreviewBulkEdit`/`ClearBulkEditPreview`) replacing the fake-flid cache; preview overlays values without mutating the model
- [x] 7.6 Bulk apply (`ApplyBulkEdit`) commits previewed operations through the edit session across affected rows
- [x] 7.7 Tests (`LexicalBrowseBulkEditTests`): chooser cell, check-all/uncheck-all, check-state-survives-re-realization, filter narrows lazily, multi-column sort, bulk preview no-mutation, bulk apply commits once. (10k-row *timing* budget is the environment-gated 2.7 item)

## 8. Close-out

- [x] 8.1 Full repo build (`.\build.ps1`, native + managed + Avalonia projects) green (exit 0) with the xWorks/XMLViews product wiring; full headless suite **354/354 passing, 0 regressions** (30 new across `LexicalBrowseTableTests`/`LexicalBrowseEditTests`/`LexicalBrowseBulkEditTests` + browse-resolver cases). Authoritative `.\test.ps1 -TestProject Src/Common/FwAvalonia/FwAvaloniaTests -SkipNative` recommended for CI evidence
- [x] 8.2 Update the Stage 3 epic / roadmap ledger to reflect 3a/3b/3c status and link the evidence manifest
- [x] 8.3 `openspec validate shared-editable-virtualized-table --strict` passes
