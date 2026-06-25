# XMLViews Table Semantics vs Typed IR vs Avalonia Table Path (Task 7.2)

> Comparison of the legacy XMLViews browse/table stack against (a) the typed view-definition IR
> (`Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionModel.cs`) and (b) the planned Avalonia
> table path per `control-selection-matrix.md` (FieldWorks-owned virtualized table over
> `ListBox`/`VirtualizingStackPanel` primitives). Every legacy claim carries a verified
> file:line citation against the live branch. Feeds the 7.1 build iterations.
> Date: 2026-06-09.

All legacy paths are relative to `Src/Common/Controls/XMLViews/` unless otherwise noted.
Abbreviations: **BV** = `BrowseViewer.cs`, **XBVB** = `XmlBrowseViewBase.cs`,
**VC** = `XmlBrowseViewBaseVc.cs`, **FB** = `FilterBar.cs`, **BEB** = `BulkEditBar.cs`,
**DDC** = `XMLViewsDataCache.cs`, **LF** = `LayoutFinder.cs`.

## Legacy XMLViews table semantics (inventory)

The legacy browse surface is a composite: `BrowseViewer` (the `XCoreUserControl` shell,
BV:170) owns a `DhListView` header (BV:196, BV:914), an optional `FilterBar` (BV:205,
BV:995–1005), an optional `BulkEditBar` (BV:209, BV:1012–1015), and the actual rows in
`XmlBrowseView : XmlBrowseViewBase : RootSite` (XBVB:28) rendered by the native Views engine
via `XmlBrowseViewBaseVc : XmlVc` (VC:36).

### 1. Column definition source

- Columns are authored as `<column>` children of a `<columns>` element in the tool/area
  configuration XML, e.g. `DistFiles/Language Explorer/Configuration/Lexicon/areaConfiguration.xml:84–91`:
  `<column label="Headword" sortmethod="FullSortKey" ws="$ws=best vernoranal" editable="false" width="68000" layout="EntryHeadwordForFindEntry"/>`.
- The VC computes the **possible** column pool from the spec via
  `PartGenerator.GetGeneratedChildren(m_xnSpec.SelectSingleNode("columns"), ...)` (VC:596–602),
  which also **generates columns for custom fields** from the metadata cache, keyed by the
  list-items class (`ListItemsClass`, VC:608–637; from the sort-item provider or the mandatory
  `listItemsClass` attribute, VC:619–626).
- **Default active set** = possible columns with `visibility="always"` (VC:212–216);
  `visibility="menu"` columns stay in the pool, available from the header menu/config dialog.
- **Per-user column set persistence**: the active column list is saved as an XML string in the
  `PropertyTable` under a per-tool key (`ColListId`, VC:103–111; saved in
  `BrowseViewer.UpdateColumnList`, BV:3022–3074) with a schema version
  (`kBrowseViewVersion = 20`, BV:2972) and an explicit **version-migration ladder** on load
  (`GetSavedColumns`, VC:270–309 and onward). Hidden-column tracking distinguishes
  "removed by user" from "new column added by an upgrade" via `<hidden label=.../>` sentinels
  (save BV:3046–3072; load VC:233–264). Columns marked `doNotPersist="true"` suppress saving
  (BV:3030–3031); `normalLayout` is persisted in place of a special edit layout (BV:3032–3040).
