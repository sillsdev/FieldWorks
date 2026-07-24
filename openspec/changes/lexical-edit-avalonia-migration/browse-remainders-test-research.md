# Browse-Remainders (Phase-1 §19f) — T0 Test Research

The remaining BROWSE-table functional-parity items on the Avalonia browse surface.
Each item: WinForms behavior × edges × workflows → mapped tests. The browse already
ships all columns/sort/filters, checkbox-select, column config/width, every bulk-edit
tab, virtualization, density, the header context menu, and the per-row select column.

Architecture (do not reinvent): the **view** (`LexicalBrowseView`, FwAvalonia) is
LCModel-free and raises events; the **host control** (`LexicalBrowseHostControl`)
re-raises; the **product edge** (`RecordBrowseView`, xWorks) owns dialogs + LCModel +
mediator and routes back. Row data/commands flow through `IBrowse*Source` (impl:
`ClerkBrowseRowSource`) and the batch-fenced `ClerkBrowseEditContext` (one UOW per
destructive gesture). `icu.net` is referenced by xWorks, NOT by FwAvalonia — so the
collation/diacritic matcher (item 2) lives in `ClerkBrowseRowSource`, keeping the view
icu-free.

---

## 1. Right-click ROW context menu — FULL

**Product wiring caveat (xcut-review-2026-06-21.json).** The view/seam described below is
built and tested, but `ClerkBrowseRowSource` (the product row source `RecordBrowseView`
actually constructs) does not implement `IBrowseRowMenuSource` — only a test fake does. In
product the menu therefore has no host commands (Copy/Paste only); the "FULL" label above
covers the view-side seam, not end-to-end product wiring. Tracked as tasks.md 19i.3.

**WinForms truth.** `SimpleRootSite` fires `RightMouseClickedEvent`
(`FwRightMouseClickEventArgs(Point, IVwSelection)`, `EventHandled` settable) on RMB-down
and on the keyboard Menu key (SimpleRootSite.cs ~4594, ~4631). The host reads the
selection → row hvo + column, and pops an xCore menu by name; commands are resolved/
enabled by the app command infrastructure. No fixed command set is passed — the host
supplies it.

**Avalonia design.** The view gains a data-row right-click → raises
`RowContextMenuRequested(rowIndex, columnIndex)`. The host supplies the command set via a
new seam `IBrowseRowMenuSource.GetRowCommands(rowIndex)` → `IReadOnlyList<BrowseRowCommand>`
(LCModel-free: key + label + enabled). The view builds an Avalonia `ContextMenu` from the
commands and, on click, raises `RowCommandInvoked(rowIndex, commandKey)` → host routes to
the mediator. Empty/null command list → no menu shown.

**Tests.**
- T1: a data row with a command source builds a context menu with the supplied entries;
  clicking an entry raises `RowCommandInvoked` with (row, key); a source returning no
  commands builds no menu.
- T1: menu reflects per-row enabled flags (disabled item is disabled).
- T3 edge: right-click with no selection / empty list → no crash, no menu; right-click on
  a row whose source returns null.
- T2 integration: row-context-menu action composes with filter + multi-select.

## 2. Find/Replace P2 (diacritic / WS-collation matching) — FULL

**WinForms truth.** Bulk Replace + FilterBar "Filter For…" build an `IVwPattern`
(`VwPatternClass.Create()`) with `MatchDiacritics`, `MatchOldWritingSystem` (== match WS),
`MatchCase`, `MatchWholeWord`, `UseRegularExpressions`, `IcuLocale` (FilterBar.cs
1159-1184; FwFindReplaceDlg.cs). `MatchDiacritics=false` ⇒ ignore combining marks; WS
collation applied via `IcuLocale`. `icu.net` available: `Icu.Collation.Collator`,
`Icu.Normalization` (LgIcuCollator.cs uses `Icu.Collation`; XDumper uses
`Icu.Normalization`).

**Avalonia design.** P1 `ClerkBrowseRowSource.ComputeReplaced` ignores
`MatchDiacritics`/`MatchWritingSystem` (literal `LiteralReplace`). P2: when
`MatchDiacritics` is OFF (the legacy default — diacritic-insensitive), normalize both the
cell text and the find text by NFD-decomposing and stripping combining marks
(`Icu.Normalization`/`CharUnicodeInfo`), match on the stripped forms, and splice the
replacement back at the matched span of the ORIGINAL text (so the cell keeps its
diacritics outside the match). When `MatchWritingSystem` is requested with a WS locale,
use an ICU primary-strength `Collator` (`Icu.Collation`) for the equality test so
collation-equivalent forms match. Regex mode unchanged. The matcher is a pure managed
static so it unit-tests without a cache.

**Tests.**
- T1: diacritic-insensitive find matches an accented cell with an unaccented pattern and
  replaces only the matched run, preserving surrounding text; with `MatchDiacritics=true`
  it does NOT match.
