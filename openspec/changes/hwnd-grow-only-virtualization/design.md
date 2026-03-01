## Context

This change addresses the #1 remaining render performance bottleneck in FieldWorks: excessive HWND allocation in the DataTree detail pane. Each of the 73 Slice subclasses creates 6+ Win32 HWNDs (UserControl + SplitContainer + 2×SplitterPanel + SliceTreeNode + content control). A pathological 253-slice lexical entry allocates ~1,500 kernel USER objects just for the detail pane.

The chosen strategy is **grow-only virtualization**: defer HWND creation until a slice scrolls into the viewport, but never destroy an HWND once created. This extends the existing `BecomeRealInPlace` pattern (which already defers RootBox creation for ViewSlices) to also defer the entire Win32 control tree.

Full research is in:
- [research/hwnd-cost-analysis.md](research/hwnd-cost-analysis.md) — why HWNDs are expensive
- [research/slice-catalog.md](research/slice-catalog.md) — all 73 slice subclasses and their HWND profiles
- [research/datatree-state-management.md](research/datatree-state-management.md) — focus, events, layout constraints
- [devils-advocate.md](devils-advocate.md) — per-component-type and per-view risk analysis

## Goals / Non-Goals

**Goals**
- Achieve 80-95% HWND reduction for the common case (user views/edits the first screen of fields in an entry).
- Maintain identical user-visible behavior — no visual changes, no functional regressions.
- Preserve existing focus management, Tab navigation, accessibility, and IME behavior.
- Add measurement infrastructure to quantify and regression-gate HWND counts.
- Deliver incrementally with test gates between each phase.

**Non-Goals**
- Destroying or recycling HWNDs for off-screen slices (high risk, diminishing returns — documented as future Phase 4).
- Replacing WinForms rendering with owner-drawn / windowless controls.
- Modifying the Views engine (`IVwRootBox`) lifecycle or the native C++ layer.
- Cross-platform rendering changes.

## Decisions

### 1) Grow-only, never destroy

Once an HWND is created (when a slice scrolls into view), it persists for the lifetime of the slice. This avoids:
- RootBox destruction / selection loss
- IME composition loss (data loss for CJK users)
- Accessibility tree instability (screen reader position loss)
- Complex event resubscription logic

**Trade-off:** If the user scrolls through ALL 253 slices, all HWNDs will eventually be created. The optimization targets the common case where users interact with a subset of fields.

### 2) Decouple `Slices` list from `Controls` collection

The logical `Slices` list (ordering, indexing, lookup) is separate from the WinForms `Controls` collection (HWND parent-child). Slices are added to `Slices` during `Install()` but only added to `Controls` when `EnsureHwndCreated()` is called.

**Rationale:** This sidesteps the .NET Framework high-water mark bug (LT-7307) where setting `Visible=true` on a child reorders the `Controls` collection. Slices not in `Controls` don't participate in this reordering.

### 3) EnsureHwndCreated() as the single enforcement point

A new method `Slice.EnsureHwndCreated()` is the sole entry point for HWND creation. It:
1. Creates the `SplitContainer` (if not yet created)
2. Creates the `SliceTreeNode` (if not yet created)
3. Adds the slice to `DataTree.Controls`
4. Subscribes HWND-dependent events (SplitterMoved, etc.)

All paths that need an HWND (focus, Tab navigation, `CurrentSlice` setter, `HandleLayout1` visibility) already call `MakeSliceVisible`, which will call `EnsureHwndCreated()`.

### 4) Virtual layout arrays for positioning

Non-HWND slices cannot use `tci.Top` / `tci.Height` (WinForms properties that require a handle). Instead:
- Maintain parallel `int[] _sliceTop` and `int[] _sliceHeight` arrays
- `HandleLayout1` writes to these arrays for non-HWND slices
- `FindFirstPotentiallyVisibleSlice` binary search uses the arrays
- Height for non-HWND slices is estimated from `GetDesiredHeight()` or XML layout hints

### 5) Split Install() into logical and physical phases

- **Logical install** (during `Install()`): Add to `Slices` list, set configuration state (indent, label, weight, expansion), initialize data bindings. No HWND created.
- **Physical install** (during `EnsureHwndCreated()`): Create SplitContainer, SliceTreeNode, content control, add to `Controls`, subscribe HWND events.

### 6) Measurement infrastructure as prerequisite

Before any behavioral changes, establish a baseline:
- `GetGuiResources()` P/Invoke to count USER handles
- NUnit regression gate test asserting HWND count for known entries
- Integration with render benchmark harness for trend tracking

## Risks / Trade-offs

