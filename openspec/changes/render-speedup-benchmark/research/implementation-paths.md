# Implementation Paths Analysis

**Source**: Synthesized from `FAST_FORM_PLAN.md` Sections 2–4
**Date**: January 2026

## Summary

Three strategic paths exist for addressing render performance. This change (render-speedup-benchmark) establishes the measurement infrastructure needed to evaluate any of them. The selected path should be driven by benchmark evidence.

## Path A: Optimize Current WinForms + Views (Incremental)

- Keep existing architecture, add targeted optimizations.
- Key changes: field-level collapse-by-default, DisplayCommand caching, incremental layout, XML caching.
- **Effort**: Low–Medium | **Risk**: Low | **Performance Gain**: 2–3× | **Time to first value**: 1–2 months
- Pros: minimal UI changes, incremental delivery, preserves functionality.
- Cons: bounded by COM overhead, doesn't address C++ Views complexity, accumulates debt.
- **Recommended for**: quick wins while planning migration.

## Path B: Hybrid Rendering (WinForms Host + Avalonia Controls)

- Replace specific slow controls with Avalonia equivalents hosted in WinForms.
- Key changes: Avalonia-based entry editor with virtualized TreeDataGrid for senses, WinForms shell retained.
- **Effort**: Medium | **Risk**: Medium | **Performance Gain**: 5–10× | **Time to first value**: 3–4 months
- Pros: targeted improvement, incremental migration, maintains UX.
- Cons: two UI frameworks, styling inconsistencies, interop complexity.
- **Recommended for**: when full migration timeline is uncertain.

## Path C: Full Avalonia Migration with Presentation IR

- Complete migration to Avalonia with a Presentation IR layer compiled from Parts/Layout XML.
- Key changes: IR compiler, IR→ViewModel mapping, TreeDataGrid-based entry editor, field-level virtualization.
- **Effort**: Medium–High | **Risk**: Medium | **Performance Gain**: 10–50× | **Time to first value**: 4–6 months
- Pros: best long-term architecture, full virtualization, no COM overhead, cross-platform.
- Cons: highest initial effort, regression risk, team needs Avalonia expertise.
- **Recommended for**: long-term strategic direction.

## Staged Approach (Recommended)

The FAST_FORM_PLAN recommends a multi-phase roll-out:

| Phase | Timeline | Focus |
|-------|----------|-------|
| 1 | Months 1–2 | Quick wins in current system (collapse-by-default, DisplayCommand caching) |
| 2 | Months 2–4 | Presentation IR foundation (IR compiler, IR→ViewModel mapping) |
| 3 | Months 4–6 | Avalonia entry editor (virtualized TreeDataGrid, real LCModel data) |
| 4 | Months 6+ | Full migration (replace remaining Views editors, deprecate C++ Views) |

## Comparison Matrix

| Dimension | Path A | Path B | Path C | Full Rewrite |
|-----------|--------|--------|--------|-------------|
| Effort | Low–Med | Medium | Med–High | Very High |
| Risk | Low | Medium | Medium | High |
| Perf Gain | 2–3× | 5–10× | 10–50× | 10–50× |
| Maintainability | Worse | Mixed | Better | Best |
| Time to Value | 1–2 mo | 3–4 mo | 4–6 mo | 12+ mo |
| Cross-Platform | No | Limited | Yes | Yes |

## Key Insight

The benchmark and measurement infrastructure built in this change is path-independent — it validates improvements regardless of which path is pursued. The optimization tasks added here focus on Path A quick wins that can be measured immediately.