- T1: `MatchCase` still honored under diacritic-insensitivity; whole-word still bounded.
- T1: WS-collation equality matches a collation-equivalent form.
- T3 edge: combining diacritics (base+U+0301), precomposed vs decomposed forms, an
  RTL/Khmer string (no spurious match), empty find, find longer than cell.

## 3. Transduce "Setup…" — FULL (already wired; verify + test)

**WinForms truth.** BulkEditBar Process tab "Setup…" launches `AddCnvtrDlg`
(`SIL.FieldWorks.FwCoreDlgs`), then `InitConverterCombo()` re-reads `EncConverters`,
filtering Unicode-to-Unicode (BulkEditBar.cs 4045-4076, 2496-2521).

**Avalonia state — DONE.** `IBulkEditBarHost.LaunchConverterSetup()`
(RecordBrowseView.cs ~1726) opens `AddCnvtrDlg` over the owning form and returns
`AvailableConverters()` (Unicode-to-Unicode filtered). `BulkTransduceTabViewModel.Setup()`
(BulkEditBarView.cs ~821) re-publishes the list preserving selection by name and clears
preview. Only TESTS are missing.

**Tests.**
- T1: `Setup()` over a host whose `LaunchConverterSetup` returns a new list re-publishes
  `Converters`, preserves selection by name when present, else first; clears preview.
- T1: a null launch result (cancel/unavailable) leaves the list untouched.
- T3 edge: Setup with no converters → empty list, `CanApply` false.

## 4. Cell copy / paste (Ctrl+C / Ctrl+V) — FULL

**WinForms truth.** Copy/paste route through `RootSiteEditingHelper`
(`OnEditCopy`/`OnEditPaste`, `CanCopy`/`CanPaste`) over the current selection's
`ITsString`; paste honors the edit context (flid/ws/editable). Editability gated by
`BrowseViewer.IsColumnEditable` (719-729).

**Avalonia design.** On a focused/selected cell, Ctrl+C copies the cell's display text to
the Avalonia clipboard (rich value when the rich source supplies it; plain text otherwise).
Ctrl+V on an editable cell pastes via the edit context: if the cell is editable
(`IBrowseEditSource.IsColumnEditable`), begin its editor and stage the clipboard text
through the shared `IRegionEditContext` (commit on the same Enter path); a non-editable
cell rejects the paste (no-op). Keep clipboard access in the view (Avalonia
`IClipboard`), the write through the edit context.

**Tests.**
- T1: Ctrl+C on a cell places its text on the clipboard.
- T1: Ctrl+V on an editable cell stages the clipboard text through the edit context.
- T3 edge: paste into a non-editable cell is a no-op; copy on an empty cell yields empty;
  paste with empty clipboard is a no-op.
- T2 integration: copy a cell, paste into another editable cell, commit, undo correct.

## 5. In-cell picture editing — FULL (closes §19d's PARITY §19f.3)

**WinForms truth.** Browse picture columns are editable in-cell; insert/replace/delete
reuse the picture-properties dialog. The §19d detail-view picture path ships fully; the
edit-context lane (`TryInsertPicture`/`TryReplacePictureFile`/`TryDeletePicture`/
`TrySetPictureMetadata`) is already present on `ClerkBrowseEditContext` (delegates to the
per-row context). The missing piece is the VIEW-side in-cell picture editor.

**Avalonia design.** When the edit source reports a picture column editable and supplies a
picture `LexicalEditRegionField`, the editable cell realizes the §19d picture field
control (via `RegionFieldControlFactory`, reusing `IRegionMediaServices` +
`RegionPictureDialogResult`) instead of a text editor. Insert (empty cell → pick file +
properties dialog → `TryInsertPicture`), replace (file pick → `TryReplacePictureFile`),
delete (`TryDeletePicture`) all route through the existing edit context as one UOW each.

**Tests.**
- T1: a picture-editable cell realizes the picture field control (not the text editor);
  insert/replace/delete route to the edit context.
- T3 edge: missing source file → no crash, no write; delete on an empty picture cell is a
  no-op.

## 6. Header drag-reorder — FULL

**WinForms truth.** `DhListView.AllowColumnReorder` + `ColumnDragDropReordered` →
`BrowseViewer.m_lvHeader_ColumnDragDropReordered` reorders `Vc.ColumnSpecs`, resets widths,
`UpdateColumnList`, syncs sort arrows (BrowseViewer.cs 3024-3049). Persistence flows through
`OrderForColumnsDisplay` → PropertyTable/clerk config. Today on the Avalonia browse, order
changes only via the Configure-Columns dialog.

**Avalonia design.** A header cell becomes drag-reorderable: pressing on the header label
and dragging past a neighbor raises `ColumnReordered(fromIndex, toIndex)`. The host
(RecordBrowseView) reorders the column model + persists via the existing
`BrowseColumnConfigStore`/`BrowseColumnModel` (the same store the Configure dialog writes),
then rebuilds the view (`RebuildColumns`) — preserving checked set + selection. The width
GridSplitter handles already exist on the trailing edge; the reorder grab is the header
body, so the two gestures don't collide.

