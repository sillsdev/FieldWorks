## Speckit to OpenSpec Migration Map

This change migrates `specs/001-render-speedup/*` into OpenSpec.

### Source artifacts

| Speckit Source | OpenSpec Destination | Migration Type |
|---|---|---|
| `spec.md` | `specs/render-speedup-benchmark/spec.md` | Restructured into ADDED requirements with scenarios |
| `tasks.md` | `tasks.md` | Carried over with completion status (T001–T026 + OS-* + OPT-*) |
| `plan.md` | `design.md` | Strategy/constitution merged into design decisions |
| `research.md` | `design.md` (Research Decisions section) | 6 decisions preserved as numbered decisions 5–10 |
| `data-model.md` | `data-model.md` | Carried over with all 7 entities |
| `contracts/render-benchmark.openapi.yaml` | `contracts/render-benchmark.openapi.yaml` | Copied verbatim |
| `quickstart.md` | `quickstart.md` | Copied verbatim |
| `checklists/requirements.md` | `design.md` (Constitution Check section) | Quality checklist folded into design notes |
| `FAST_FORM_PLAN.md` | `research/FAST_FORM_PLAN.md` | Copied verbatim — architecture analysis, algorithms, impl paths |
| `FORMS_SPEEDUP_PLAN.md` | `research/FORMS_SPEEDUP_PLAN.md` | Copied verbatim — 10 optimization techniques with code examples |
| `JIRA_FORMS_SPEEDUP_PLAN.md` | `research/JIRA_FORMS_SPEEDUP_PLAN.md` | Copied verbatim — JIRA story defs, sprint plan, feature flags |
| `views-architecture-research.md` | `research/views-architecture-research.md` | Copied verbatim — native C++ Views deep dive |
| `JIRA-21951.md` | `research/JIRA-21951.md` | Copied verbatim — original problem statement |
| _(new)_ | `research/implementation-paths.md` | Synthesized from FAST_FORM_PLAN Sections 2–4 |
| _(new)_ | `timing-artifact-schema.md` | New artifact for benchmark output schemas |
| `Slow 2026-01-14 1140.fwbackup` | _(not migrated)_ | Binary test data; stays in Speckit folder or is archived separately |

### Data model entities carried over

- Render Scenario
- Render Snapshot
- Benchmark Run
- Benchmark Result
- Trace Event
- Analysis Summary
- Contributor

### Task status carry-over

Speckit task completion state is preserved in `tasks.md` (T001–T026 and pivots), with OpenSpec continuation tasks (`OS-*`) for work started post-migration and optimization tasks (`OPT-*`) for the 10 techniques from FORMS_SPEEDUP_PLAN.

### Key content that was restructured (not copied verbatim)

- **Edge cases** from Speckit `spec.md` → added to OpenSpec `spec.md` Edge Cases section
- **Assumptions** from Speckit `spec.md` → added to OpenSpec `spec.md` Assumptions section
- **Success criteria** (SC-001 through SC-005) → added to OpenSpec `spec.md` Success Criteria section
- **Performance targets** → converted from absolute ms to relative improvement % in OpenSpec `spec.md`
- **Research decisions** (6 decisions from `research.md`) → numbered decisions 5–10 in `design.md`
- **Constitution check** (from `plan.md`) → table in `design.md`
- **Optimization techniques** (10 from FORMS_SPEEDUP_PLAN) → OPT-1 through OPT-11 tasks in `tasks.md`
