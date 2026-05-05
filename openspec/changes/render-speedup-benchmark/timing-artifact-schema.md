## Timing Artifact Schema

This schema defines the required keys for benchmark outputs used in this change.

### Output: `Output/RenderBenchmarks/datatree-timings.json`

Each scenario key maps to:
- depth (int)
- breadth (int)
- slices (int)
- initMs (number)
- populateMs (number)
- totalMs (number)
- density (number)
- timestamp (ISO-8601 string)

Required scenario keys for current DataTree suite:
- simple
- deep
- extreme
- collapsed
- expanded
- multiws
- timing-shallow
- timing-deep
- timing-extreme

### Output: `Output/RenderBenchmarks/results.json`

Per run:
- runId
- runAt
- configuration
- environmentId
- results[] (scenarioId, coldRenderMs, warmRenderMs, variancePercent, pixelPerfectPass)
- summary (topContributors[], recommendations[])

### Comparability rules

- Compare runs only for same configuration + environmentId class.
- Treat workload growth checks (slice monotonicity) as pass/fail guardrails.
- Use trend direction over absolute threshold when hardware differs.
