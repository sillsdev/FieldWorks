# Stage 3 Review — Shared editable virtualized grid/tree control

**Reviewer:** Claude (Opus 4.8)  •  **Date:** 2026-06-15  •  **Branch:** `010-advanced-entry-view-phase-1-2`

Scope under review: master plan §4 (stage table, row 3) and §6 Stage-3 detail in
`openspec/changes/avalonia-migration-roadmap/complete-migration-program.md`, against the frozen
architecture (`architecture-patterns.md` §4, §12), pivot triggers (`seam-catalog.md` §3),
perf budgets (`parity-evidence.md` §5), and the as-built code under `Src/Common/FwAvalonia/`.

---

## 1. Scope assessment

**Verdict: scope is correct in *charter* but mis-sized as *one epic of equal weight*. Split it,
sequence table-first, and pull the tree half forward into Stage 0 close-out where it already lives.**

The instinct to own this control is right and is now *more* right than when the plan was written
(see §3 — TreeDataGrid's FOSS repo was archived 2025-10-13 and editing moved behind the commercial
Accelerate license). The "#1 off-the-shelf gap" framing holds.

But "table AND tree in one stage" conflates two problems with very different maturity in this repo:

- **The tree is largely already solved.** The detail/slice surface does *not* need a virtualized
  tree at all. `Src/Common/Controls/DetailControls/DataTree.cs` (5,453 lines) lays slices out as a
  **flat vertical stack with indent-only visual nesting** — it is not a real parent/child tree
  widget. The Avalonia replacement `Src/Common/FwAvalonia/Region/LexicalEditRegionView.cs` (563
  lines) already renders that flat field list, and the committed baseline
  `DataTreeTimingBaselines.json` tops out at **253 slices** (`timing-extreme`/`paint-extreme`).
  253 controls in a `ScrollViewer` is *not* a virtualization problem — it is comfortably below the
  ~100+ threshold where Avalonia even recommends virtualizing per-item, and the existing view ships
  it unvirtualized today. So "owned virtualized TREE" is **over-specified** for the surfaces named.
  The genuinely unbounded tree need is the **popup possibility-list / chooser**, and that is
  *already built and virtualized*: `FwOptionPicker.cs` uses a `VirtualizingStackPanel` with depth
  indentation, and `TreeSpikeAndRtlTests.cs` (`SenseTreeSpikeTests`) validated stock `TreeView` for
  the bounded (≤500) case per `architecture-patterns.md` §4.