- **Column-spec attribute vocabulary** (the table-semantics surface area):
  - `label` / `originalLabel` (identity + custom-field relabel, VC:653–658, VC:642–668)
  - `width` in millipoints (default-width selection comment VC:211; example fixtures
    VC:287–301; live spec areaConfiguration.xml:84)
  - `ws` with `$ws=` magic values (`vernacular`, `analysis`, `best vernoranal`, `reversal`, …)
    resolved per row via `WritingSystemServices.GetWritingSystem`/`FindWsParam`
    (FB:983–989, VC:1045–1053)
  - `layout` (cell content = named part/layout, LF:84) or inline view content under the column
    node (VC:1146–1151)
  - `sortmethod` (C# sort-key method on the model object → `SortMethodFinder`, LF:85–91) and
    `sortType` (`integer`, `date`, `genDate`, `YesNo`, `stringList`, `occurrenceInContext` →
    specialized finders, LF:92–115)
  - `cansortbylength` (BV:3230–3234)
  - `editable`, `transduce`, `commitChanges`, `editif` (in-cell editability, VC:1471–1517)
  - `blankPossible`, `multipara` (filter combo seeding, FB:964–977; multipara also switches
    cell flow, VC:1132–1140)
  - `bulkEdit` / `chooserFilter` (list-choice filter + bulk-edit target wiring, FB:991–993,
    BEB:672)
  - `common` (auto-add of new shipped columns to upgraded saved sets, VC:256–264)
  - view-level (not per-column) attributes: `selectColumn` (check-box column, VC:562–576) and
    `convertDummiesInView` (XBVB:481–484), `disableConfigButton` (BV:1053).

### 2. Cell rendering

- Each row is rendered as a **one-row Views table per object**: `AddTableRow` opens
  `vwenv.OpenTable(colCount, …)` with per-column `VwLength` widths taken from the live header
  (VC:901–1012; widths VC:938, sourced from `BrowseViewer.GetColWidthInfo` converting header
  pixel widths to point-1000 `VwLength`s at current DPI, BV:2207–2228).
- `AddTableCell` (VC:1038–1181) per cell:
  - resolves the column's **writing system per row** (`GetBestWsForNode`, VC:1045) and derives
    **RTL alignment** (VC:1055–1072) and paragraph direction (VC:1105–1110); audio (voice)
    writing systems force the cell non-editable (VC:1074–1076);
  - wraps content in a paragraph unless `multipara="true"` (VC:1132–1140);
  - when a sort-item provider exists, the cell content is produced through the row's
    `IManyOnePathSortItem` **path** (`DisplayCell`, VC:1159–1168 and VC:1410–1439) so a "row"
    can represent a child object (e.g. a sense) with cell content pulled from anywhere on the
    path; otherwise the column node's children are processed directly (VC:1142–1158);
  - a column-level forced WS (`m_wsForce`, VC:78–85, set in VC:1148/1421) overrides multistring
    WS resolution inside the cell.
- **Whole-column RTL order**: `m_fShowColumnsRTL` reverses column iteration (VC:92, VC:999–1008).
- **Row chrome**: border color, selected-row background/border highlight per
  `SelectionHighlighting` mode (VC:903–935), tentative-color constant for RDE (VC:51).
- Cell hotlinks launch FW links (`DoHotLinkAction`, VC:1758–1781); embedded ORC objects are
  suppressed (VC:1750–1752).
- **In-cell editing** is real Views editing on the *current row only* (`SetCellProperties`
  gates editability to `rowIndex == SelectedIndex`, VC:1243–1248): editable when
  `editable="true"`, or `transduce` present and not `editable="false"` (optionally guarded by
  an `editif` method probe via reflection), and never when `commitChanges` is set
  (`AllowEdit`, VC:1471–1517). Click-copy mode forces exactly one editable column
  (`OverrideAllowEditColumn`, VC:737, VC:908–912, VC:1481–1482). A separate rapid-data-entry
  subclass (`XmlBrowseRDEView.cs`, VC fragment `kfragEditRow` VC:46) supports row-entry editing.

### 3. Sorting

- Sorting is **not done by the view**: the browse surface only *builds and publishes* a
  `RecordSorter`; the record clerk/list applies it (`RecordSorter.Sort/MergeInto` contract,
  `Src/Common/Filters/RecordSorter.cs:276–284`).
- Header **left click** picks the column's `FilterSortItem.Sorter` and raises `SorterChanged`
  (BV:2277–2328 via `SetAndRaiseSorter`, BV:716–722); clicking the active column **reverses**
  it (BV:2293–2298, BV:2330–2349); **Shift+click builds a multi-column `AndSorter`**
  (BV:2299–2325).
- Column sorters are `GenRecordSorter(new StringFinderCompare(finder, new WritingSystemComparer(ws)))`
  (FB:796–807); the finder comes from `LayoutFinder.CreateFinder`, which dispatches on
  `sortmethod` → `SortMethodFinder`, `sortType` → `IntCompareFinder`/`OccurrenceInContextFinder`/
  plain `LayoutFinder`, else plain `LayoutFinder` over the cell layout (LF:81–122). Sort keys
  are strings produced per `IManyOnePathSortItem` (`SortStrings`, LF:283; SortMethodFinder
  key walk LF:514–656), compared with ICU writing-system collation.
- **Sort arrows** in the header reflect the active (possibly compound) sorter with large/medium/
  small arrows for precedence (`SyncSortArrows`, BV:737–788; `DhListView.ShowHeaderIcon`,
  `DhListView.cs:696`).
- Extra sort modes per column: **sort from end** (BV:2358–2456) and **sort by length**
  (`cansortbylength`, BV:3215–3236), surfaced as xCore commands.
- Sorting requires the filter bar to exist (BV:2279–2280); `InitSorter` reconciles a persisted
  clerk sorter with current columns on load/column change (BV:1188–1264).

### 4. Filtering

- The `FilterBar` renders **one `FwComboBox` per active column**, aligned under the header
  columns (`MakeOrReuseItems`, FB:560–585; `SetColWidths`, FB:703–731).
- Combo contents are seeded from column attributes (`MakeCombo`, FB:954–1077): Show All;
  Blanks/Non-blanks (`blankPossible`, FB:964–971); line-count matchers (`multipara`,
  FB:972–977); `sortType`-specific presets — integer zero/ranges + Restrict dialog
  (FB:996–1012), date Restrict (FB:1013–1021), Yes/No (FB:1022–1029), `stringList`
  exact/exclude matchers (FB:1030–1047); spelling-errors matcher when a dictionary exists
  (FB:1048–1054); **"Filter for…"** free-text `FindComboItem` (FB:1056) supporting match
  patterns; and a **list-choice chooser filter** when `bulkEdit`/`chooserFilter` is set
  (FB:991–993, FB:1058–1061).
- A cell filter is `FilterBarCellFilter(finder, matcher)` (FB:1567,
  `Src/Common/Filters/RecordFilter.cs:2396`); multiple active cells combine into an
  `AndFilter` (`RecordFilter.cs:2559`). Filters accept/reject rows by `IManyOnePathSortItem`
  (`RecordFilter.Accept`, `RecordFilter.cs:211`) and are applied by the clerk, not the view:
  the bar raises `FilterChanged` which `BrowseViewer` forwards (BV:244, FB:944–948) and the
  clerk persists.
- On reload/column change the bar **re-binds persisted clerk filters to combos**
  (`UpdateActiveItems`/`ActivateCompatibleFilter`, FB:593–658; finder identity via
  `SameFinder`, FB:685–690), and removes user-visible filters that no longer match a column
  (FB:653–657).
- Filter combos carry stable nonlocalized automation IDs (`FilterCombo.*`, FB:1075–1098) —
  already baselined by `WinFormsUiaSmokeTests` (task 2.4).
- Bar height adapts to writing-system font sizes (FB:629–635).

### 5. Selection model

- **Single current row** (`SelectedIndex`, XBVB:263 ff.) with `SelectedIndexChanged` events
  (XBVB:45, XBVB:354–355); the selected object is `m_sda.get_VecItem(m_hvoRoot, m_fakeFlid,
  SelectedIndex)` (XBVB:230–236). There is **no multi-row selection**; multiplicity comes from
  the check-box column (below).
- Row highlight modes `all`/`border`/`none` (`SelectionHighlighting`, XBVB:894–902; chosen from
  editability/ReadOnlySelect, XBVB:1582–1591), repainted by faking a `PropChanged` on a
  special per-row tag (`TagMe`, VC:581–587, XBVB:928–933) so only the affected rows re-lay out.
- Views text selections map back to a row index (`HandleSelectionChange`, XBVB:1966–1987;
  `GetRowIndexFromSelection`, XBVB:663); clicks select a row, and in `ReadOnlySelect` mode the
  click is intercepted without installing an editing selection (`XmlBrowseView.OnMouseUp`,
  `XmlBrowseView.cs:146–219`); Up/Down arrows move the selected row in read-only mode
  (`XmlBrowseView.cs:116–139`); selection is scrolled into view (`DoSelectAndScroll`,
  XBVB:1684–1697; `MakeSelectionVisible`, XBVB:2208–2210).
- Selection survives reconstruct/scroll via saved scroll/selection state
  (XBVB:487–493, `XmlBrowseViewSelectionRestorer.cs`).

### 6. Check-box bulk-edit columns

- The check column is enabled by `selectColumn="true"` on the view spec (`SetupSelectColumn`,
  VC:562–576) and rendered as a leading cell with an **integer-property picture** bound to the
  decorator tag `ktagItemSelected` (`AddSelectionCell`, VC:1314–1362; checked/unchecked picture
  selection in `DisplayPicture`, VC:1676–1688); a disabled picture is shown when
  `ktagItemEnabled` is off (VC:1334–1351).
- Check state, enabled state, preview values, and the active preview column live in a
  **decorator SDA**, not the model: `XMLViewsDataCache` defines `ktagItemSelected` (90000000),
  `ktagItemEnabled`, `ktagActiveColumn`, `ktagAlternateValue`, `ktagAlternateValueMultiBase`
  (DDC:39–71) with default-checked semantics (DDC:34–39).
- Clicking the check cell toggles it through normal Views editing of the int property
  (`XmlBrowseView.cs:204–210` routes the click; editability forced on in VC:1340–1358);
  changes raise `CheckBoxChanged(hvosChanged)` (BV:253, BV:421–437).
- **Check-all header button** with CheckAll/UncheckAll/Toggle menu (BV:1032–1045,
  BV:3356–3367; `ResetAll`, BV:3548–3569), `AllItems` enumeration (BV:3377) and programmatic
  `SetCheckedItems` (BV:3424).
- The **BulkEditBar** consumes the checked set: operation tabs List Choice, Bulk Copy, Click
  Copy, Process (transduce), Find/Replace, Delete (BEB:52–56, BEB:55, BEB:120), with per-tab
  enable flags from XML (BEB:260–270); targets are declared by `bulkEditListItemsClasses`
  (BEB:206–211) and **ghost fields** via `GhostParentHelper` (BEB:214–219; `GhostParentHelper.cs`).
- **Preview semantics**: Preview/Apply buttons (BEB:106–108, BEB:282–283); the preview writes
  alternate values into the decorator and marks the active column; the VC then renders
  original + arrow + alternate as three inner piles in the cell (VC:1112–1128 layout comment,
  `AddPreviewPiles`/`AddAlternateCellContents`, VC:1270–1312; per-cell active test
  VC:1078–1094; RTL arrow VC:1289–1297; multi-column preview via
  `ktagAlternateValueMultiBase + icol`, VC:1304–1311).

### 7. Header behavior

- The header is a real WinForms `ListView` in details mode used **only as a header**
  (`DhListView`, `DhListView.cs:21`; BV:914–958; `Scrollable = false`, BV:957; not a tab stop,
  BV:958).
- **Resize**: dragging persists per-column pixel widths in the `PropertyTable` under
  `{tool}_{view}Column_{i}_Width` keys (`SaveColumnWidths`/`FormatColumnWidthPropertyName`,
  BV:2183–2199); `AdjustColumnWidths` pushes new `VwLength`s into the root box and the filter
  bar (BV:2131–2156); minimum column width 25 px (`kMinColWidth`, `DhListView.cs:607`);
  proportional fill on first layout (`MaximizeColumnWidths`, BV:2233–2251).
- **Reorder**: drag-and-drop column reordering (`AllowColumnReorder`, BV:955;
  `ColumnDragDropReordered` event, BV:954, `DhListView.cs:46,174`), with a display-order
  mapping (`OrderForColumnsDisplay`, BV:2282).
- **Column choosing**: header right-click menu (BV:953) and the blue-arrow configure button
  (BV:1052–1076) open the column menu / `ColumnConfigureDialog.cs`; changes flow through
  `UpdateColumnList` (BV:2977–3075) which rebuilds filter bar, sorter, bulk-edit bar, and
  persists the set.
- **Sort arrows** drawn into header icons (BV:776–788, `DhListView.cs:696`).

### 8. Virtualization / lazy behavior

- Rows are **lazy by construction**: the root display adds the whole list as a lazy vector
  (`vwenv.AddLazyVecItems(m_fakeFlid, this, kfragListItem)`, VC:1643–1651); the native Views
  engine materializes rows on scroll using the VC's `EstimateHeight` (a flat 17-point guess,
  VC:1740–1743). `LoadDataFor` is a no-op because all data is already in the SDA decorator
  (VC:1699–1703).