**Tests.**
- T1: dragging a header from index i past index j raises `ColumnReordered(i, j)`.
- T1: the host reorder helper produces the new field order and persists it.
- T3 edge: reorder to first / to last; a no-op drag (drop on self) raises nothing.
- T4 workflow: reorder → persists across a view reopen (store round-trip).
- T5 visual: reordered headers PNG.

## 7. RDE (Rapid Data Entry) — CORE + PARITY note

**WinForms truth.** `XmlBrowseRDEView` shows a virtual "new row" (khvoNewItem
= -1234567890) whose cell text lives in fake flids; Enter on a row with the minimum data
runs `CreateObjectFromEntryRow` in an `UndoableUnitOfWorkHelper.Do`, reflection-invoking
`{EditRowClass}Factory.{EditRowSaveMethod}(rootHvo, columns, ITsString[])` → new hvo, then
appends to the list and (post-UOW) runs an optional `RDEMergeSense`-style merge.

**Avalonia design.** Ship a CORE managed RDE row model: a `BrowseRdeRowModel` holding the
new-row cell strings (LCModel-free), a new-row affordance at the bottom of the table, and a
`NewRowCommitRequested(values)` event. The host commits via a new seam
`IBrowseRdeSource.CommitNewRow(IReadOnlyList<string> values, IRegionEditContext)` → returns
the new row's hvo (the product edge does the factory/UOW). Editable columns mirror the
column `editable` attribute. Deep RDE semantics (multi-field RDE templates, the post-UOW
merge pass, the per-tool RDE column subset) are scoped with a precise
`// PARITY §19f` note — the core new-row entry is the common Collect-Words case.

**Tests.**
- T1: the RDE row model accumulates per-column values; `CommitNewRow` clears the row and
  raises the commit with the typed values; empty row does not commit.
- T3 edge: empty RDE row (no commit), first-row commit.
- T4 workflow (real cache): type a new entry row → commit → it appears in the list.

## 8. Per-cell UIA peers — FULL (realized cells) + PARITY note

**WinForms truth.** No per-cell `IAccessible` in the legacy browse — only container
`AccessibleName`s and header column text. So this is an IMPROVEMENT; parity floor is low.
Today the Avalonia browse exposes a table-level `DataGrid` peer + synthesized per-ROW peers
(`BrowseRow.{i}`), but no per-cell name/value.

**Avalonia design.** Give each realized cell a custom `AutomationPeer` exposing a Name of
"{columnLabel}: {cellText}" and the `DataItem`/`Text` control type, so a screen reader
announces column + content. The synthesized de-realized per-row peers stay as-is;
virtualization-aware per-cell peer recycling for de-realized rows is scoped with a precise
`// PARITY §19f` note (the realized window is what a screen reader navigates).

**Tests.**
- T1: a realized cell's peer Name includes the column label and the cell text.
- T3 edge: empty cell peer (label + empty); after a virtualized scroll the realized cells
  still expose correct peers.

## 9. Print / Export — managed CSV FULL + PARITY note

**Product wiring caveat (xcut-review-2026-06-21.json).** `ExportVisibleCsv()` and its host
hook are implemented and tested, but `RecordBrowseView` has no Export/Csv menu, toolbar, or
command wiring to reach them — zero product callers, so the export is unreachable from the
live app today. "FULL" above describes the computation + host bridge, not a user-reachable
trigger. Tracked as tasks.md 19i.11.

**WinForms truth.** Export is shell-level (`RecordClerk.OnExport` → `ExportDialog`/
`NotebookExportDialog`, ConfiguredExport engine → XHTML/XML). Print is
`SimpleRootSite.OnPrint` (OS print of the root box). These are genuinely shell/Phase-2.

**Avalonia design.** Ship a managed CSV export of the VISIBLE columns/rows: a pure
`BrowseCsvExporter.ToCsv(headers, rows)` (RFC-4180 quoting) on the view's current
columns/cells, surfaced via `LexicalBrowseView.ExportVisibleCsv()` + a host hook. OS print
integration and the full legacy ConfiguredExport are scoped with a
`// PARITY §19f → avalonia-end-game` note.

**Tests.**
- T1: CSV of headers + visible cells; quoting of commas/quotes/newlines.
- T3 edge: empty table (header-only CSV), large table (uses current row count), a cell
  with embedded quotes/commas/newline.

---

## Integration / Edge / Workflow (T2–T5) at the §19f category level

- **T2** (one realized `LexicalBrowseView`): filter → multi-select → bulk-edit + row
  context-menu action + copy/paste compose; checked set survives; undo correct.
- **T3**: every item's edge bullets above.
- **T4** (real cache): "filter → select → row-context-menu/bulk delete → undo"; "RDE: type
  new row → commit → appears"; "header reorder → persists across reopen".
- **T5** (PNG stages, then `AssertNoCrowding`, READ each): row context menu open, RDE row,
  in-cell picture edit, reordered headers.
