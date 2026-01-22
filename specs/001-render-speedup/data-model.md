# Data Model: Render Performance Baseline & Optimization Plan

## Entities

### Render Scenario
- **Purpose**: Defines a single benchmark case for a lexical entry.
- **Fields**:
  - `id` (string): Stable identifier (e.g., simple, medium, complex, deep-nested, custom-field-heavy).
  - `description` (string): Human-readable scenario summary.
  - `entrySource` (string): Path or identifier for test data.
  - `expectedSnapshotId` (string): Reference to the approved render snapshot.
  - `tags` (string[]): Category labels (e.g., nested, custom-fields).
- **Validation**:
  - `id` must be unique.
  - `expectedSnapshotId` must exist.

### Render Snapshot
- **Purpose**: Stores approved pixel-perfect baseline output.
- **Fields**:
  - `id` (string): Unique snapshot identifier.
  - `scenarioId` (string): Owning scenario reference.
  - `imagePath` (string): Baseline image file path.
  - `environmentHash` (string): Hash of deterministic environment settings.
  - `createdAt` (datetime): Snapshot creation timestamp.
- **Validation**:
  - `environmentHash` must match the current deterministic environment before validating.

### Benchmark Run
- **Purpose**: Records the timing results for a single suite execution.
- **Fields**:
  - `id` (string): Unique run identifier.
  - `runAt` (datetime): Execution time.
  - `configuration` (string): Build/config identifiers.
  - `environmentId` (string): Deterministic environment identifier.
  - `results` (Benchmark Result[]): Scenario results.
  - `summary` (Analysis Summary): Aggregated analysis for the run.
- **Validation**:
  - Must include results for all five scenarios.

### Benchmark Result
- **Purpose**: Captures measurements for a scenario.
- **Fields**:
  - `scenarioId` (string): Scenario reference.
  - `coldRenderMs` (number): Cold-start render duration.
  - `warmRenderMs` (number): Warm-cache render duration.
  - `variancePercent` (number): Run-to-run variance.
  - `pixelPerfectPass` (boolean): Snapshot comparison result.
- **Validation**:
  - `pixelPerfectPass` must be true for a passing suite run.

### Trace Event
- **Purpose**: Captures rendering stage diagnostics.
- **Fields**:
  - `runId` (string): Benchmark run reference.
  - `stage` (string): Rendering stage name.
  - `startTime` (datetime): Stage start timestamp.
  - `durationMs` (number): Stage duration.
  - `context` (object): Additional metadata (e.g., class, layout name).
- **Validation**:
  - `stage` must be from the approved stage list.

### Analysis Summary
- **Purpose**: Ranks contributors and recommends optimizations.
- **Fields**:
  - `runId` (string): Benchmark run reference.
  - `topContributors` (Contributor[]): Ranked stage time contributions.
  - `recommendations` (string[]): Optimization candidate list.
- **Validation**:
  - At least three recommendations must be present.

### Contributor
- **Purpose**: Represents a ranked timing contributor.
- **Fields**:
  - `stage` (string): Rendering stage.
  - `sharePercent` (number): Percentage of total render time.

## Relationships

- Render Scenario 1..1 → Render Snapshot
- Benchmark Run 1..* → Benchmark Result
- Benchmark Run 1..* → Trace Event
- Benchmark Run 1..1 → Analysis Summary

## State Transitions

- **Render Snapshot**: Draft → Approved → Superseded
- **Benchmark Run**: Pending → Completed → Compared