- Row identity is a **fake flid** (`m_fakeFlid`, VC:57) published by the record list through
  the `XMLViewsDataCache` decorator over `DomainDataByFlid` (DDC:24); each lazy item indirects
  through `TagMe` (`kfragListItem` → `AddObjProp(m_tagMe, …, kfragListItemInner)`,
  VC:1653–1658) so single rows can be invalidated cheaply.
- Dummy-to-real object conversion can be deferred to paint time
  (`ShouldConvertDummiesInView`, XBVB:481–484; `InOnPaint` handshake, VC:1705–1719,
  XBVB:1952–1957).
- The view participates in `IVwNotifyChange` so model edits repaint affected rows (XBVB:28);
  scroll-range adjustments are specially handled for the table layout (XBVB:504 ff.).

## What the typed IR expresses today vs gaps

Reference: `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionModel.cs`.

### Expressed today

- `ViewNodeKind` covers exactly the **detail-view** vocabulary: `Field`, `Group`,
  `ObjectAtom`, `Sequence`, `CustomFieldPlaceholder` (ViewDefinitionModel.cs:19–35). There is
  **no table/column/row kind**.
- `ViewNode` carries: `StableId`, `Label`, `Abbreviation`, `Field`, `RawEditor` +
  `EditorClassification`, `WritingSystem` (string, `$ws`-style), `Visibility`
  (`Always`/`IfData`/`Never`, :37–48), `Expansion`, `Indented`, `TargetLayout`, `Children`,
  plus task-4.7 metadata `LocalizationKey`, `AutomationId`, `SurfaceRouting`
  (ViewDefinitionModel.cs:150–232).
