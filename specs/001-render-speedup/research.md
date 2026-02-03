# Research Notes: Render Performance Baseline & Optimization Plan

## Decisions

### 1) Pixel-perfect validation via deterministic environment control
- **Decision**: Enforce fixed fonts, DPI, and theme with zero tolerance image comparison.
- **Rationale**: Guarantees strict pixel identity and avoids false positives from tolerances.
- **Alternatives considered**:
  - Image diff with tolerance: reduces false positives but weakens correctness guarantees.
  - Layout tree snapshot: lower fidelity for visual correctness and misses paint issues.

### 2) Cold + warm render metrics
- **Decision**: Report cold-start and warm-cache render timings separately.
- **Rationale**: Cold captures first-load cost; warm captures steady-state performance.
- **Alternatives considered**:
  - Single “best of 3” metric: hides cold-start regressions.
  - Median of 5: blurs warm vs cold differences.

### 3) Fixed five timing scenarios
- **Decision**: Use five scenarios: simple, medium, complex, deep-nested, custom-field-heavy.
- **Rationale**: Balanced coverage for common and worst-case entries without excessive runtime.
- **Alternatives considered**:
  - Minimum three scenarios: insufficient coverage for custom/nested entry regressions.
  - Ten scenarios: better coverage but longer runtime and higher maintenance.

### 4) File-based trace logging
- **Decision**: Append-only file output with timestamps and durations.
- **Rationale**: Low overhead, aligns with existing FieldWorks tracing practices.
- **Alternatives considered**:
  - ETW/EventSource: higher fidelity but more setup and complexity.
  - UI panel trace: visible but adds measurement overhead.

### 5) Harness approach: offscreen render capture
- **Decision**: Use managed WinForms offscreen rendering (Option 2) with `DummyBasicView`/RootSite layout and `Control.DrawToBitmap`-style capture to a bitmap for pixel-perfect comparison.
- **Rationale**: Fastest integration path with existing managed test scaffolding and deterministic capture for baseline checks.
- **Alternatives considered**:
  - Native Views offscreen render (Option 1): higher fidelity but more complex native harness work.
  - On-screen capture: introduces window manager variability.
  - Layout-only verification: insufficient for visual correctness.

### 6) Initial optimization candidates to evaluate
- **Decision**: Start with layout/DisplayCommand caching, custom-field collapse, and field-level virtualization/lazy creation.
- **Rationale**: Targets high-impact bottlenecks in XML layout interpretation and control creation.
- **Alternatives considered**:
  - Full views engine replacement: too large for initial optimization phase.

## Sources

- specs/011-faster-winforms/FAST_FORM_PLAN.md
- specs/011-faster-winforms/FORMS_SPEEDUP_PLAN.md
- specs/011-faster-winforms/views-architecture-research.md
- specs/011-faster-winforms/JIRA_FORMS_SPEEDUP_PLAN.md
- specs/011-faster-winforms/JIRA-21951.md
