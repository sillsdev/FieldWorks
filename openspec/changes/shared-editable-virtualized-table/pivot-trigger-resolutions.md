# Pivot-trigger resolutions (task 1.4)

Records the Stage-3 control-selection pivot triggers and their current state, per
`control-selection-matrix.md` and the Stage-3 review. These are decisions, not measurements — the
*measurement* gates (scroll/expand at real DPI, #18626 condition with numbers) are tasks 1.2/2.7 and
require the running app.

| Pivot trigger | State | Evidence / rationale |
|---|---|---|
| **TreeDataGrid** ("re-evaluate if relicensed permissively / maintained fork emerges") | **Confirmed closed** | FOSS repo archived 2025-10-13; editing + advanced features moved behind the commercial **Avalonia Accelerate** license (v11.2.0+), with the FOSS line under AGPL-3 and frozen. The trigger has moved *further* from firing, not closer. FieldWorks is LGPL and broadly redistributed, so per-seat commercial / AGPL is unworkable. Decision: **own the table** (this change). |
| **ItemsRepeater** ("reconsider only if un-deprecated with maintained virtualization") | **Confirmed closed** | On a deprecation path (kept only for 12.x back-compat); no selection/peers. Not the substrate. The owned table builds on the maintained `ListBox`/`VirtualizingStackPanel` path instead. |
| **`Avalonia.Controls.DataGrid`** | Closed (not adopted) | Unmaintained Silverlight port with long-standing virtualization/editing bugs; nothing over owned headers for template-heavy multi-WS cells. |
| **`VirtualizingStackPanel` scroll/GC (#18626)** | **OPEN — pending measurement (1.2/2.7)** | The realization-count bound is met (`TenThousandRows_RealizeOnlyTheVisibleWindow`), but scroll/expand *latency* on production fixtures at 100%/150% DPI is **not yet measured**. If it fails the budget, escalate to a fully-owned realization-window virtualizer. This is the one trigger that cannot be resolved headlessly — it needs the running app. |

## Net
Three triggers are **confirmed closed** in favor of the owned table; the `VirtualizingStackPanel`
scroll-budget trigger remains **open pending the in-app measurement pass** (tasks 1.2/2.7) and is the
highest remaining technical risk.