- `ViewDefinitionModel` carries `ClassName`/`LayoutName`/`LayoutType`, roots, and a
  **diagnostics channel** with stable codes and node paths (:121–143, :238–262), plus the
  deterministic `ToSnapshot` baseline format (:269–313).
- The import pipeline (`XmlLayoutImporter`, `ViewDefinitionCompiler`, `ViewDefinitionCache`,
  `LayoutImportCoverage`) handles **detail layouts only**; nothing in
  `Src/Common/FwAvalonia/ViewDefinition/` parses a `<columns>`/`<column>` browse spec (verified
  by search across the folder — the words column/table/sort/filter occur only in coverage-report
  bookkeeping and importer comments).

Several legacy table concepts have *near* analogues worth reusing rather than duplicating:
a `<column layout="X">` is shaped like a `Field`/`ObjectAtom` with `TargetLayout`; generated
custom-field columns parallel `CustomFieldPlaceholder` (both ride `PartGenerator`-style
expansion); `multipara` cells are `Sequence`-shaped; the diagnostics/`StableId`/snapshot
infrastructure applies unchanged to a table definition.

### Gaps (precise)

1. **No table container kinds.** Nothing represents "a browse view over listItemsClass C with
   columns [...]": no `Table`/`Column` `ViewNodeKind`, no `listItemsClass` slot on
   `ViewDefinitionModel`.
