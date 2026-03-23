## Why

We are migrating the full Speckit `specs/001-render-speedup` scope into OpenSpec and continuing implementation from there. This scope combines three linked goals: deterministic pixel-perfect render validation, repeatable timing baselines across fixed scenarios, and trace diagnostics for render-stage attribution. Recent DataTree benchmark improvements surfaced a fidelity gap (deep scenarios not always exercising true deep workload), so migration must preserve existing completed work and explicitly track remaining items.

## What Changes

- Carry over all Speckit user stories, FR/SC requirements, and tasks into OpenSpec with completion status preserved.
- Keep the benchmark harness and five-scenario timing suite as first-class capabilities.
- Preserve trace-diagnostics requirements for native Views (`VwRootBox`, `VwEnv`, lazy expansion paths).
- Add DataTree benchmark-fidelity guardrails so scenario complexity growth is enforced.
- Continue optimization work using measured before/after evidence and snapshot safety gates.

## Non-goals

- Rewriting the DataTree or Views architecture in this change.
- Replacing WinForms UI framework.
- Introducing external services or non-repo dependencies.

## Capabilities

### New Capabilities

- `render-speedup-benchmark`: Pixel-perfect render baseline + five-scenario timing suite + benchmark artifacts.
- `render-trace-diagnostics`: File-based render-stage diagnostics, parsing, and top-contributor summaries.
- `datatree-benchmark-fidelity`: DataTree timing path with monotonic workload guardrails.

### Modified Capabilities

- `architecture/ui-framework/views-rendering`: Add benchmark and tracing guidance for managed/native render analysis.

## Impact

- Managed test/harness code under `Src/Common/RootSite/RootSiteTests/`, `Src/Common/RenderVerification/`, and `Src/Common/Controls/DetailControls/DetailControlsTests/`.
- Native trace instrumentation surfaces under `Src/views/`.
- Benchmark artifacts under `Output/RenderBenchmarks/`.
- Migration references retained to Speckit source under `specs/001-render-speedup/` for auditability.
