# Rendering cutover — decoupling the Entries table from the native C++ Views engine

**Status:** design / decision register · **Date:** 2026-06-16 · **Author:** Claude (Opus 4.8)
**Scope:** the deferred task **6.2** ("Retire the legacy `BrowseViewer` fallback for this surface")
and its dependencies — making the Avalonia `LexicalBrowseView` the *sole* owner of the lexicon
Entries table, with no live `BrowseViewer` and no native Views (`IVwRootBox`/`IVwEnv`) dependency.
Reviewed against the as-built overlay (`architecture-review.md` §D1/§D2) and Stage 3 / Stage 13.

This document **grills the uncertain paths with code evidence and lays out alternatives** for the
resulting state. It does not change scope on its own; it is the foundation for the cutover PRs.

---

## 1. The reframing finding (decisive)

The request was framed as "the table uses low-level C++ for rendering — migrate it." The research
shows the framing is *narrower than feared*:

- **Cell-value extraction is already engine-free in mechanism.** `LayoutFinder.Key()` /
  `XmlViewsUtils.StringsFor` produce a cell's content by walking the column layout into a
  `TsStringCollectorEnv : CollectorEnv`, and **`CollectorEnv` is a pure managed C# implementation of
  `IVwEnv`** (`Src/Common/RootSite/CollectorEnv.cs:36`; its own class doc: the native C++ impl
  "is normally used to actually produce a Views-based display… This class and its subclasses are
  used by the same code, but for purposes like producing an equivalent string"). It reads only from
  `ISilDataAccess`; **no `IVwRootBox` is constructed.** `LayoutFinder.Key` even builds its own
  throwaway VC when none is supplied (`LayoutFinder.cs:257-263`).
- **Sort and filter are equally engine-free.** `GenRecordSorter`/`StringFinderCompare` and
  `RecordFilter`/`IMatcher`/`FilterBarCellFilter` are managed; the only native calls are
  registration-free COM **kernel/text** services (`IVwPattern`, `IVwTextSource`) — *not* the Views
  renderer — proven headless by `Src/Common/Filters/FiltersTests/TestPersistence.cs`.
- **A managed, LCModel-free rich-text model already ships and is in production** for the detail
  pane: `RegionTextRun`/`RegionRichTextValue` + `RegionRichTextAdapter.FromTsString`
  (`Src/xWorks/RegionValueFactory.cs:97-162`), consumed by `FwMultiWsTextField`. The detail pane
  already sources rich values headlessly straight off the SDA
  (`FullEntryRegionComposer.cs:816-923`) — no VC, no finder.

So the native Views engine (`Src/views`, C++) is genuinely required only for **rich interactive
document rendering** (caret, selection, multi-paragraph layout) — that is Stage 9, and
`stage-09-managed-document-text-engine.md:26-29,199-201` explicitly scopes the browse table *out* of
it. A **read-only browse grid is below the Stage 9 line.**

**What actually blocks "no C++":** two things, both structural rather than algorithmic.
1. `LayoutFinder`/`XmlBrowseViewBaseVc` live in `XMLViews.csproj`, which references forbidden
   assemblies (`RootSite`, `SimpleRootSite`, `ViewsInterfaces`, `Geckofx`, `SIL.LCModel`, `xCore`,
   `System.Windows.Forms`) — every one on the `EngineIsolationAuditTests` forbidden list
   (`EngineIsolationAuditTests.cs:28-32`). `FwAvalonia` may not reference them. **But they may stay
   in `xWorks` (LCModel-allowed) behind the `IBrowseRowSource` seam during coexistence;** the
   whole-assembly audit + `XMLViews` deletion is explicitly Stage 13b.
2. In product, `RecordBrowseView` still *constructs a live `BrowseViewer`* and reads cells from it
   (`GetRowCellStrings`), keeping ~50 coupling points on the legacy viewer.

The migration is therefore: **(a) re-source cell content / sort / filter directly off the clerk's
SDA behind the existing seam (in `xWorks`); (b) render runs faithfully in Avalonia; (c) own the
column model; (d) move the coupling points onto the owned table; (e) stop constructing
`BrowseViewer` under `UIMode=New`; (f) delete `XMLViews` in Stage 13b.** Steps (a)–(e) need no
native code and no `XMLViews` reference *from FwAvalonia*; (f) is the program-terminal deletion.

---

## 2. Target architecture (engine-isolated, seam-preserving)

```
 Avalonia (FwAvalonia, LCModel-free)            xWorks (LCModel-allowed, coexistence)        Model
 ┌───────────────────────────────────┐   seam   ┌──────────────────────────────────┐
 │ LexicalBrowseView                  │ ───────▶ │ ClerkBrowseRowSource              │
 │  • columns from BrowseColumnModel  │  IBrowse │  • cells: ITsString → Region-     │ RecordClerk
 │  • cells: RegionRichTextValue      │  RowSource│    RichTextValue (FromTsString)  │ RecordList
 │    → WS-aware inline renderer (NEW) │  + Sort/ │  • sort: GenRecordSorter →        │ ISilData-
 │  • header sort / filter row        │  Filter/ │    Clerk.OnSorterChanged          │ Access
 │  • selection / checkbox / peers    │  Edit/   │  • filter: FilterBarCellFilter →  │
 │  • context flyout (XCoreMenuBridge)│  Column  │    Clerk.OnChangeFilter           │
 └───────────────────────────────────┘  sources └──────────────────────────────────┘
            no XMLViews / no IVwRootBox                value pipeline: managed CollectorEnv
                                                       (XMLViews stays here until Stage 13b)
```

Key contract changes from the as-built overlay:
- **Seam value type:** `IBrowseRowSource.GetCellValues` returns rich cells (a `RegionRichTextValue`
  per cell), not `IReadOnlyList<string>`. The Avalonia cell renderer maps runs → Avalonia inlines.
  (Today `LexicalBrowseView.cs:35` returns flattened, run-less, WS-less joined strings — lossy.)

  **AS-BUILT DECISION (2026-06-17) — dual projection retained intentionally.** The shipped seam does
  NOT collapse to a single rich-primary `GetCellValues`. It keeps two members: the plain
  `IBrowseRowSource.GetCellValues : IReadOnlyList<string>` AND an optional
  `IBrowseRichCellSource.GetRichCell` (`LexicalBrowseView.cs`), with `ClerkBrowseRowSource`
  implementing both. **Rationale:** the rich path is the *display* source (read by
  `BrowseCellRenderer`, the managed cell renderer); the plain string is the *derived* value used for
  selection text and UIA/accessibility names (`BrowseRow.Cells`, `GetNameCore` in
  `LexicalBrowseView`). Both project the SAME per-column finder for a row, so they cannot represent
  independent content — the plain string is the rich value's `PlainText` for that cell. Collapsing to
  one rich-primary `GetCellValues` is the cleaner end state but the interface lives in `FwAvalonia`
  (owned elsewhere) and the change would ripple across the renderer, the selection name path, and the
  headless tests; it is therefore deferred. **Anti-drift guard:** `ClerkBrowseRowSource.GetRichCell`
  calls a DEBUG-only `AssertRichAndPlainAgree` asserting the rich and plain projections of a cell
  agree on emptiness (they share a finder but use `Key()` vs `Strings()`, so byte-equality is not
  asserted — only that they never contradict on whether the cell is blank), so the two projections
  cannot silently drift into describing different content.
- **Sort/filter authority moves to the clerk:** `ClerkBrowseRowSource` builds managed
  `RecordSorter`/`RecordFilter` and calls `Clerk.OnSorterChanged`/`Clerk.OnChangeFilter` (the list
  actually narrows, persists, and agrees with the detail pane), replacing `SortByDataColumn`
  delegation and the display-only `BrowseFilterProjection`. **LANDED (2026-06-17):** filtering and
  sorting are now fully clerk-routed and the vestigial `BrowseFilterProjection` (a display-only
  index-remap that was never populated in product) has been **deleted**. With no client-side remap the
  seam is honestly **pass-through: table row index == clerk index** (1:1, order-preserving), which is
  the documented INDEX CONTRACT on `ClerkBrowseRowSource`. This also fixes a latent selection bug: the
  clerk→Avalonia mirror pushed `SelectRow(Clerk.CurrentIndex)` (a raw clerk index) while a click read
  back through `HvoAt`, which previously applied the projection's `ClerkIndex` remap — the two
  disagreed the moment the projection was non-pass-through. Round-trip proven by
  `ClerkBrowseRowSourceSelectionTests` (real-domain, `xWorksTests`).
- **Column model is owned:** a managed `BrowseColumnSpec` set, read once from the layout
  XML + `browseDialogColumns.xml` + `<generate>` custom-field expansion, persisted byte-compatibly
  with the legacy `…ColumnList` blob + per-index width keys.
- **No live `BrowseViewer`** for this surface under `UIMode=New`; a **browse `ActiveHostContract`**
  + contract test proves it is never constructed (the browse analogue of
  `RecordEditViewActiveHostContractTests`).

---

## 3. Decision register (alternatives grilled)

### D-R1 — Cell content + rendering
**Recommendation: a managed `ITsString` → `RegionRichTextValue` cell provider (reusing the shipping
detail-pane mechanism) feeding a NEW read-only WS-aware inline cell renderer, with a
`LayoutFinder`-`Key()`-via-the-seam fallback for the hard columns.**

| Alt | Approach | Preserves | Effort | Risk |
| --- | --- | --- | --- | --- |
| A | Reuse `LayoutFinder.Key()` (not `.Strings()`) in `xWorks`, adapt via `FromTsString` | exact legacy column semantics (sortmethod, ghost, multipara) | Low | keeps `XMLViews` alive in xWorks (fine until 13b); does not by itself reach run-fidelity unless the call site captures the `ITsString` |
| B | Reimplement cell-value extraction in managed code from the ViewDefinition IR + SDA | full control, zero `XMLViews` | High | re-derives a large parity tail (sortmethod/ghost/best-WS) — where legacy bugs hide |
| **C (rec.)** | SDA-read `RegionRichTextValue` for common columns (as the detail pane does) **+ A-fallback** for the hard set, both behind `IBrowseRowSource` | runs, per-WS font/RTL, named styles, ORC markers; LCModel-free Avalonia | Medium | concentrated in the fallback set; size unknown (see Spike S1) |

Rendering: build a small `RegionTextRun[] → Avalonia InlineCollection` mapper (font/size/weight/
style/decoration per run; `FlowDirection` from dominant run direction; ORC → neutral placeholder,
read-only and rare in lexicon columns). **Single-run cells degrade to a plain styled `TextBlock`**
to bound shaping cost. This is net-new (no runs→inlines renderer exists today) but small; the run
*model* and the `FromTsString` adapter already ship. (`stage-09` confirms read-only single-paragraph
rendering needs none of the document engine.)

### D-R2 — Sort + filter ownership
**Recommendation: Alternative A (LANDED). Sort and filter are fully clerk-routed; the client-side
`BrowseFilterProjection` has been DELETED (2026-06-17) — it was inert in product, so keeping it only
created the index-space ambiguity described in §2.**

| Alt | Approach | Parity | Persistence | Risk |
| --- | --- | --- | --- | --- |
| **A (LANDED)** | Avalonia headers/filter-row build `GenRecordSorter`/`FilterBarCellFilter` and call `Clerk.OnSorterChanged`/`OnChangeFilter` | full (list narrows, status bar, nav, bulk-edit all agree); ICU-correct matching | free (clerk persists to PropertyTable LocalSettings) | reproducing every combo branch (chooser/list/spelling/date) is broad |
| B | Keep client-side `BrowseFilterProjection` | low — display only, wrong match semantics, no persistence, blocks legacy retirement; created a latent display↔clerk index disagreement | none | low tech / high divergence |
| C | Sort via clerk now; filter via clerk incrementally; projection as shrinking fallback | grows to full; sort full immediately | full for clerk-routed | transitional mixed-authority UX |

**Why A over the transitional C:** the projection's `SetFilter`/`SetPreset` were never called in
product (filtering routed through `_clerk.OnChangeFilter`), so `RowCount == _clerk.ListSize`,
`UnderlyingIndex(i) == i`, and `Sort()`'s `ClearFilters()` was a no-op. The projection was therefore
dead weight that nonetheless implied a display→underlying remap the selection mirror did not honor
consistently. Deleting it makes the seam honestly pass-through (see the INDEX CONTRACT) and removes
the only place a wrong remap could be reintroduced.

Sort is the simpler half and **removes the table's dependency on `BrowseViewer.SortByDataColumn`**
immediately. `WritingSystemComparer`/`IcuComparer` give ICU-correct ordering. Refresh the table off
`SorterChangedByClerk`/`FilterChangedByClerk` (the same signals the legacy viewer used).

### D-R3 — Column model + configure dialog
**Recommendation: Alternative C now (snapshot the full column spec from a transient VC, release the
viewer) → Alternative B as target (typed `BrowseColumnSpec` + ported configure dialog). Defer A.**

| Alt | Approach | Persistence-compat | Effort | Risk |
| --- | --- | --- | --- | --- |
| A | Model columns in the ViewDefinition IR + override system | needs a migrator; **IR can't represent width or per-column ws today, and lacks the dialog/menu visibility tri-state** | High | destabilizes the shared DataTree IR |
| **B (target)** | Typed managed `BrowseColumnSpec` (label/layout/ws/width/visibility/field/sort/bulkEdit) + ported configure dialog; write the same `…ColumnList` blob + width keys | excellent (byte-compatible round-trip with legacy) | Medium | faithfully reproducing `WsComboContent` + bulk-edit wiring |
| **C (now)** | Read `ColumnSpecs` once from a transient `XmlBrowseViewBaseVc`, snapshot *all* attributes, discard the viewer; keep the existing `ColumnConfigureDialog` | perfect (it *is* the legacy path) | Low | keeps `XMLViews` in the graph (a stepping stone, not the destination) |

**Hard dependency (all alts):** custom-field columns come from `<generate>` expanded by
`PartGenerator.GetGeneratedChildren` against the **live `LcmCache`** — the column set can be
WinForms-free but not cache-free. **Width keys are positional** (`FormatColumnWidthPropertyName(iCol)`);
a managed reorder must keep that index consistent or saved widths attach to the wrong column.

**Landed (F1):** the owned column model exists — `Src/xWorks/BrowseColumnSpec.cs` (a managed
per-column snapshot: label, stable field token from `field`/`transduce`, ws, editability) +
`ToViewDefinition`, consumed by `RecordBrowseView.BuildColumnDefinition` instead of fabricated
`"col{i}"` tokens (`BrowseColumnSpecTests`). This is the C snapshot read from the still-live viewer;
width/persistence and the configure-dialog port (target B) and viewer-free sourcing (F2) remain.

### D-R4 — Cutover sequencing
**Recommendation: Alternative C (shared-control-first, then per-surface cutover), landed inside the
existing Stage 13a (flip + bake) → 13b (delete) gating.**

| Alt | Approach | Rollback | Evidence | Risk |
| --- | --- | --- | --- | --- |
| A | Move the ~50 coupling points off `BrowseViewer` one at a time while it stays constructed | trivial (`UIMode=Legacy`) per step | strongest (per-coupling tests) | long-lived **dual-authority** guard sprawl; two browse stacks per open |
| B | Big-bang: never construct `BrowseViewer` under New; re-implement all coupling + delete overlay in one change | only whole-flag revert | must land all gates at once → "hallucinated parity" risk | highest; effectively un-reviewable |
| **C (rec.)** | First make the owned table the true *owner* (clerk-routed sort/filter, standalone cell-value source, context menu, message targets, PrepareToGoAway, snap, ctrl-tab) + browse `ActiveHostContract`; *then* stop constructing `BrowseViewer` per surface; deletion is a separate gated PR | full runtime rollback via `UIMode` through the bake window | best-aligned: seam capabilities get headless coverage; perf + promoted audit gate the deletion, not the flip | medium; front-loads the hard pieces as independently testable seam work |

---

## 4. Phased plan (firm foundation → cutover)

**Phase F0 — Spikes (gate the estimates).** S1 column-parity diff; S2 perf at scale; S3 headless
clerk sort/filter end-to-end (see §5). Nothing irreversible.

**Phase F1 — Make the owned table the OWNER (no deletion, overlay still present).**
- Rich cell pipeline: seam returns `RegionRichTextValue`; build the runs→inlines renderer + single-run
  fast path; source cells via D-R1/C (+A-fallback).
- Clerk-routed **sort** (D-R2: immediate), then **filter** (incremental); `BrowseFilterProjection`
  retired — DELETED 2026-06-17 (seam now pass-through, see §2 / D-R2).
- Owned **column model** (D-R3/C now): snapshot full `ColumnSpecs`; keep `ColumnConfigureDialog`.
- Owned **selection/index persistence, checkbox, context flyout** (`XCoreMenuBridge` +
  `RegionMenuFlyout`, the edit-surface pattern), **message targets**, **ctrl-tab**,
  **PrepareToGoAway/commit-on-close**, **snap width**, **stylesheet**.
- Each capability ships behind `UIMode=New` with headless tests; legacy stays the default.

**Phase F2 — Browse active-host contract + stop constructing `BrowseViewer`.**
- Introduce `m_legacyBrowseInitialized`/`m_activeHostContract` on `RecordBrowseView`; a browse
  `ActiveHostContract` (approved adapters TBD — see Q3) + a `RecordBrowseViewActiveHostContractTests`
  asserting, under New mode, the legacy `BrowseViewer` is **never constructed or parented**.
- `SetupDataContext` short-circuits viewer construction under New. `UIMode` remains live rollback.

**Phase F3 — Bake + delete (Stage 13).** 13a default flip + field bake metric; 13b promoted
whole-assembly `EngineIsolationAuditTests` + 2.7 perf budget green → delete `XMLViews`/`BrowseViewer`
(leaf-first, after Stages 7 & 9 drop the last `SimpleRootSite`/`IVwRootBox` consumers).

This makes F1 the "firm foundation for the remaining phases": once the owned table is the authority
for value/sort/filter/columns/menus, every other `RecordBrowseView` consumer (concordance, bulk-edit
tools) and Stage 7/8 inherit it, and the cutover/deletion become mechanical and gated.

---

## 4a. F2 implementation recipe — standalone column/finder provider (decouple from the live viewer)

Investigation (2026-06-16) confirms `ClerkBrowseRowSource`'s entire dependency on the live
`BrowseViewer` (columns, finders, cell `ITsString`s, sorters, filters) can be served by a standalone
provider with **no WinForms control, root box, or `DhListView`** — every Stage-3 method already routes
through managed types (`XmlBrowseViewBaseVc`, `LayoutFinder`/`CollectorEnv`, `XMLViewsDataCache`
decorator, `StringFinderCompare`/`FilterBarCellFilter`). Land it **additively, behind `UIMode=New`**, so
the Legacy default path is byte-for-byte unchanged.

Steps (additive; legacy ctor untouched):
1. **Parent-free column factory.** Extract the active-column build in `XmlBrowseViewBaseVc(xnSpec,
   fakeFlid, xbv)` (XmlBrowseViewBaseVc.cs:190-268) into a helper that takes `(cache, mediator,
   propertyTable, sortItemProvider, specialSda)` instead of reading them off `xbv`
   (`m_bv.PropTable`, `Mediator`, `SortItemProvider`, `SpecialCache`, `Cache`). Two in-body retargets:
   `ComputePossibleColumns` line 601 `m_xbv.Cache`→`m_cache`; null-guard `m_app` in
   `SetupSelectColumn` (562-576). The existing ctor calls the same helper → identical legacy behavior.
2. **`BrowseColumnProvider` (xWorks):** holds `specialSda = new XMLViewsDataCache(baseSda, nodeSpec)`
   (keep — it installs the `XmlViewsMdc` override the finders need), a standalone
   `XmlBrowseViewBaseVc` built via step 1, and lifts the ~7 self-contained methods currently on
   `BrowseViewer` (`GetColumnName`, `IsColumnEditable`, `GetColumnEditAttributes`,
   `EnsureAvaloniaColumnFinders`, `GetRowCellStrings`/`GetRowCellTsString`, `MakeColumnSorter`,
   `MakeColumnFilter`). Inputs all available in `RecordBrowseView` without the viewer: `Cache`,
   `m_mediator`, `m_propertyTable`, `m_configurationParameters` (nodeSpec), `Clerk.SortItemProvider`,
   `Clerk.VirtualListPublisher` (baseSda), `propertyTable.GetValue<IApp>("App")`.
3. **`ClerkBrowseRowSource`:** take an interface both `BrowseViewer` and `BrowseColumnProvider`
   satisfy (extract `IBrowseColumnSource`), defaulting to the viewer; `BrowseColumnSpec.Snapshot`
   already reads only those accessors, so it re-targets with no logic change.
4. **`RecordBrowseView`:** under `UIMode=New`, construct the provider instead of `CreateBrowseViewer`;
   move the surviving coupling points (selection-index persistence, right-click `mnuBrowseView` via
   `XCoreMenuBridge`/`RegionMenuFlyout`, `GetMessageTargets`, ctrl-tab, `PrepareToGoAway`, snap,
   stylesheet) onto the owned table; add `m_legacyBrowseInitialized` + a browse `ActiveHostContract`
   and a `RecordBrowseViewActiveHostContractTests` asserting the viewer is never constructed under New.
5. **Gate:** the byte-identical **spike** below; deletion of `BrowseViewer`/`XMLViews` stays Stage-13b
   (promoted whole-assembly `EngineIsolationAuditTests`).

Effort: low–moderate, mostly mechanical. Blast radius: contained to `UIMode=New` (opt-in) plus the
single shared extract-method on `XmlBrowseViewBaseVc` (behavior-preserving for the legacy path).

## 5. Load-bearing unknowns → spikes (do these first)

- **S1 — Column-display parity / fallback-set size (gates D-R1 effort: Medium vs High).** Render the
  full `browseDialogColumns.xml` always/dialog column set against `TestLangProj` two ways — the C
  SDA-projection and the A `LayoutFinder.Key()`→`FromTsString` — and **diff the `RegionRichTextValue`s
  (runs+WS+ORC, not `.Text`)**. The count/identity of mismatches tells you exactly how much of
  `XmlVc.DisplayCell` must stay on the finder fallback (sortmethod, multipara, ghost-field rows,
  `displayWs="best …"`, reference `…TSS` columns).
- **S2 — Per-cell shaped-text cost at 10k rows / 150% DPI under scroll (gates D-R1 renderer + closes
  task 2.7 / #18626).** Render a mixed-WS/RTL/multi-run fixture into the inline cell across a 10k
  scroll with the single-run `TextBlock` fast path in place; record frame/realization timing vs the
  legacy `BrowseViewer` baseline. The existing `TypingLatencyHarnessTests` covers model algorithms
  only — **layout/shaping is unmeasured.**
- **S3 — Headless clerk sort/filter end-to-end (gates D-R2 + whether the column XmlNode must be
  carried).** Stand up one `lexiconBrowse` `RecordClerk` with **no** `BrowseViewer`/`FilterBar`;
  build a `GenRecordSorter` and a `FilterBarCellFilter` for Lexeme Form + Gloss from column metadata;
  call `OnSorterChanged`/`OnChangeFilter`; assert `ListSize`/`SortedObjects` reorder and narrow.
  Reveals how much of the raw `colSpec` XML the browse-column model must retain.

---

## 6. Open questions (need a human decision)

- **Q1 — Scope.** Is the cutover scoped to the **lexicon Entries surface only**, or to **all
  `RecordBrowseView` consumers** (Bulk Edit Reversal Entries, concordance, `RecordBrowseActiveView`)?
  This changes whether bulk-edit (`BulkEditBar`, ~7.7k LOC) parity is in scope now or deferred.
- **Q2 — Public-API consumers.** `RecordBrowseView.BrowseViewer`, `CheckedItems`, `CheckBoxChanged`
  are public; a repo-wide find-and-fix is required before deletion. Acceptable to re-point these at
  the owned table's API?
- **Q3 — Browse context-menu routing.** Do browse row commands resolve purely via the owned host's
  `GetMessageTargets`, or do they need a `command-menu-routing`-style approved baseline adapter (as
  the edit surface's hidden DataTree colleague chain does)? Determines the browse `ActiveHostContract`
  approved-adapter set.
- **Q4 — Column-IR unification (D-R3/A).** Defer indefinitely, or schedule after the DataTree IR
  migration stabilizes (to unify override storage)?