| Risk | Severity | Mitigation |
|------|----------|------------|
| Code accessing `SplitCont` / `TreeNode` before HWND created | Medium | Audit all callers; add null guards; fail-fast with `InvalidOperationException` in debug mode |
| `Validate()` on CurrentSlice that never had HWND | Medium | Guard with `IsHandleCreated` check |
| Height estimation inaccuracy → scrollbar thumb jumps | Low-Med | Use conservative initial heights; correct on scroll-into-view; accept <5% error |
| Properties set via `Control` before HWND (Label, Indent, Weight) | Medium | Store in plain fields; apply to controls in `EnsureHwndCreated()` |
| Third-party code or plugins accessing `Slice.Handle` directly | Low | Unlikely — Slice inheritance is internal; no public extension points |
| Performance of `HandleLayout1` full layout pass over non-HWND slices | Low | Virtual arrays are faster than WinForms property access (no cross-thread marshaling) |

## Open Questions

1. **Should `DummyObjectSlice.BecomeReal()` create HWNDs for expanded slices?** Current plan: No — expanded slices enter the `Slices` list without HWNDs (grow-only). They get HWNDs only if they scroll into view after expansion.

2. **How should `AutoScrollMinSize` be computed?** Option A: Sum of virtual heights (estimated). Option B: High-water position + remaining estimated height. Option A is more accurate; Option B is cheaper.

3. **Should Tab navigation skip non-HWND slices or force their creation?** Current plan: Force creation (via `MakeSliceRealAt` → `EnsureHwndCreated`), matching existing behavior where Tab creates slices as needed.

## Architecture Diagram

```
Before (current):
┌─────────────────────────────────────────────┐
│ DataTree (UserControl)                       │
│ Controls: [Slice0, Slice1, ... Slice252]     │  ← 253 entries in Controls
│ Each Slice:                                  │     each with HWND
│   UserControl(HWND)                          │
│     └── SplitContainer(HWND)                 │
│           ├── Panel1(HWND)                   │
│           │    └── SliceTreeNode(HWND)        │
│           └── Panel2(HWND)                   │
│                └── ContentControl(HWND)       │
│                                              │
│ Total HWNDs: ~1,500                          │
└─────────────────────────────────────────────┘

After (grow-only):
┌─────────────────────────────────────────────┐
│ DataTree (UserControl)                       │
│ Slices: [Slice0, Slice1, ... Slice252]       │  ← 253 entries in Slices (logical)
│ Controls: [Slice5, Slice6, ... Slice19]      │  ← ~15 entries in Controls (visible)
│                                              │
│ Visible Slices (5-19): full HWND tree        │
│ Non-visible Slices: NO HWND, just data       │
│   - config fields (indent, label, weight)    │
│   - virtual layout (top, height arrays)      │
│   - model reference (obj, flid)              │
│                                              │
│ Total HWNDs: ~90 (15 × 6)                   │
└─────────────────────────────────────────────┘
```

## Key Code Paths Affected

### Slice.cs
- Constructor: move SplitContainer creation to `EnsureHwndCreated()`
- `Install()`: split into logical (Slices list) + physical (Controls collection)
- `Label`, `Indent`, `Weight` setters: add null guards for `m_treeNode` / `SplitCont`
- New method: `EnsureHwndCreated()`
- `Dispose()`: handle case where HWND was never created

### DataTree.cs
- `HandleLayout1`: use virtual layout arrays for non-HWND slices; only call Handle-forcing for visible slices
- `MakeSliceVisible`: call `EnsureHwndCreated()` before setting `Visible`
- `FieldAt`: no change needed (only called for visible slices)
- `CurrentSlice` setter: guard `Validate()` with `IsHandleCreated`
- `FindFirstPotentiallyVisibleSlice`: use virtual layout arrays
- New: parallel `_sliceTop[]` / `_sliceHeight[]` arrays
- `OnLayout`: compute `AutoScrollMinSize` from virtual heights

### ViewSlice.cs
- `BecomeRealInPlace()`: may need to call `EnsureHwndCreated()` first if not already called
- Event subscriptions: move from `Install()` to `EnsureHwndCreated()`

### SliceTreeNode.cs
- No changes to the class itself; creation is deferred

## Alternatives Considered

Three approaches were analyzed in detail (see [research/hwnd-cost-analysis.md](research/hwnd-cost-analysis.md)):

| Approach | Reduction | Risk | Effort | Verdict |
|----------|-----------|------|--------|---------|
| **A: Grow-only virtualization** (this change) | 80-95% common case | Low | 7-8 weeks | **Selected** |
| **B: Windowless labels** (owner-draw SliceTreeNode + SplitContainer) | 66% standalone, 98% with A | Medium-High | 6-10 weeks | Deferred — independent, complementary |
| **C: Single-surface composited rendering** | 99.5% | Very High | 3-6 months | Deferred — requires Views engine changes |

Grow-only was selected because it:
- Extends the existing `BecomeRealInPlace` pattern (proven, familiar)
- Has the lowest risk profile (no HWND destruction, no state save/restore)
- Gets the majority of the benefit for the common case
- Can be delivered incrementally with test gates at each phase
