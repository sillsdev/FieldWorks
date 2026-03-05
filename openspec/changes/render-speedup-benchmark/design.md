## Context

This change migrates the complete Speckit `001-render-speedup` workstream into OpenSpec and continues delivery from current code state. The migrated scope includes:

- RootSite baseline and timing suite infrastructure.
- File-based render trace diagnostics and parser/report integration.
- DataTree render harness extensions and timing-fidelity fixes.

## Goals / Non-Goals

**Goals**
- Preserve the three Speckit user stories in OpenSpec:
	- Pixel-perfect baseline (P1)
	- Five-scenario timing suite (P2)
	- Render-stage trace diagnostics (P3)
- Keep deterministic environment safeguards for pixel-perfect validation.
- Enforce benchmark-fidelity invariants so deeper scenarios represent larger work.
- Drive optimization with measurable evidence and regression safety gates.

**Non-Goals**
- Full architectural rewrite of DataTree/Views.
- Behavior-changing UI redesign.
- External telemetry or service dependencies.

## Decisions

### 1) Full Speckit carry-over with status preservation

- Carry over FR/SC and task IDs semantically (including completed and pending status) into OpenSpec tasks.
- Keep Speckit files as source references during transition; OpenSpec becomes authoritative moving forward.

### 2) Separate confidence channels by purpose

- Snapshot channel verifies visual correctness and deterministic output.
- Timing channel emphasizes repeatability and workload scaling.
- Trace channel attributes time to native rendering stages.

### 3) Benchmark-fidelity guardrails are mandatory

- Timing scenarios must show monotonic workload growth indicators.
- Timing tests fail fast when expected complexity is not exercised.

### 4) Optimization loop is evidence-first

- Baseline -> change -> remeasure on same scenarios.
- Require non-regressing snapshots and timing evidence for acceptance.

## Risks / Trade-offs

- Snapshot and timing channels may use different internals for stability; risk mitigated by documenting each channel's intent.
- Native trace instrumentation carries low-level regression risk; mitigate with scoped trace switches and targeted tests.
- Timing variance across machines complicates absolute thresholds; use trend comparisons and workload invariants.

## Open Questions

1. Should benchmark comparison output (`comparison.md`) become a required artifact for optimization PRs?
2. Should pending native trace tasks be completed in this change or tracked as a follow-on OpenSpec change if blocked by native build constraints?

## Research Decisions (carried over from Speckit)

These decisions were made during the original Phase 0 research and remain in effect.

### 5) Pixel-perfect validation via deterministic environment control
- **Decision**: Enforce fixed fonts, DPI, and theme with zero tolerance image comparison.
- **Rationale**: Guarantees strict pixel identity and avoids false positives from tolerances.
- **Alternatives rejected**: Image diff with tolerance (weakens correctness guarantees); layout tree snapshot (lower fidelity, misses paint issues).

### 6) Cold + warm render metrics
- **Decision**: Report cold-start and warm-cache render timings separately.
- **Rationale**: Cold captures first-load cost; warm captures steady-state performance.
- **Alternatives rejected**: Single "best of 3" (hides cold-start regressions); median of 5 (blurs warm vs cold).

### 7) Fixed five timing scenarios
- **Decision**: Use five scenarios: simple, medium, complex, deep-nested, custom-field-heavy.
- **Rationale**: Balanced coverage for common and worst-case entries without excessive runtime.
- **Alternatives rejected**: Minimum three (insufficient coverage); ten (longer runtime, higher maintenance).

### 8) File-based trace logging
- **Decision**: Append-only file output with timestamps and durations.
- **Rationale**: Low overhead, aligns with existing FieldWorks tracing practices.
- **Alternatives rejected**: ETW/EventSource (more setup); UI panel trace (adds measurement overhead).

### 9) Harness approach: offscreen render capture
- **Decision**: Use managed WinForms offscreen rendering (Option 2) with `DummyBasicView`/RootSite layout and `Control.DrawToBitmap`-style capture.
- **Rationale**: Fastest integration path with existing managed test scaffolding and deterministic capture.
- **Alternatives rejected**: Native Views offscreen render (higher fidelity but more complex); on-screen capture (window manager variability); layout-only verification (insufficient for visual correctness).
- **Pivot note**: T009a/T009b track potential pivot to native capture and production StVc if `DrawToBitmap` limitations prove blocking.

### 10) Initial optimization candidates
- **Decision**: Start with layout/DisplayCommand caching, custom-field collapse, and field-level virtualization/lazy creation.
- **Rationale**: Targets high-impact bottlenecks in XML layout interpretation and control creation.
- **Alternatives rejected**: Full views engine replacement (too large for initial phase).
- **Reference**: See `research/FORMS_SPEEDUP_PLAN.md` for the 10 optimization techniques and `research/implementation-paths.md` for strategic paths A/B/C.

## Quickstart

Detailed build, run, snapshot generation, diagnostics, and output artifact documentation is in [quickstart.md](quickstart.md) (carried over from Speckit).

## Key Architecture Findings

The `research/` folder contains the full analysis. Key points for context:

- **Worst-case complexity**: O(M × N × D × F × S) where M=senses, N=subsenses, D=layout depth, F=custom fields, S=source XML size.
- **No field-level virtualization**: `VwLazyBox` virtualizes at vector property level only; within each expanded item, all fields render immediately.
- **Custom field explosion**: Each custom field triggers XML subtree cloning via `PartGenerator.Generate` — O(F × S × A).
- **COM boundary**: Every `IVwEnv` call crosses managed/native boundary synchronously.
- **Layout recomputation**: No incremental/dirty-region optimization at the managed layer; property changes trigger full subtree re-layout.
- **Known bottlenecks**: `VwSelection.cpp` comments explicitly note performance issues; `VwRootBox` has 1/10 second timeout for `MakeSimpleSel`.
- **Original problem statement**: DataTree needs redesign; current architecture intermixes view model (XML layouts), data model (liblcm), and view (Slices with XML nodes + HVO/flid). See `research/JIRA-21951.md`.

## Constitution Check (carried over from Speckit)

| Category | Status | Notes |
|----------|--------|-------|
| Data Integrity / Backward Compatibility | PASS | No schema/data changes in scope |
| Test & Review Discipline | PASS | Harness, timing suite, and trace validation included |
| Internationalization / Script Correctness | PASS | Pixel-perfect validation must include complex scripts |
| Stability & Performance | PASS | Feature flags / staged rollout for optimizations; baseline first |
| Licensing | PASS | No new external dependencies or services |
| Documentation Fidelity | PASS | Plan includes updating docs when code changes |
