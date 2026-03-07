## ADDED Requirements

### Requirement: Pixel-perfect baseline harness for lexical entry rendering

The system SHALL provide a deterministic render-timing harness for lexical entries that validates pixel-perfect output against approved snapshots with zero visual tolerance.

#### Scenario: Baseline passes with deterministic environment

- **WHEN** a reference entry is rendered in a matching deterministic environment (font/DPI/theme hash)
- **THEN** the harness SHALL produce timing metrics
- **AND** snapshot validation SHALL pass pixel-perfect comparison

#### Scenario: Baseline fails on visual mismatch

- **WHEN** rendered output differs from approved snapshot
- **THEN** snapshot validation SHALL fail and report mismatch diagnostics

### Requirement: Five-scenario timing suite with cold and warm metrics

The system SHALL execute exactly five benchmark scenarios (simple, medium, complex, deep-nested, custom-field-heavy) and report cold and warm render timings.

#### Scenario: Suite emits complete results

- **WHEN** the timing suite runs
- **THEN** results SHALL include all five scenarios
- **AND** each scenario SHALL include cold and warm timing values plus summary metrics

### Requirement: Benchmark run metadata and comparison support

Each benchmark run SHALL record run metadata and support comparison against prior runs to identify regressions and improvements.

#### Scenario: Run metadata is present

- **WHEN** a benchmark run completes
- **THEN** output SHALL include run timestamp, configuration, and environment identifier

#### Scenario: Run comparison highlights regression

- **WHEN** a run is compared with a baseline run
- **THEN** comparison output SHALL flag regressing and improving scenarios

### Requirement: File-based render trace diagnostics

The system SHALL emit append-only, timestamped trace diagnostics with durations for key render stages and allow diagnostics to be toggled.

#### Scenario: Trace enabled

- **WHEN** diagnostics are enabled and rendering runs
- **THEN** trace output SHALL contain stage timing events in file-based append-only format

#### Scenario: Trace disabled

- **WHEN** diagnostics are disabled
- **THEN** benchmark execution SHALL proceed without trace-stage emission overhead

### Requirement: Analysis summary and optimization guidance

The benchmark pipeline SHALL produce an analysis summary listing top contributors and optimization candidates.

#### Scenario: Summary includes contributors and recommendations

- **WHEN** benchmark + trace data are available
- **THEN** summary SHALL rank top time contributors
- **AND** provide at least three optimization recommendations targeting nested/custom-heavy entry performance

### Requirement: Pixel-perfect is a hard gate for timing suite pass

Timing suite execution SHALL fail if snapshot validation fails for any required scenario.

#### Scenario: Snapshot failure fails suite

- **WHEN** any scenario fails pixel-perfect validation
- **THEN** timing suite result SHALL be failed even if timing metrics were collected

### Requirement: DataTree timing scenarios must reflect workload growth

DataTree timing scenarios SHALL exercise increasing render workload as scenario depth/breadth grows.

#### Scenario: Benchmark complexity monotonicity

- **WHEN** shallow, deep, and extreme DataTree timing scenarios are executed
- **THEN** workload indicators (including slice count) SHALL increase monotonically shallow < deep < extreme
- **AND** the test SHALL fail if workload growth is not observed

### Requirement: No external services for harness and timing suite

Harness, timing suite, and diagnostics workflow SHALL run locally without external service dependencies.

#### Scenario: Local-only operation

- **WHEN** tests run in an offline local environment with repo dependencies
- **THEN** benchmark and validation workflows SHALL complete without external service calls

## Edge Cases

- Entry contains no custom fields and no senses — harness must still produce valid timing and snapshot.
- Extremely large nested entries with deep sense hierarchies (5+ levels, 20+ senses per level) — suite must complete without timeout or crash.
- Environment changes (fonts, DPI, theme) invalidate snapshots — harness must detect and fail with environment-mismatch diagnostic rather than a confusing pixel diff.
- Timing variance exceeding 5% tolerance — repeated runs must be flagged rather than silently accepted.
- Render output affected by ClearType, text scaling, or window DPI — harness enforces deterministic settings via environment hash validation.

## Assumptions

- Pixel-perfect validation relies on deterministic environment control (fixed fonts, DPI, and theme) with zero tolerance.
- Test data for scenarios can be stored in-memory via LCModel test infrastructure and reused without manual re-creation.
- Timing variance of ±5% is acceptable for determining baseline trends across runs on the same machine.
- No external services are required for the harness, timing suite, or diagnostics workflow.

## Performance Targets

Performance targets are expressed as **relative improvement %** over the measured baseline, not absolute millisecond thresholds. This accounts for hardware variation across developer machines and CI environments.

| Scope | Metric | Target |
|-------|--------|--------|
| Simple entry load | Form load time | ≥60% reduction from baseline |
| Complex entry load | Form load time | ≥70% reduction from baseline |
| Custom fields expand | Section expand time | ≥80% reduction from baseline |
| Memory per entry | Working set delta | ≥50% reduction from baseline |
| Handle count | Window handles | ≥70% reduction from baseline |

These targets guide the optimization phase (Phase 7). Each optimization is measured against its own before/after baseline run.

## Success Criteria

- SC-001: The timing harness produces a pixel-perfect pass/fail result for the reference entry on every run.
- SC-002: Each timing scenario completes with run-to-run variance at or below 5% across three consecutive runs.
- SC-002a: Each timing scenario reports both cold-start and warm-cache timings.
- SC-003: The suite includes exactly five scenarios (simple, medium, complex, deep-nested, custom-field-heavy).
- SC-004: Each benchmark run produces a report that identifies the top five time contributors and their share of total render time.
- SC-005: The analysis summary lists at least three optimization candidates focused on reducing growth in nested or custom-field-heavy entries.
