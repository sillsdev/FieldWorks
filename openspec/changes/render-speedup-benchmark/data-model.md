## Data Model (Migrated from Speckit 001)

### Render Scenario
- id, description, entrySource, expectedSnapshotId, tags
- id is unique; expectedSnapshotId must resolve

### Render Snapshot
- id, scenarioId, imagePath, environmentHash, createdAt
- environmentHash must match current deterministic environment for validation

### Benchmark Run
- id, runAt, configuration, environmentId, results, summary
- must contain all five required scenarios for full suite

### Benchmark Result
- scenarioId, coldRenderMs, warmRenderMs, variancePercent, pixelPerfectPass
- pixelPerfectPass must be true for suite pass

### Trace Event
- runId, stage, startTime, durationMs, context
- stage must be from approved render-stage list

### Analysis Summary
- runId, topContributors, recommendations
- recommendations count >= 3

### Contributor
- stage, sharePercent
