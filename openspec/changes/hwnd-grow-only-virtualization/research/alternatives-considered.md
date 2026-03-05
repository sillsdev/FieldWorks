# Alternatives Considered

Three approaches to reducing HWND count in DataTree were evaluated during the research phase. This document captures the full analysis for each, including the rationale for selecting Approach A (grow-only virtualization).

---

## Approach A: Grow-Only Virtualized Container (SELECTED)

**Concept:** Defer HWND creation until a slice scrolls into the viewport. Never destroy an HWND once created. Extend the existing `BecomeRealInPlace` pattern from "defer RootBox" to "defer entire HWND tree."

**HWND Reduction:** 80-95% for common case (user edits first screen); 0% for full scroll-through.

| Dimension | Assessment |
|-----------|-----------|
| Risk | **Low** — no state destruction, no IME loss, no accessibility churn |
| Effort | **7-8 weeks** (3 delivery phases + baseline phase) |
| Incremental delivery | **Yes** — each phase independently testable |
| Extends existing patterns | **Yes** — BecomeRealInPlace, HandleLayout1 visibility check, MakeSliceVisible |
| User-visible impact | **None** — slices render identically once HWND'd |
| Main weakness | Scrollbar accuracy for estimated heights; HWND count grows monotonically |

**Selected because:** Lowest risk, highest reward-per-effort, extends proven patterns, delivers incrementally.

---

## Approach B: Windowless Labels (Owner-Draw SliceTreeNode + SplitContainer)

**Concept:** Replace the per-slice `SplitContainer` + `SliceTreeNode` with a single owner-drawn panel that renders labels, tree lines, and expansion indicators using GDI+. Each slice's left-side "label" becomes a painted region rather than a windowed control.

**HWND Reduction:** 66% standalone (eliminate 4 of 6 HWNDs per slice: SplitContainer + Panel1 + Panel2 + SliceTreeNode). Combined with Approach A: 98%.

| Dimension | Assessment |
|-----------|-----------|
| Risk | **Medium-High** — must reimplement hit-testing, focus visuals, accessibility, tooltip positioning, and tree node expand/collapse behavior |
| Effort | **6-10 weeks** |
| Incremental delivery | **Partially** — label panel must be built before any slice can use it |
| Extends existing patterns | **No** — entirely new rendering approach for the label column |
| User-visible impact | **Potential** — label rendering fidelity, focus indicators, and tree lines must exactly match current appearance |
| Main weakness | Large surface area of custom painting; must handle DPI scaling, theme changes, high contrast mode |

**Why deferred:**
- High risk of visual regressions in the label column
- Accessibility for owner-drawn regions requires implementing `IAccessible` manually
- Does not address the content-control HWNDs (the most expensive ones)
- Can be implemented independently of Approach A as a complementary follow-on

**Could be combined with Approach A for 98% reduction.**

---

## Approach C: Single-Surface Composited Rendering

**Concept:** DataTree becomes a single HWND. All slices, labels, content, and tree structure are rendered onto one surface via the Views engine or custom GDI+ painting. No child HWNDs at all.

**HWND Reduction:** 99.5% (DataTree HWND + 1-2 floating HWNDs for focused-field editing = 2-3 total).

| Dimension | Assessment |
|-----------|-----------|
| Risk | **Very High** — requires rewriting the entire DataTree rendering pipeline |
| Effort | **3-6 months** |
| Incremental delivery | **No** — must be done as a single large migration |
| Extends existing patterns | **No** — fundamentally different architecture |
| User-visible impact | **High** — all rendering is custom; must recreate every visual detail |
| Main weakness | Requires Views engine modifications for embedding editable text surfaces within a single HWND; focus/selection management becomes entirely custom |

**Why deferred:**
- Scope is too large for the current optimization phase
- Requires modifying the native Views engine (`IVwRootBox`, `VwEnv`) which is C++ and high-risk
- Testing burden is enormous — every slice type must be regression-tested for visual and functional correctness
- Approach A gets 80-95% of the benefit at 10% of the cost and risk
- If Approach A + B prove insufficient, Approach C remains a viable long-term architecture target

---

## Decision Summary

| Approach | Reduction | Risk | Effort | Selected? |
|----------|-----------|------|--------|-----------|
| **A: Grow-only** | 80-95% common case | Low | 7-8 weeks | **Yes** |
| **B: Windowless labels** | 66% standalone | Medium-High | 6-10 weeks | Deferred (complementary) |
| **C: Single-surface** | 99.5% | Very High | 3-6 months | Deferred (long-term) |

Approaches B and C are not rejected — they are deferred as follow-on work that can be layered on top of Approach A if the grow-only ceiling proves insufficient for real-world usage.
