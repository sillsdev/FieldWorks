## Context

This change builds out Stage 3 of the Avalonia migration roadmap (see
`openspec/changes/avalonia-migration-roadmap/epics/stage-03-virtualized-grid-tree.md` and its
review). It is the program's largest foundation effort: the owned, editable, virtualized **table**
that replaces the WinForms XMLViews stack and gates Stages 4 (Advanced Entry View), 7 (concordance),
and 8 (bulk-edit).

Current state in-repo:
- `LexicalBrowseView` (`Src/Common/FwAvalonia/Region/LexicalBrowseView.cs`, 165 LOC) is **read-only
  display**: a `ListBox` over `VirtualizingStackPanel`, columns from the typed view definition's
  field nodes, cells from a lazy `IBrowseRowSource`/`BrowseRowList` facade. Each header/cell already
  stamps an automation id. There is no selection model, sort, edit, filter, or bulk-edit yet, and
  **no custom AutomationPeer** (zero exist anywhere in `FwAvalonia`).
- Proven seams to reuse: `IBrowseRowSource`/`BrowseRowList` (data virtualization;
  `BrowseAndCanonicalJsonTests.TenThousandRows_RealizeOnlyTheVisibleWindow`), `IEditSession`
  (`Src/Common/FwAvalonia/Seams/ISeams.cs`), the `avalonia-undo-redo` capability,
  `FwMultiWsTextField`/`FwChooserField` (`Region/FwFieldControls.cs`), `FwOptionPicker`'s
  virtualization/keyboard/focus, `FwAvaloniaDensity`, and the `TypingLatencyHarnessTests` /
  density/DPI harnesses.

Hard constraints: Avalonia is pinned to 11.x until WinForms is removed; the app is LGPL and
redistributed broadly, so per-seat commercial grids are unworkable; multi-writing-system content
means non-uniform row heights and full per-cell template control.

## Goals / Non-Goals

**Goals:**
- One owned table control, extended in place from `LexicalBrowseView`, delivered in three
  consumer-gated milestones: 3a read at scale, 3b editable cells, 3c bulk-edit/filter/sort.
- Wire FLEx's main Lexical Edit browse/table view onto it (read then edit) as the first production
  consumer.
- Prove the 10k-row scroll/expand budget with measured numbers; resolve the `VirtualizingStackPanel`
  pivot trigger with evidence.
- First-class custom AutomationPeers for virtualized rows/cells.

**Non-Goals:**
- A virtualized **tree** control (detail surface is a flat 253-slice stack already shipping
  unvirtualized; chooser is already virtualized).
- Interlinear/document rendering (Stage 9).
- TreeDataGrid / `Avalonia.Controls.DataGrid` / any commercial grid.
- Column reorder/resize persistence (deferred to a consuming stage unless required).
- Migrating Stage 4/7/8 surfaces beyond the Lexical Edit main table exemplar.

## Decisions

**D1 — Own the row/cell; virtualize with the stock `VirtualizingStackPanel`; escalate only on
measured failure.** Continue the proven two-layer pattern (lazy `IBrowseRowSource` for *data*
virtualization, stock panel for *UI* virtualization). *Alternatives:* (a) TreeDataGrid — rejected:
FOSS archived 2025-10, editing behind commercial Accelerate/AGPL, weak editing+accessibility;
(b) `Avalonia.Controls.DataGrid` — rejected: unmaintained Silverlight port, virtualization/editing
bugs, nothing over owned headers for template-heavy cells; (c) a from-scratch realization-window
virtualizer up front — rejected as premature; it is the documented *pivot*, taken only if 3a
fixtures fail.

**D2 — Extend `LexicalBrowseView` in place rather than start a new control.** It already carries the
lazy facade, density tokens, and automation-id stamping the specs require; new owned types
(header bar, cell host, selection service, AutomationPeer) compose around it. Keeps the read path
shipping while editing lands.

**D3 — Reuse existing field controls inside cells (3b).** Editable cells host `FwMultiWsTextField` /
`FwChooserField` chosen via the view definition's editor descriptor / editor registry, rather than a
new in-cell editor. Commit/cancel drive `IEditSession`; changes record on the global undo stack via
`avalonia-undo-redo`. *Alternative:* a bespoke lightweight cell editor — rejected: would fork the
multi-WS/RTL and undo behavior already solved in the field controls.

**D4 — Replace the fake-flid cache with a managed in-memory model (3c).** Bulk-edit
preview/edit-storage moves off `XMLViewsDataCache`'s 90000000-range fake flids onto an explicit
in-memory model; apply commits through the edit session. A recorded `BulkEditBar` feature census
gates parity so no capability is silently dropped.

**D5 — Custom AutomationPeer synthesizes peers from data, not containers.** Subclass
`ItemsControlAutomationPeer`; build child peers from `IBrowseRowSource.RowCount` (Avalonia's
`ItemContainerGenerator` does not retain de-realized containers), implementing
Selection/Grid/Invoke and stable StableId-derived ids. Tracked as a first-class work item, not a
sub-bullet — it is the most novel piece.

**D6 — Spike-first, evidence-gated milestones.** 3a starts with a spike that measures scroll/expand
(not just realization count) on production fixtures at 100%/150% DPI and records the
`VirtualizingStackPanel` pivot fire/clear in the manifest. 3a unblocks 7/8 display; 3b unblocks 4;
3c unblocks 8 and can overlap Track II.

## Risks / Trade-offs

- **Scroll/GC cost of `VirtualizingStackPanel` at 10k rows (upstream #18626, unresolved).** → 3a
  spike measures scroll/expand on production fixtures; if budgets fail, escalate to a realized
  buffer above/below the viewport, then to a fully-owned realization window — record which and why.
- **Custom AutomationPeers are net-new and unprecedented in-repo.** → Size as its own work item with
  UIA2/FlaUI realized-window evidence as its gate; could be the long pole.
- **Bulk-edit preview/edit semantics underspecified vs the fake-flid cache.** → Gate 3c on a
  `BulkEditBar` feature census + `EngineIsolationAuditTests`; risk is silently dropping a legacy
  capability.
- **Mixed row heights fight virtualization estimate/bring-into-view.** → Validate variable-height
  behavior against the production fixture before committing the panel choice.
- **Scope creep into a generic "table for everyone."** → Build the FLEx-shaped control first;
  generalize into a reusable add-on only by harvesting working code later, never up front.

## Migration Plan

1. **3a:** spike + measure → extend `LexicalBrowseView` (header sort, selection, keyboard, custom
   AutomationPeer) → wire Lexical Edit main table read path → record pivot-trigger resolution.
2. **3b:** in-cell editing via field controls + edit-session/undo + typing-latency gate → Lexical
   Edit main table edits at parity.
3. **3c:** checkbox column → multi-column filter/sort → bulk-edit in-memory model + census.
Each milestone is independently shippable behind its consumer; the legacy `BrowseViewer` stays as
fallback until the Lexical Edit main table reaches edit parity. Roll back per-milestone by leaving
the consumer on the legacy surface.

## Open Questions

- Does Stage 7 concordance need 3b editing or only 3a display? (Confirm with the Stage 7 owner to
  refine the `S3→S7` edge.)
- Is column reorder/resize/persistence (legacy `DhListView` drag-reorder) in scope for any near-term
  consumer, or fully deferred?
- Where exactly is the variable-height bring-into-view ceiling on Avalonia 11.x — does it hold at
  the 10k fixture without an owned realization window?