2. **Visibility vocabulary mismatch.** Browse columns use `visibility="always"|"menu"`
   (default-active vs available-in-pool, VC:212–216); IR `ViewVisibility` is
   `Always`/`IfData`/`Never` — `IfData` has no browse meaning and "menu" (pool membership) is
   inexpressible.
3. **No column presentation metadata**: `width` (millipoints + per-user pixel persistence),
   `common`, `originalLabel`, `doNotPersist`, `normalLayout`, column order / RTL column order.
4. **No sort metadata**: `sortmethod`, `sortType`, `cansortbylength`, sort-from-end, default
   sorter, multi-sort precedence. The IR has no concept that a node yields a *sort key*.
5. **No filter metadata**: `blankPossible`, `multipara` (as filter affordance),
   `sortType`-driven preset matcher sets, `bulkEdit`/`chooserFilter` list-choice filters —
   i.e., no way to declare what filter UI a column offers.
6. **No editability/bulk-edit metadata**: `editable`, `transduce`, `commitChanges`, `editif`,
   `selectColumn`, `bulkEditListItemsClasses`, ghost-field declarations (ghost state was
   explicitly deferred in 4.1; the browse path makes it load-bearing via `GhostParentHelper`).
7. **No row-identity/path concept.** Legacy rows are `IManyOnePathSortItem`s (an object plus
   the ownership path it was reached by), which is what makes senses-as-rows, path-aware cell
   display (VC:1410–1439), sorting (LF:283), and filtering (`RecordFilter.cs:211`) coherent.
   The IR has no row/path abstraction at all (it describes one object's detail view).
8. **No persistence contract** for the user-modified column set / widths / version migration
   (`kBrowseViewVersion` ladder). The 9.1 canonical-format design covers detail overrides keyed
   by `StableId`; browse column sets are a second, currently unmodeled override family.
9. **`WritingSystem` is carried but unresolved** — fine for detail slices, but table semantics
   need the resolved-per-row "best" behavior (VC:1045) and the audio/RTL consequences to be a
   *contract* (per-cell WS resolution rule), not an importer string.

## Avalonia mapping (per `control-selection-matrix.md`)

Target per the matrix §"Browse/table view": **FieldWorks-owned virtualized table — flattened
row list over `VirtualizingStackPanel`, owned shared-scope column header bar, owned cell
layout (uniform column grid via lightweight panel), stock `ListBox` selection** — with
`TreeDataGrid` rejected on licensing/editing/automation and named as the pivot option.

Disposition legend: **maps cleanly** (stock primitive or existing seam covers it) /
**needs IR extension** (new typed metadata per gaps above) / **needs owned-control feature**
(code we write in the owned table) / **stays legacy-side during coexistence**.

| Legacy semantic | Avalonia disposition |
|---|---|
| Column definition from XML + custom-field generation (VC:596–602) | **Needs IR extension** (table/column kinds, gap 1–3) + importer lane for `<columns>`; custom-field columns reuse the `CustomFieldPlaceholder` expansion pattern from 4.10. |
| Default-vs-menu column pool, user column set, version migration (VC:212–265, BV:3022–3074) | **Needs IR extension** (visibility vocabulary, gap 2) + a persistence decision shared with 9.1 (gap 8). Reading the legacy `ColListId` saved XML at import keeps user sets during coexistence. |
| Row = `IManyOnePathSortItem` path (VC:1410, LF:283) | **Needs IR extension** (row/path model, gap 7) — the owned table's items source should be path-aware row models, mirroring "flatten in the model". |
| One-row-table cell rendering, per-cell WS/RTL/font (VC:1038–1181) | **Maps cleanly** to owned row templates (matrix: full `DataTemplate` control is the design center); per-cell grid is the owned lightweight panel. Rich TsString cells are **gated by 6.13**; plain-text multi-WS cells work today. |
| Mixed row heights (`multipara` cells) under virtualization | **Maps with risk**: `VirtualizingStackPanel` estimated-size behavior is pivot trigger 2 in the matrix; validate against 10k-row fixtures (7.7). Legacy's own estimate is a flat 17 pt (VC:1740–1743), so parity does not require per-row estimation accuracy. |
| Lazy row realization (VC:1650) | **Maps cleanly** — `VirtualizingStackPanel` realization replaces `AddLazyVecItems`; data is already fully in memory in legacy too (VC:1699–1703), so no async-data story is required for parity. |
| Single current row + highlight + scroll-into-view (XBVB:263, 894–902, 1684) | **Maps cleanly** — stock `ListBox` selection + `BringIntoView`; current-record sync rides the existing 3.12 `IRecordNavigationContext` bridge. |
| Header: resize/reorder/sort arrows/column menu (BV:914–958, 2131–2199, 2277) | **Needs owned-control feature** — the owned header bar (matrix decision). Persist widths via the same property keys during coexistence (BV:2193–2199) or the new format per gap 8. |
| Sort: header click → `RecordSorter` to clerk (BV:2277–2328) | Split: sort-affordance metadata **needs IR extension** (gap 4); arrow UI **needs owned-control feature**; the *sorting itself* **stays legacy-side during coexistence** — the owned table should publish a `RecordSorter` through the clerk exactly as `BrowseViewer` does, since sorted order is clerk state shared with other surfaces. |
| Filter bar: per-column matcher combos (FB:954–1077) | Split: filter-affordance metadata **needs IR extension** (gap 5); the filter row UI **needs owned-control feature** (combos/flyouts per 6.3); matchers/`FilterBarCellFilter`/`AndFilter` **stay legacy-side** (LCModel-free `Filters` assembly consumed through a seam) so clerk filter state remains shared. Keep the `FilterCombo.*` automation-ID contract (FB:1080–1098) for the 2.4 baselines. |
| Check-box column + decorator tags (VC:562–576, DDC:39–73) | **Needs owned-control feature** (a real checkbox cell — simpler than int-prop pictures) + **IR extension** for `selectColumn` (gap 6). Check-state storage should move from decorator-SDA tags to a managed selection-set service (matrix "Managed selection"), bridged to `XMLViewsDataCache` only if a legacy `BulkEditBar` must drive an Avalonia table during coexistence. |
| Bulk edit bar, preview arrows, apply (BEB, VC:1270–1312) | **Stays legacy / out of first scope.** Bulk edit is a workflow on top of the table (own tabs, ghost handling, undoable apply). First Avalonia table iterations should target read/select/sort/filter browse parity; bulk-edit surfaces remain on the legacy `BrowseViewer` via explicit fallback (6.12 pattern) until a dedicated change. |
| In-cell editing (`editable`/`transduce`, VC:1471–1517) | **Gated by 6.13** (TsString editor foundation) + **IR extension** (gap 6). Not needed for first browse parity: most shipped columns are `editable="false"` (areaConfiguration.xml:84–91). |
| Click copy (XmlBrowseView.cs:177–199) | **Stays legacy / out of first scope** — bulk-edit-tab workflow. |
| Row repaint on model change (XBVB:28 `IVwNotifyChange`) | **Maps cleanly** — the 3.15 `AvaloniaRegionRefreshController` pattern generalizes: subscribe the notify bus, re-resolve affected row models. |

## Gaps list feeding 7.1 iterations

Ordered so each iteration of 7.1 (virtualized table path) has a concrete contract to build against:

1. **IR: table vocabulary.** Add `Table` (or a `TableDefinitionModel` peer) + `Column` node
   kinds carrying: label/originalLabel, width hint, ws spec, target layout, visibility
   (`Active`/`Pool` — fix the gap-2 vocabulary), sort metadata (`sortmethod`, `sortType`,
   `cansortbylength`), filter affordances (`blankPossible`, `multipara`, preset family,
   list-choice ref), editability flags, `selectColumn`, `listItemsClass`, and stable IDs.
   Reuse `ViewDiagnostic`/`ToSnapshot`.
2. **Importer lane for `<columns>` specs**, including `PartGenerator`-equivalent custom-field
   column generation and reading the legacy `ColListId` saved-column XML (with the
   version-20 ladder) so user column sets survive; extend `LayoutImportCoverage` to census the
   browse vocabulary the same way 4.9 did for detail layouts.
3. **Row model**: a path-aware row abstraction equivalent to `IManyOnePathSortItem`
   (object + ownership path), produced by the clerk-side provider, consumed by the owned table
   as its flattened virtualized items source.
4. **Owned header bar**: resize (persisting widths), drag reorder, sort-arrow display with
   multi-sort precedence, right-click column menu / config entry point; keep header reachability
   automation IDs compatible with the 2.4 UIA baselines.
5. **Sort/filter bridge seam**: owned table publishes `RecordSorter`/`RecordFilter` changes to
   the real clerk (reusing `Filters` types through a narrow interface) and re-binds persisted
   clerk sort/filter state to columns on load — the Avalonia analogue of `InitSorter`
   (BV:1188) + `UpdateActiveItems` (FB:593).
6. **Filter row control**: per-column combo/flyout with the matcher families seeded from the
   column's filter metadata, including the free-text "Filter for…" path and stable
   `FilterCombo.*` automation IDs.
7. **Selection service**: single current row bridged via `IRecordNavigationContext` (3.12),
   plus a managed checked-set service for the future check-box column (decoupled from
   `XMLViewsDataCache` tags).
8. **Per-cell WS resolution contract**: codify the `GetBestWsForNode` behavior (best/reversal/
   audio/RTL consequences) as a service the row templates consume, shared with 6.2.
9. **Performance fixtures**: 10k+-row virtualization benchmarks (open, scroll, sort-apply
   re-render) wired into 7.7 budgets; mixed-height fixture to exercise pivot trigger 2.
10. **Explicitly deferred, recorded for 9.5**: bulk-edit bar (tabs, preview/apply, ghost
    handling), click copy, in-cell editing (gated on 6.13), RDE row entry
    (`XmlBrowseRDEView`) — these keep the legacy `BrowseViewer` as the supported fallback
    surface for their tools until separately scheduled.
