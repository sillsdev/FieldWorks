# Stage 3 — Shared editable virtualized table + row chrome (Epic — **finished spec**)

> Status: **implementation-ready spec; the build itself is the program's largest foundation effort and is
> multi-session.** Re-scoped from "grid AND tree" per `reviews/stage-03-virtualized-grid-tree.md`.
>
> **Implementation underway** in change `shared-editable-virtualized-table` (branch `editable-table`).
> Control-layer done + verified (317/317 FwAvalonia tests, 0 regressions): **3a** sortable headers,
> selection, keyboard nav, de-realized programmatic selection; **3b** inline text + chooser cell
> editing through `IEditSession` (Enter commits+advances, Esc cancels); **3c** checkbox-select column,
> column filter, multi-column sort, bulk-edit preview/apply over a managed in-memory model; **3d** the
> table peer synthesizes a peer per row (de-realized rows enumerated); **product wiring (4)** —
> the Lexicon Browse tool now overlays the Avalonia table behind `UIMode=New` via the same
> `WinFormsAvaloniaControlHost` bridge the edit surface uses, with a clerk-backed read-only adapter
> (faithful cells via `BrowseViewer.GetRowCellStrings`) forwarding selection to the clerk; the legacy
> `BrowseViewer` stays the default and functional fallback. Full managed build green, 354/354 headless
> (consolidated branch; the browse gate covers both `lexiconEdit` and `lexiconBrowse`).
> Remaining: in-app/real-DPI manual verification (4.3, 2.7), in-product browse *editing* (6),
> `BulkEditBar` per-op census follow-ups (7.4 done as a doc), desktop UIA2/FlaUI (3.3), typing-latency
> gate (5.5), and the spike measurement record (1.x).

## Epic
- **Summary:** Build the one owned, editable, virtualized **table** (browse/XMLViews replacement) plus
  indented row chrome that Stages 4, 7, and 8 depend on — the #1 off-the-shelf gap.
- **Type:** Epic · **Labels:** `track-foundation`, `lead-senior` · **Size:** XL
- **Description:** The **tree half is already solved** — the detail surface is a flat indented stack
  (rendered today by `LexicalEditRegionView`, budget tops at 253 slices, below where virtualization matters)
  and the unbounded chooser is **already virtualized** (`FwOptionPicker` over `VirtualizingStackPanel`). The
  real, unsolved work is the **editable table**: `LexicalBrowseView` is **read-only**, and the legacy
  surface it replaces (`BrowseViewer` 4,332 / `XmlBrowseViewBase` 2,245 / `BulkEditBar` 7,685 /
  `FilterBar` 2,835) carries editing, bulk-edit, checkbox, filter, and sort.
- **Acceptance criteria:** the owned table passes 10k-row browse **and** 253-slice detail at 100%/150% DPI
  within the measured legacy perf budget, with **scroll/expand measured on production fixtures** (not just
  realization count), editing + selection + keyboard, and custom AutomationPeers.
- **Dependencies:** **gates Stage 4 (via 3b), Stage 7B/7C, Stage 8 (via 3c).** No TreeDataGrid (FOSS repo
  **archived 2025-10**; editing behind a commercial license — the pivot trigger moved *further* from firing).

## Sub-epics / stories (ship consumer-gated, in order)

### 3a Read-only table at scale  · Story · L
- Owned virtualized table rendering 10k rows within budget; promote `LexicalBrowseView` from its current
  read-only state onto the shared control. **Unblocks Stage 7/8 display.**
- **Acceptance:** 10k-row + scroll/expand on production fixtures within budget at 150% DPI.

### 3b Editable cells  · Story · L  *(top priority — unblocks Stage 4)*
- Cell editing, selection model, keyboard navigation, commit/cancel through the existing edit-session seam.
- **Acceptance:** Stage-4 entry tables edit at parity; one global undo stack honored.

### 3c Bulk-edit / checkbox / filter / sort  · Story · XL
- The `BulkEditBar` replacement (6 op tabs + custom column editors), checkbox column, `FilterBar`, sort.
  **Unblocks Stage 8.** Can overlap Track II.
- **Acceptance:** bulk operations + filtering at parity on the 10k-row fixture.

### 3d Custom AutomationPeers (first-class)  · Story · M
- Virtualized UIA enumeration of de-realized rows (Avalonia's `ItemContainerGenerator` doesn't retain
  containers). **Zero custom AutomationPeers exist in-repo today** — this is net-new and non-trivial.
- **Acceptance:** UIA tree exposes rows/cells with stable AutomationIds under virtualization.

## Notes / open questions
- Spike first (3a) and **record the `VirtualizingStackPanel` pivot fire/clear with numbers** — its
  scroll/GC cost is a known unresolved upstream issue (#18626); escalate to a fully-owned realization
  window only if production fixtures fail.
- Confirm with the Stage 7 owner whether the concordance grid needs 3b editing or only 3a display, to
  refine the `S3 → S7` edge.
