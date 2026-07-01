## Why

The Avalonia migration's #1 off-the-shelf gap is an editable, virtualized table: the WinForms
XMLViews stack it replaces (`BrowseViewer` 4,332 / `XmlBrowseViewBase` 2,245 / `BulkEditBar` 7,685
/ `FilterBar` 2,835 LOC) carries editing, bulk-edit, checkbox, filter, and sort, and no shippable
Avalonia control supplies it. TreeDataGrid's FOSS line was archived (2025-10) with editing now
behind the commercial Accelerate/AGPL line; `DataGrid` is an unmaintained Silverlight port; every
capable third party (Eremex, DevExpress) is per-seat commercial — unworkable for an LGPL app on the
11.x pin. So FieldWorks must own this control. It gates Stage 4 (Advanced Entry View), Stage 7
(concordance), and Stage 8 (bulk-edit), so it ships in consumer-gated milestones rather than as one
monolith. Today `LexicalBrowseView` is read-only display only; this change builds it out.

## What Changes

- **3a — Read-only table at scale.** Harden the owned virtualized table (`LexicalBrowseView` over
  `ListBox`/`VirtualizingStackPanel` + the lazy `IBrowseRowSource`/`BrowseRowList` facade): column
  header sort affordance, row/cell selection, keyboard navigation, and **custom AutomationPeers**
  that enumerate de-realized rows for UIA. Prove the 10k-row **scroll/expand** budget (not just
  realization count) on production fixtures at 100%/150% DPI — firing or clearing the
  `VirtualizingStackPanel` pivot trigger (upstream #18626) with recorded numbers.
- **3b — Editable cells.** In-cell editing reusing the owned field controls (`FwMultiWsTextField`,
  `FwChooserField`) inside cells, with selection/keyboard commit-cancel (F2/Enter/Esc/Tab) routed
  through the existing `IEditSession` seam and the `avalonia-undo-redo` capability so the table
  honors one global undo stack. Typing-latency budget enforced via the existing harness.
- **3c — Bulk-edit / checkbox / filter / sort.** Checkbox-select column, multi-column filter and
  sort, and the `BulkEditBar` replacement (its op tabs + custom column editors), replacing the
  fake-flid `XMLViewsDataCache` preview/edit-storage mechanism with a managed in-memory model.
- **Lexical Edit main table view.** Wire FLEx's main lexical-edit browse/table view onto this shared
  control as the first production consumer — read (3a) then editable (3b) — so the Advanced Entry
  View no longer depends on the legacy `BrowseViewer`.

All changes are **managed (C#)** in `Src/Common/FwAvalonia/`; no native (C++) changes.

## Capabilities

### New Capabilities
- `shared-virtualized-table`: The owned virtualized table contract — lazy data + UI virtualization,
  column headers with sort affordance, selection model, keyboard navigation, density/DPI budgets,
  measured scroll/expand budget, and custom AutomationPeers for virtualized rows/cells (3a + peers).
- `editable-table-cells`: In-cell editing through owned field controls, commit/cancel semantics,
  keyboard edit flow, and routing through the edit-session/undo seams (3b).
- `table-bulk-edit-filter-sort`: Checkbox-select column, multi-column filter/sort, and bulk-edit
  preview/apply columns over a managed in-memory model replacing the fake-flid cache (3c).

### Modified Capabilities
<!-- The Lexical Edit main-table wiring is captured as consumer requirements inside the new
     capabilities above; the lexical-edit surface specs are still in-flight under
     openspec/changes/lexical-edit-avalonia-migration and not yet archived into openspec/specs,
     so no archived capability's requirements change here. -->

## Impact

- **Code:** `Src/Common/FwAvalonia/Region/LexicalBrowseView.cs` (extended), new owned table/header/
  cell/selection/peer types under `Src/Common/FwAvalonia/`, new tests under
  `Src/Common/FwAvalonia/FwAvaloniaTests/`. Reuses `IBrowseRowSource`/`BrowseRowList`,
  `FwMultiWsTextField`/`FwChooserField`, `IEditSession`, `FwAvaloniaDensity`, the typing-latency and
  density/DPI harnesses, and `FwOptionPicker`'s virtualization patterns — none re-created.
- **Dependencies:** stays on Avalonia 11.x in-box primitives only; no TreeDataGrid, no `DataGrid`,
  no commercial grid. Records the TreeDataGrid and ItemsRepeater pivot-trigger resolutions.
- **Consumers unblocked:** Stage 4 (via 3b), Stage 7 grid (via 3a), Stage 8 (via 3c).
- **Risk:** scroll smoothness at 10k rows (#18626) and net-new custom AutomationPeers are the two
  understated risks; both are spike-first exit gates.

## Non-goals

- **No virtualized *tree* control.** The detail/slice surface is a flat 253-slice indented stack
  already shipping unvirtualized via `LexicalEditRegionView`; the unbounded chooser is already
  virtualized via `FwOptionPicker`. Indent/expander row chrome only escalates to virtualization if a
  fixture fails its budget — out of scope here.
- **No interlinear/document rendering** (that is Stage 9's managed text engine).
- **No adoption of TreeDataGrid / `DataGrid` / any commercial grid.**
- **No column reorder/resize persistence** unless a consuming stage requires it (deferred).
- **No Stage 4/7/8 surface migration** beyond wiring the Lexical Edit main table view as the
  exemplar consumer.