- **The table is the real, unsolved, large-data problem.** `LexicalBrowseView.cs` (165 lines)
  exists but is explicitly **read-only display only** (its own header: "First version: read-only
  display… sorting/filtering and bulk-edit columns follow"). The legacy surface it replaces is
  enormous and feature-dense: `BrowseViewer.cs` (4,332), `XmlBrowseViewBase.cs` (2,245, inherits
  native `RootSite`/`IVwRootBox`), `BulkEditBar.cs` (**7,685**), `FilterBar.cs` (2,835),
  `DhListView.cs` (838), plus the fake-flid `XMLViewsDataCache.cs`. Editing, checkbox columns,
  bulk-edit preview columns, RDE in-cell editing, multi-column sort, filtering, and column
  reorder all live there. **This is where the engineering risk and most of the LOC sit.**

**Recommendation:** keep one epic but make it **table-led and explicitly phased** (§5), and
**down-scope the "tree" deliverable** to "flat indented expander/collapse row chrome on the same
owned virtualizing list, used only if/when a detail surface exceeds the unvirtualized budget" —
governed by the existing `VirtualizingStackPanel` pivot trigger (`seam-catalog.md` §3), not built
speculatively. Treat the tree exit-gate fixture (253-slice detail) as a *budget the existing
unvirtualized view must already meet*, not proof of a new virtualized tree control.

---

## 2. Feasibility (repo-grounded)

**Owned virtualization on Avalonia 11.3.17 (the version in `Directory.Packages.props`) is feasible
for read display and is the only viable path given licensing; editable virtualization at scale is
feasible but carries two concrete, repo-relevant risks.**

What is already proven in-repo:
- **Lazy data virtualization works.** `LexicalBrowseView.BrowseRowList` is an `IList`/
  `IReadOnlyList<BrowseRow>` facade that returns the count without materializing rows; cells
  materialize only on `BrowseRow.Cells` access. `BrowseAndCanonicalJsonTests.cs`
  (`TenThousandRows_RealizeOnlyTheVisibleWindow`) asserts <100 realized `ListBoxItem`s and <300
  materialized cells against a 10,000-row source. The 10k-row exit gate is therefore *already
  partially met for read-only display.*
- **Typing-latency budget tooling exists.** `TypingLatencyHarnessTests.cs` enforces ≤6 ms/keystroke
  at 100% DPI and ≤8 ms at 150% DPI over 500 keystrokes incl. RTL/bidi — the harness Stage 3's
  editable-cell gate needs is in place.
- **Density/DPI gates exist** (`VisualParityAndDensityTests.cs`, `FwAvaloniaDensity` locked by
  `DensityTokenGateTests`).

Repo-grounded risks:
1. **`VirtualizingStackPanel` scroll/GC cost is a known upstream weakness.** Avalonia issue #18626
   (open, unresolved) documents a measure/arrange cycle on *every* scroll delta and GC pressure
   from aggressive recycling; the maintainer-proposed fix (a ~50% viewport buffer) is not shipped.
   This is *exactly* the condition behind the `seam-catalog.md` §3 pivot trigger ("escalate to a
   fully owned realization-window virtualizer if scroll/expand or open-time budgets fail on the
   production fixtures"). **The spike must measure scroll/expand on the production fixtures, not
   just open-time**, because the current 10k test only proves realization count, not scroll
   smoothness.
2. **Custom AutomationPeers for virtualized items are genuinely missing and non-trivial.** The
   second survey found **zero** custom `AutomationPeer` subclasses anywhere in `FwAvalonia` — only
   attached `AutomationProperties`. Avalonia's `ItemContainerGenerator` (unlike WPF/UWP)
   **does not retain realized containers**; the panel does. A UIA tree that exposes *all* rows
   (not just realized ones) for a virtualized grid therefore requires a custom
   `ItemsControlAutomationPeer` that synthesizes peers for de-realized items and coordinates with
   the virtualizing panel — this is real work the plan currently buries in one sub-bullet and is a
   hard exit-gate dependency (`migration-checklist.md`/§7.7–7.9 require UIA2 evidence on realized
   windows). **Size it as its own work item.**
3. **In-cell editing through the owned table is unproven.** `LexicalBrowseView` is read-only; the
   legacy editable path (`XmlBrowseRDEView`, fake-flid `ktagEditColumnBase`, native `RootSite`
   cells) has no Avalonia counterpart yet. Editable cells must reuse the existing owned field
   controls (`FwMultiWsTextField`, `FwChooserField`) inside cells and route through `IEditSession`/
   `IUndoRedoCoordinator` — feasible because those seams exist, but it is the bulk of the build.

Net: read-display + selection + keyboard at 10k rows is low-risk (mostly done). Editable cells +
custom AutomationPeers + scroll-budget-proven virtualization is the senior-grade work.

---

## 3. Best practices (and an external development that strengthens the plan)

- **Data virtualization vs UI virtualization — do both, as the repo already does.** The
  `IBrowseRowSource`/`BrowseRowList` lazy facade (data virtualization) over a
  `VirtualizingStackPanel` (UI virtualization) is the correct two-layer pattern; preserve it and
  extend it to editable cells rather than introducing a second mechanism.
- **Prefer the existing stock substrate until a fixture fails.** Avalonia's own guidance and the
  `seam-catalog.md` pivot trigger both say: own the *row/cell*, virtualize with stock primitives,
  and escalate to a fully-owned realization-window virtualizer *only* if measured scroll/expand/
  open budgets fail. The spike's job is to fire-or-clear that trigger with numbers, not to default
  to a from-scratch virtualizer.
- **Recycling/buffering:** if scroll smoothness fails (per #18626), the cheapest mitigation is a
  realized buffer above/below the viewport before committing to a fully-owned panel; record which
  was chosen and why in the manifest.
- **ItemsRepeater is NOT the substrate.** It is on a deprecation path (slated for eventual
  obsolescence once a `VirtualizedUniformPanel`/`ItemsControl` virtualization story lands; kept
  only for 12.x back-compat). The `seam-catalog.md` "ItemsRepeater" pivot trigger ("reconsider only
  if un-deprecated with maintained virtualization") should be marked **confirmed-closed** by this
  review.
- **Custom AutomationPeer pattern:** subclass `ItemsControlAutomationPeer`; synthesize child peers
  from the *data* (row count) not the realized containers, and implement
  `Selection`/`ExpandCollapse`/`Invoke`/`Grid` patterns explicitly. Day-one `AutomationId` from
  StableId is already the repo convention (§7.5/7.8).

**External development that *strengthens* the no-TreeDataGrid decision (record it):** TreeDataGrid's
FOSS repository was **archived 2025-10-13** (no further fixes/updates), and editing/advanced
features now require the commercial **Avalonia Accelerate** license (v11.2.0+). The plan's
"display-only, licensing-blocked" rationale is no longer just a snapshot — the FOSS option is now
*abandoned*. The `seam-catalog.md` TreeDataGrid pivot trigger ("re-evaluate if relicensed
permissively") has moved **further** from firing, not closer. This should be noted in the manifest
as the recorded decision.

---

## 4. Interactions & dependencies

What the downstream stages actually pull from Stage 3:

- **Stage 4 (finish Lexical/Advanced Entry):** needs the **editable table** for the entry view's
  embedded tables (lexical-edit tasks 7.x; §6 Stage-4 bullet "Tables/browse in the entry view on
  the Stage-3 control"). This is the *first real consumer of editing*, and Stage 4 is the exemplar
  every Track-II stream copies. **Stage 4 is gated on the editable table, not on any tree.** This is
  the strongest argument for table-first.
- **Stage 7 (Texts & Words / Interlinear + Concordance):** needs the **table** for concordance /
  occurrence grids. Note the survey finding: `ConcordanceControl.cs` drives a `BrowseViewer`
  *indirectly* through a `RecordClerk` (`OccurrencesOfSelectedUnit`), and interlinear itself is
  **`RootSite`/Views-engine document rendering, not a grid** — so Stage 7 depends on Stage 3 for the
  *concordance browse grid* but on **Stage 9** (managed document engine) for interlinear proper. The
  master graph already wires `S3→S7` and `S9→S7`; that is correct. Stage 7's grid need is
  display + sort + filter + selection, plausibly *not* heavy in-cell editing.
- **Stage 8 (Lists, Notebook, bulk-edit):** the heaviest consumer of **bulk-edit columns**
  (`BulkEditBar.cs`, 7,685 lines), **checkbox-select columns**, and **multi-column sort/filter**.
  This is where the table's most complex non-editing features (preview columns via fake flids,
  check-all/uncheck-all, transduce/find-replace) are actually exercised.

Implication for sequencing: the table's feature set should land **incrementally aligned to its
consumers** — Stage 4 needs *editing*; Stage 7 needs *sort/filter/select*; Stage 8 needs
*bulk-edit/checkbox*. They do **not** all need everything at once, which makes incremental shipping
both possible and desirable (§5).

Cross-stage conflict to flag: the master table marks Stage 3 "Parallel? with 2" and Stage 7 "Depends
on 3,9". If Stage 3 ships as one monolithic "table+tree+editing+bulk-edit" epic, it becomes a
**serial bottleneck** in front of 4/7/8. Phasing it (table-read → table-edit → bulk-edit) lets
Stage 4 start as soon as table-edit lands and lets Stage 8's bulk-edit work overlap.

---

## 5. Recommended plan changes

1. **Re-title and re-scope the epic to "Shared editable virtualized *table* (+ indented-row tree
   chrome)".** Demote the standalone "owned virtualized TREE" deliverable: the detail surface is a
   flat 253-row list that already ships unvirtualized; deliver expander/indent row chrome on the
   *same* owned list and only escalate to virtualization if the 253-slice budget fails. Keep the
   253-slice fixture as an exit gate on the *existing* `LexicalEditRegionView`, not as proof of a
   new control.
2. **Ship the table in three sub-milestones, gated by consumer:**
   - **3a Read table at scale** — extend `LexicalBrowseView`: column header sort affordance,
     selection, keyboard nav, **custom AutomationPeer**, 10k-row scroll/expand budget proven on
     production fixtures at 100%/150% DPI (closes most of the current read gaps; #18626 risk
     retired or pivot fired). *Unblocks Stage 7 grid + Stage 8 display.*
   - **3b Editable cells** — in-cell editing reusing `FwMultiWsTextField`/`FwChooserField`, routed
     through `IEditSession`/`IUndoRedoCoordinator`; typing-latency budget via the existing harness.
     *Unblocks Stage 4 (exemplar) — highest priority after 3a.*
   - **3c Bulk-edit + checkbox + filter** — checkbox-select column, multi-column filter/sort,
     bulk-edit preview columns (replacing the fake-flid `XMLViewsDataCache` mechanism with an
     in-memory model). *Unblocks Stage 8.* Can overlap Track II.
3. **Make the spike measure scroll/expand on production fixtures, not just open-time realization
   count.** Explicitly evaluate the #18626 condition and record fire/clear of the
   `VirtualizingStackPanel` pivot trigger in the manifest with numbers.
4. **Promote "custom AutomationPeer for the virtualized table" to a first-class, separately-tracked
   work item** with UIA2/FlaUI realized-window evidence as its gate (it is currently one sub-bullet;
   it is the single most novel piece of engineering in the stage).
5. **Record two pivot-trigger resolutions in `seam-catalog.md` in the Stage-3 PR:** TreeDataGrid →
   *confirmed closed* (FOSS archived 2025-10-13, editing behind Accelerate commercial license);
   ItemsRepeater → *confirmed closed* (on deprecation path; not the substrate).
6. **Reuse, don't rebuild:** `IBrowseRowSource`/`BrowseRowList`, `FwOptionPicker`'s
   keyboard/focus/virtualization, `FwAvaloniaDensity`, `TypingLatencyHarnessTests`, the Path-3
   bundle. The survey confirms none of these need re-creation.

---

## 6. Open questions / risks

- **Scroll smoothness at 10k rows is unmeasured.** The current test proves realization *count*, not
  scroll/expand latency. Upstream #18626 is unresolved. **Highest technical risk.** If it fails, the
  stage absorbs a fully-owned realization-window virtualizer — a materially larger build the master
  risk register only obliquely covers.
- **Custom AutomationPeer effort is unestimated and unprecedented in-repo** (zero exist today).
  Could be the long pole within the stage; UIA enumeration of de-realized rows is the hard part.
- **Editable-cell semantics vs. native RDE / fake-flid cache.** Replacing `XMLViewsDataCache`'s
  fake-flid preview/edit-storage model (90000000-range tags) with a managed in-memory model is
  underspecified; bulk-edit *preview* columns especially. Risk of silently dropping a legacy
  capability — `EngineIsolationAuditTests` + a census of `BulkEditBar` features should gate it.
- **Is heavy in-cell editing even needed for Stage 7 concordance?** If concordance grids are
  display+navigate only, 3b need not block 7 — worth confirming with the Stage 7 owner to refine the
  dependency graph (currently `S3→S7` implies the whole control).
- **Column reorder / resize / persistence** (legacy `DhListView` drag-reorder) is not mentioned in
  the Stage-3 detail; confirm whether it is in-scope or deferred to a consuming stage.

---

## 7. Confidence

**High** on the central judgments: (a) owning the table is correct and *increasingly* correct given
the TreeDataGrid FOSS archival + Accelerate licensing move; (b) the "tree" half is over-scoped
because the detail surface is a flat 253-row list already shipping unvirtualized and the unbounded
tree need is the *already-virtualized* chooser; (c) the stage should ship table-first in three
consumer-gated sub-milestones; (d) custom AutomationPeers and scroll-budget proof are the real,
currently-understated risks. All grounded in read code (`LexicalBrowseView.cs`,
`LexicalEditRegionView.cs`, `FwOptionPicker.cs`, `DataTreeTimingBaselines.json`, the test files) and
current Avalonia status (11.3.17 in-repo; #18626; TreeDataGrid archival).

**Medium** on the precise editable-cell effort and the AutomationPeer effort — both lack in-repo
precedent, so their sizing is the spike's first job. **Medium** on whether Stage 7 truly needs 3b
(editing) vs only 3a — depends on a concordance-grid scope answer from the Stage 7 owner.

---

### Sources (external)

- Avalonia `VirtualizingStackPanel` scroll/GC issue (open): https://github.com/AvaloniaUI/Avalonia/issues/18626
- TreeDataGrid → Accelerate licensing change + FOSS repo archival (2025-10): https://avaloniaui.net/blog/building-a-sustainable-future-for-avalonia , https://github.com/AvaloniaUI/Avalonia.Controls.TreeDataGrid
- ItemsRepeater deprecation trajectory: https://github.com/AvaloniaUI/Avalonia/discussions/16829
- Avalonia performance / virtualization guidance: https://docs.avaloniaui.net/docs/app-development/performance
- Custom AutomationPeer / ItemContainerGenerator (containers not retained by generator): https://api-docs.avaloniaui.net/docs/T_Avalonia_Controls_Generators_ItemContainerGenerator
