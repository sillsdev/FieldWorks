# PerformOffscreenLayout Optimization Plan (Paths 1+2+5)

## Purpose

Define the implementation plan for three native text-layout optimization paths targeting `PerformOffscreenLayout`:

- Path 1: ShapePlaceRun result caching (revised from FindBreakPoint memoization)
- Path 2: Text analysis cache (`ScriptItemize`, NFC buffers, and non-NFC offset maps)
- Path 5: Common-case fast path in line breaking

This plan includes:

- concrete code changes,
- phased task list,
- expected savings grounded in the current render run,
- edge-case test coverage (unit + render/timing),
- explicit pre-implementation decisions that still need to be made.

## Current Evidence (2026-03-11)

Source artifacts:

- `Output/RenderBenchmarks/summary.md`
- `Output/RenderBenchmarks/results.json`
- `Output/RenderBenchmarks/summary.cache-on-final.md`
- `Output/RenderBenchmarks/results.cache-on-final.json`
- `Output/RenderBenchmarks/summary.cache-off.md`
- `Output/RenderBenchmarks/results.cache-off.json`

Latest cache-enabled timing-suite run `aeead2c451e2426e92a8b945790d5e80` (`2026-03-11 15:53:56Z`):

- Flags: `FW_PERF_P125_PATH1=1`, `FW_PERF_P125_PATH2=1`, `FW_PERF_P125_PATH5=not-implemented`
- Avg cold render: 110.90 ms
- Avg warm render: 43.72 ms
- Avg cold `PerformOffscreenLayout`: 90.54 ms
- Avg warm `PerformOffscreenLayout`: 1.81 ms
- `PerformOffscreenLayout` remains the dominant traced stage (55.6% of traced stage time)

Matched cache-disabled control run `e2167ab0e60f4dd0a944601e266ca3aa` (`2026-03-11 15:52:23Z`):

- Flags: `FW_PERF_P125_PATH1=0`, `FW_PERF_P125_PATH2=0`, `FW_PERF_P125_PATH5=not-implemented`
- Avg cold render: 156.44 ms
- Avg warm render: 77.04 ms
- Avg cold `PerformOffscreenLayout`: 136.80 ms
- Avg warm `PerformOffscreenLayout`: 2.27 ms

Observed cache-on improvements versus control:

- Avg cold render: 156.44 → 110.90 ms (`-45.54 ms`, about `-29.1%`)
- Avg cold `PerformOffscreenLayout`: 136.80 → 90.54 ms (`-46.26 ms`, about `-33.8%`)
- Avg warm render: 77.04 → 43.72 ms (`-33.32 ms`, about `-43.2%`)

Representative heavy-scenario cold `PerformOffscreenLayout` wins from the matched control pair:

- `footnote-heavy`: 459.00 → 139.65 ms
- `many-paragraphs`: 194.56 → 63.61 ms
- `complex`: 191.27 → 68.40 ms
- `deep-nested`: 97.05 → 30.76 ms
- `medium`: 89.49 → 28.11 ms
- `custom-heavy`: 84.70 → 35.16 ms

Some scenarios were noisy or slightly worse in the single-run control pair (`lex-deep`, `lex-extreme`, `multi-book`, `multi-ws`). Treat the current pair as strong directional evidence, not the final tuned benchmark set.

Historical pre-implementation benchmark reference `1b193fb08a1b477f9419b59b0ec8d78f` (`2026-03-10 19:33:16Z`):

- `long-prose`: 149.90/152.92 ms (98.0%)
- `mixed-styles`: 109.37/115.23 ms (94.9%)
- `rtl-script`: 67.28/70.09 ms (96.0%)
- `lex-extreme`: 82.12/86.39 ms (95.1%)

## Branch Context: Optimizations Already Landed

This plan was originally drafted against an older benchmark snapshot. On the current
`001-render-speedup` branch, several related optimizations have already changed the baseline:

- `PATH-L1` width-invariant layout guard is complete and validated.
- `PATH-L4` harness GDI resource caching is complete.
- `PATH-R1` reconstruct guard is complete.
- `PATH-C1` HFONT cache and `PATH-C2` color-state cache are complete.
- `PATH-N1` NFC-aware fast path is complete in the Uniscribe pipeline.

Implications for this plan:

1. Warm-path protection is already excellent and must not regress.
2. Cold-path numbers must now be evaluated against the current 49.22 ms baseline, not the older 64.98 ms run.
3. Path 2 is no longer "cache NFC work from scratch"; `PATH-N1` already removes redundant normalization work for the common `fTextIsNfc == true` case.
4. The remaining native opportunity is still real because `PerformOffscreenLayout` now dominates an even larger share of cold render time.

## Validated Implementation Status

Implemented and validated on this branch:

- `LayoutPassCache` is owned by `ParaBuilder` and exposed through a thread-local pointer.
- Path 2 is implemented: `CallScriptItemize` analysis reuse, cached NFC text, cached non-NFC offset maps, and `DoAllRuns` reuse of cached analysis.
- Path 1 first slice is implemented: `ShapePlaceRun` result reuse keyed by NFC text content + font + `SCRIPT_ANALYSIS`, with deep-copied shaping buffers.
- Shape-cache hits are allowed for all `ShapePlaceRun` callers, while new cache stores remain restricted to `fCreatingSeg` calls.
- Layout-pass telemetry is implemented and emitted from `ParaBuilder` teardown: analysis/shape request counts, hit/miss counts, evictions, and miss compute time.
- Runtime kill switches are implemented for the validated paths:
  - `FW_PERF_P125_PATH1`
  - `FW_PERF_P125_PATH2`
- Render benchmark artifacts now record active P125 flag states and per-scenario cold/warm `PerformOffscreenLayout` totals.

Explicitly attempted and rejected:

- Broad reuse of cached `ScriptBreak` / `ScriptGetLogicalWidths` results from the shape cache was implemented experimentally and then fully reverted after it caused large pixel-output regressions across 12 render scenarios.

Still pending:

- Path 5 fast path is not implemented.
- A native runtime flag for Path 5 is not yet needed because there is no Path 5 code to gate.
- There are no new native unit tests yet for the new cache layers; current confidence comes from existing native tests plus render/timing/baseline validation.

## Design Goals and Constraints

Goals:

1. Reduce cold `PerformOffscreenLayout` by at least 20% in heavy scenarios.
2. Preserve exact line-break and pixel output behavior.
3. Keep warm-path gains intact (no regressions to `PATH-L1`, `PATH-L4`, or `PATH-R1`).

Constraints:

1. Text pipeline correctness (bidi, surrogates, trailing whitespace, fallback) is non-negotiable.
2. Optimizations must be safely disableable (feature flags or compile-time guard).
3. Cache invalidation must be conservative; stale cache is worse than a miss.
4. Existing `PATH-N1` NFC fast path should remain the common-case fast path for already-normalized text.

## Scope

In scope:

- `Src/views/lib/UniscribeEngine.cpp`
- `Src/views/lib/UniscribeSegment.cpp`
- `Src/views/lib/UniscribeSegment.h`
- `Src/views/lib/UniscribeEngine.h` (if new helper declarations are required)
- `Src/views/Test/*` for native unit coverage
- `Src/Common/RootSite/RootSiteTests/*` for render/timing assertions

Out of scope:

- DataTree architectural virtualization changes
- Graphite engine redesign
- DirectWrite migration

## Architectural Analysis: Why Full FindBreakPoint Memoization Was Discarded

Deep-dive analysis (traced `ParaBuilder::MainLoop` → `AddWsRun` → `GetSegment` → `FindBreakPoint`
and the `BackTrack` re-entry path) revealed that memoizing the complete `FindBreakPoint` result
has a near-zero hit rate:

- **Pattern A — Sequential segments (most common):** `AddWsRun` calls `GetSegment` with advancing
  `ichMinSeg` positions. Each call starts where the previous one ended. Non-overlapping calls mean
  no repeated `(ichMinSeg, dxMaxWidth)` tuple.
- **Pattern B — Backtracking:** `BackTrack()` re-calls `GetSegment` with the same `ichMinSeg` but
  different `ichLimBacktrack` and potentially different width. No exact-parameter match.
- **Pattern C — Bidi ws toggle:** `AddWsRun` may retry with `ktwshOnlyWs` after `ktwshNoWs` fails.
  Same text, different `twsh`.
- **Pattern D — Re-layout:** Same paragraph at different `dxAvailWidth` — all parameters differ.
  (Already handled by `PATH-L1` guard when width is unchanged.)

The expensive operations inside `FindBreakPoint` — `CallScriptItemize`, `ShapePlaceRun`,
`ScriptBreak`, `ScriptGetLogicalWidths` — depend only on text content + font/HDC + writing system.
They do not depend on `dxMaxWidth`, `ichLimBacktrack`, `lbPref`, `twsh`, or any layout-policy
parameters. The policy parameters only control how the main loop uses those results to pick a
break point.

This means the valuable cache target is the sub-result level (analysis products and shaped runs),
not the full `FindBreakPoint` output.

## Invalidation Contract

`IVwTextSource` has no version stamp or change counter (confirmed by inspecting `VwTxtSrc.h`).
This is intentional: the architecture guarantees text immutability during a layout pass.

- `PropChanged` → `VwNotifier::PropChanged` → regenerates boxes → `DoLayout`/`Relayout` is a
  sequential pipeline. Text source content cannot change while `ParaBuilder::MainLoop` is running.
- The `IVwTextSource *` pointer identity is stable throughout the entire `ParaBuilder` lifetime.
- `UniscribeEngine` instances are shared (cached per writing system + font + bold/italic in
  `RenderEngineFactory`), so cache must be scoped to the layout pass, not the engine.

**Invalidation strategy:** Clear all caches at layout-pass boundary. No version stamps needed.
Within a pass, `IVwTextSource *` pointer + character range is a sufficient identity key.

## Cache Ownership

Because `UniscribeEngine` is shared across paragraphs and layout passes, caches cannot live on
the engine instance. Two viable ownership models:

1. **Stack-scoped context object** passed through `FindBreakPoint` (requires signature change or
   thread-local) — cleanest lifecycle, automatic cleanup.
2. **Layout-pass context on `VwParagraphBox` or `ParaBuilder`** — passed via `IVwTextSource`
   extension or side-channel. Cleared when `ParaBuilder` is destroyed.

Preferred direction: thread-local or RAII-managed layout-pass context, because it keeps cache
lifetime explicit without changing the COM-facing `IRenderEngine::FindBreakPoint` signature.

Research refinement: `ParaBuilder` already owns the full layout-pass lifetime and has a
destructor in `VwTextBoxes.cpp`, so the cleanest implementation is a `ParaBuilder`-owned cache
with a thread-local scope helper only if needed to reach `FindBreakPoint` without a COM signature
change.

## Path 1: ShapePlaceRun Result Caching (Revised)

### Intent

Cache per-run glyph shaping results (`ScriptShape` + `ScriptPlace` outputs) within a layout pass,
so that when backtracking re-encounters a run that was already shaped, it reuses the
widths/glyphs without calling the expensive Win32 shaping APIs again.

### Why this replaces full FBP memoization

`ShapePlaceRun` is the single most expensive call inside the `FindBreakPoint` main loop
(each invocation calls `ScriptShape` + `ScriptPlace`, both of which hit the font rasterizer).
During backtracking (Pattern B), the loop re-shapes runs it already shaped in the previous
attempt. Caching the shaped results eliminates this redundant work.

### Cache key

```
ShapeRunKey = {
  const OLECHAR * prgch,   // NFC text slice (content, not pointer — memcmp)
  int cch,                 // length of the NFC text slice
  HFONT hfont,             // font handle (from VwGraphics font cache)
  SCRIPT_ANALYSIS sa       // Uniscribe script analysis (from ScriptItemize)
}
```

### Cache value

```
ShapeRunValue = {
  WORD * prgGlyphs,        // glyph indices
  int cglyph,              // glyph count
  int * prgAdvance,        // advance widths
  GOFFSET * prgGoffset,    // glyph offsets
  WORD * prgCluster,       // cluster mapping
  SCRIPT_VISATTR * prgsva, // visual attributes
  int dxdWidth,            // total run width
  int * prgcst,            // stretch types
  bool fScriptPlaceFailed  // whether ScriptPlace failed
}
```

### Implementation approach

1. Introduce a `ShapeRunCache` class with a small fixed-capacity map (initially about 32 entries).
   Key comparison is `memcmp` on the NFC text content + `HFONT` + `SCRIPT_ANALYSIS` bytes.
2. At the start of `FindBreakPoint`, accept or create the cache context.
3. Before `ShapePlaceRun(uri, true)`, probe the cache. On hit, copy cached values into `uri`.
   On miss, call `ShapePlaceRun` as before, then store the result.
4. Cache lifetime: created by `ParaBuilder` at layout-pass start, destroyed when `ParaBuilder`
   goes out of scope. Passed to `FindBreakPoint` via thread-local or added parameter.
5. Sub-task: deep-copy the owned `UniscribeRunInfo` buffers. Current code shows
   `UniscribeRunInfo` owns its glyph/advance/cluster/offset storage via `realloc`, so the cache
   should copy those owned buffers directly rather than trying to alias any transient `g_v*` state.

### Scenarios that benefit

- **Backtracking** (Pattern B): runs shaped in the first attempt are reused when the loop retries
  with `ichLimBacktrack` reduced.
- **Bidi ws toggle** (Pattern C): the same text is shaped twice with different `twsh` — the
  shaping result is identical.
- **Multi-line paragraphs**: runs near the end of line N that did not fit may be re-shaped at
  the start of line N+1 with the same text content.

### Expected savings

- `PerformOffscreenLayout`: about 5-12% reduction (concentrated in backtrack-heavy scenarios)
- Best gains in `long-prose`, `lex-extreme` (many lines, frequent backtracking)
- Modest gains in simple scenarios (little backtracking)

## Path 2: Text Analysis Cache (`ScriptItemize` + NFC buffers + non-NFC maps)

### Intent

Compute and reuse the expensive text-analysis products (`ScriptItemize`, NFC text buffers,
and non-NFC offset maps) once per paragraph text source, instead of recomputing them on every
`FindBreakPoint` call.

This is still the main analysis-layer optimization, but it is now narrower than the original
idea because `PATH-N1` already removes redundant normalization work when the source text is
already NFC. The main remaining target is repeated `CallScriptItemize` work plus the
non-NFC offset-map case.

### Call-chain analysis: where the redundancy is

During a single `ParaBuilder::MainLoop` pass for one paragraph:

1. `AddWsRun` calls `GetSegment` → `FindBreakPoint` once per line segment.
2. Each `FindBreakPoint` call re-runs `CallScriptItemize` on the same text range
   (`ichMinSeg..ichLimText`). For a 10-line paragraph, this means 10+ calls to
   `ScriptItemize` on overlapping or identical text windows.
3. `BackTrack` re-calls `FindBreakPoint` on the same text with different limits,
   triggering another full `CallScriptItemize`.
4. `UniscribeSegment::DoAllRuns` also calls `CallScriptItemize` again when the
   segment is drawn, so the same paragraph can be itemized once in `FindBreakPoint`
   and again in the segment path.

The text content does not change between any of these calls. The `IVwTextSource *` is the
same object with the same content throughout the entire `ParaBuilder` lifetime.

### Cache key

```
AnalysisKey = {
  IVwTextSource * pts,     // pointer identity — stable within layout pass
  int ichMinSeg,           // start of text region in source
  int ichLimText,          // end of text region in source
  int ws,                  // writing system (from chrp.ws)
  ComBool fParaRtoL        // paragraph direction (affects ScriptItemize)
}
```

Note: `HFONT` is not part of this key — `ScriptItemize` and normalization are font-independent.
Font only matters for shaping (Path 1).

Research refinement: match the key to the actual `CallScriptItemize` inputs in the current code.
`UniscribeSegment::CallScriptItemize(...)` derives bidi state from `GetCharProps(ichMin)`
(`chrp.fWsRtl`) and does not currently consume `fParaRtoL` when building `SCRIPT_STATE`.
That means the cache key should be based on text source identity/span plus the effective initial
bidi state used by `CallScriptItemize`, not on `HFONT` and not necessarily on paragraph RTL alone.

### Cache value

```
AnalysisValue = {
  OLECHAR * prgchNfc,       // NFC-normalized text buffer (owned copy)
  int cchNfc,               // NFC text length
  bool fTextIsNfc,          // whether source text was already NFC
  SCRIPT_ITEM * prgscri,    // ScriptItemize results (owned copy)
  int citem,                // script item count
  // Offset maps (only populated when fTextIsNfc == false):
  int * prgichOrigToNfc,    // original→NFC offset map
  int * prgichNfcToOrig,    // NFC→original offset map
}
```

### Implementation approach

1. **Introduce `TextAnalysisCache` class** with a small map (initially about 8-16 entries,
   keyed by `AnalysisKey`).
   - Owns allocated buffers; entries freed on cache destruction.
   - Supports `Lookup(key) → AnalysisValue*` and `Store(key, value)`.

2. **Refactor the `CallScriptItemize` call site in `FindBreakPoint`** (currently around
   `UniscribeEngine.cpp` line 414):
   - Before calling `CallScriptItemize`, probe the cache.
   - On hit: copy `prgchNfc`, `cchNfc`, `fTextIsNfc`, `prgscri`, `citem` from cache
     into local variables. Skip the `CallScriptItemize` call entirely.
   - On miss: call `CallScriptItemize` as today, then store results in cache.

3. **Refactor offset conversion calls** (`OffsetInNfc`, `OffsetToOrig`):
   - When the analysis cache entry exists with `fTextIsNfc == true`, these are identity
     (already optimized by `PATH-N1`). No extra conversion cache is needed.
   - When `fTextIsNfc == false`, use the cached offset maps instead of re-normalizing.
     Currently `OffsetInNfc` calls `StrUtil::NormalizeStrUni` on each call.

4. **Wire `DoAllRuns` to consume cached analysis** when the same text source
   is still active. `DoAllRuns` in `UniscribeSegment.cpp` still calls
   `CallScriptItemize`; if the cache is available via thread-local or parameter,
   it can skip re-itemization there as well.

5. **Cache lifetime:** Same as Path 1 — created by `ParaBuilder`, destroyed when
   `ParaBuilder` goes out of scope. Same context object can hold both caches.

### Concrete code changes

| File | Change |
|------|--------|
| New: `Src/views/lib/LayoutCache.h` | `TextAnalysisCache` and `ShapeRunCache` class definitions, key/value structs |
| `Src/views/lib/UniscribeEngine.cpp` | `FindBreakPoint`: probe analysis cache before `CallScriptItemize`, store on miss |
| `Src/views/lib/UniscribeSegment.cpp` | `CallScriptItemize`: extract into cacheable form; `DoAllRuns`: probe analysis cache before itemizing |
| `Src/views/lib/UniscribeSegment.h` | Updated `CallScriptItemize` signature or overload accepting cache |
| `Src/views/VwTextBoxes.cpp` | `ParaBuilder`: create/own `LayoutPassCache` context and expose it to layout code |

### Expected savings

- `PerformOffscreenLayout`: about 8-15% reduction if repeated itemization remains a top residual cost after `PATH-N1`
- Total cold average: about 4-7% reduction from the current 49.22 ms baseline
- Strongest in multi-line paragraphs, redraws that revisit the same paragraph text, and render paths that hit both `FindBreakPoint` and `DoAllRuns`
- Savings from NFC normalization itself should be treated as mostly already captured by `PATH-N1`

## Path 5: Common-Case Fast Path in FindBreakPoint

### Intent

Short-circuit common LTR/no-special handling path with fewer branches and less fallback machinery.

### Main implementation changes

1. Add explicit fast-path gate for requests meeting all conditions:
   - LTR simple direction depth,
   - no complex trailing-whitespace mode,
   - no hard breaks in candidate window,
   - no script/layout failure flags.
2. In fast path:
   - use precomputed analysis from Path 2,
   - perform streamlined width accumulation and break lookup,
   - avoid generic backtracking scaffolding unless needed.
3. Immediate fallback to existing generic logic when any guard fails.

### Expected savings

- `PerformOffscreenLayout`: about 4-10% reduction if loop/control-flow overhead remains visible after Paths 1 and 2
- Total cold average: likely secondary to Paths 1 and 2; only justify if re-profiling shows the generic loop itself still dominates

## Decisions Required Before Further Coding

## Research-Backed Recommendation

Code review plus Microsoft Uniscribe guidance suggest the following execution order and guardrails:

1. **Implement `LayoutPassCache` at `ParaBuilder` scope first.** Uniscribe `SCRIPT_CACHE` is
  already per-font/style and should stay that way; the new cache should capture higher-level
  analysis/shaping results for one layout pass only, not live on the shared `UniscribeEngine`.
2. **Do Path 2 before Path 1.** The current branch duplicates `CallScriptItemize(...)` in both
  `UniscribeEngine::FindBreakPoint(...)` and `UniscribeSegment::DoAllRuns(...)`, and repeats
  `OffsetInNfc`/`OffsetToOrig` work on non-NFC text. That duplication happens on every segment,
  not just on backtracking-heavy paragraphs.
3. **Do Path 1 second, after Path 2 telemetry is in place.** `ShapePlaceRun(...)` reuse is still
  valuable, but its biggest wins are tied to backtracking and rerun scenarios. It should be keyed
  by NFC text content + font/style identity + `SCRIPT_ANALYSIS`, with deep-copied buffers.
4. **Defer Path 5 until re-profiling after Paths 1/2.** Microsoft guidance for `ScriptBreak`
  explicitly assumes whole-item processing, so a manual fast path must preserve item boundaries and
  fall back immediately on any mixed-script, bidi, whitespace-mode, or fallback-font complexity.
5. **Add telemetry before tuning capacities.** Reuse the existing render-trace infrastructure or
  equivalent counters to record cache hits, misses, evictions, `CallScriptItemize` timings,
  `ShapePlaceRun` timings, and backtrack frequency before locking in capacities or defaults.

1. **Cache threading model**
  - Preferred choice: `ParaBuilder` owns the cache object; a narrow thread-local scope helper can
    expose it to `FindBreakPoint` without changing the COM-facing `IRenderEngine::FindBreakPoint`
    signature.
  - Reason: matches the real layout-pass lifetime and avoids putting mutable state on the shared
    render engine.
  - Must still verify no cross-thread reuse of the same layout context is possible.

2. **Initial cache capacities**
   - The draft `~16` analysis entries and `~32` shaped-run entries are placeholders, not validated values.
   - First implementation should instrument hit/miss/eviction counts and tune after benchmark runs.

3. **Feature-flag rollout**
   - The three paths should remain independently disableable.
   - Default enablement should be decided after correctness coverage and first benchmark pass, not assumed up front.

4. **Path ordering after current profiling**
   - `PATH-N1` changed the cost distribution inside the Uniscribe path.
   - Re-profile the current branch before committing to Path 2 as the first substantive optimization after infrastructure work.

Current status of these decisions:

- Cache ownership/lifetime: settled in favor of `ParaBuilder` + thread-local exposure.
- Initial capacities: implemented as `16` analysis entries and `32` shaped-run entries, with telemetry in place for later tuning.
- Feature-flag rollout: implemented paths default on and are independently disableable via environment variables.
- Path ordering: resolved in practice as infrastructure → Path 2 → Path 1 slice → re-profile → Path 5 last.

## Execution Order

Based on the current branch state, the revised execution order is:

1. **Refresh the micro-profile against the current branch** — confirm the residual split between
   `CallScriptItemize`, non-NFC offset work, and `ShapePlaceRun` after `PATH-N1`.
2. **P125-1 infrastructure first** — `LayoutPassCache` lifetime and cache plumbing are required
  regardless of whether Path 1 or Path 2 lands first.
3. **Default to Path 2 first unless the refreshed profile clearly disproves it**:
  - repeated itemization and non-NFC offset conversion are currently duplicated in both
    `FindBreakPoint` and `DoAllRuns`;
  - Path 1 should move ahead only if the refreshed profile shows backtracking reshaping dominating
    more than expected.
4. **Re-profile after Paths 1 and/or 2** to measure actual gains and identify remaining hot spots.
5. **Path 5 last** — only if re-profiling still shows meaningful generic loop overhead after the
   expensive API calls are being reused.

Current state:

- Steps 1-4 are complete enough for the current slice.
- The next substantive implementation target is Path 5, guided by the refreshed benchmark/reporting data.

## Combined Savings Model (Paths 1+2+5)

Combined reductions are not additive. Against the current baseline, a realistic expectation is:

- `PerformOffscreenLayout`: 15-25% reduction on heavy scenarios
- Total cold average: about 3.4-6.8 ms saved on the current 49.22 ms average cold baseline

Using current average cold baseline (49.22 ms):

- projected cold range: about 42.4-45.8 ms
- projected average savings: about 3.4-6.8 ms
- heavy-case savings (`long-prose`/`mixed-styles`/`lex-extreme`) can be materially larger.

Note: Previous estimates were optimistic because they assumed full `FindBreakPoint` memoization hits.
The revised model reflects sub-result caching, which eliminates API call overhead but
still requires the loop logic to run. It also assumes `PATH-L1`, `PATH-L4`, `PATH-R1`, and `PATH-N1`
remain intact.

## Implementation Task List

### P125-0: Refresh evidence and settle design choices

- [x] Reconfirm the current `PerformOffscreenLayout` sub-cost split on this branch before choosing Path 1 vs. Path 2 ordering.
- [x] Decide whether the cache context is thread-local RAII or an explicit parameter path.
- [x] Decide initial cache capacities and required hit/miss/eviction telemetry.
- [x] Decide initial feature-flag defaults and rollout strategy.

### P125-1: LayoutPassCache infrastructure

- [x] Create `Src/views/lib/LayoutCache.h` with:
  - `AnalysisKey` and `AnalysisValue` structs.
  - `ShapeRunKey` and `ShapeRunValue` structs.
  - `TextAnalysisCache` class (fixed-capacity map, initial target about 16 entries, key comparison via `memcmp`).
  - `ShapeRunCache` class (fixed-capacity map, initial target about 32 entries).
  - `LayoutPassCache` wrapper owning both caches.
- [x] Add `LayoutPassCache` creation in `ParaBuilder::Initialize` (`VwTextBoxes.cpp`).
- [x] Thread the cache to `GetSegment` → `FindBreakPoint` via either:
  - (a) thread-local pointer set/cleared by `ParaBuilder`, or
  - (b) added parameter to `IRenderEngine::FindBreakPoint` (COM signature change — heavier).
  - Current recommendation: prefer (a) to avoid COM interface change.
- [x] Ensure cache is destroyed when `ParaBuilder` goes out of scope (RAII or destructor).

### P125-2: Path 2 — Text analysis cache

- [x] Refactor `CallScriptItemize` call in `FindBreakPoint` (currently around line 414 of `UniscribeEngine.cpp`):
  - Build `AnalysisKey` from `{pts, ichMinSeg, ichLimText, chrpThis.ws, fParaRtoL}`.
  - Probe `TextAnalysisCache`. On hit, use cached `prgchNfc`, `cchNfc`, `fTextIsNfc`,
    `prgscri`, `citem`. Skip `CallScriptItemize`.
  - On miss, call `CallScriptItemize` as today, then store results.
- [x] Refactor `OffsetInNfc`/`OffsetToOrig` calls in the main loop to use cached offset maps
  when `fTextIsNfc == false` (when `true`, these are already identity via `PATH-N1`).
- [x] Add overload or optional parameter to `DoAllRuns` in `UniscribeSegment.cpp` to accept
  cached analysis and skip its own `CallScriptItemize` call.
- [x] Add trace counter: analysis cache hits vs. misses per layout pass.

### P125-3: Path 1 — ShapePlaceRun cache

- [x] Before `ShapePlaceRun(uri, true)` in `FindBreakPoint` main loop (currently around line 547):
  - Build `ShapeRunKey` from `{uri.prgch, uri.cch, hfont (from VwGraphics), uri.psa}`.
  - Probe `ShapeRunCache`. On hit, copy cached glyph/advance/cluster/width data into `uri`.
  - On miss, call `ShapePlaceRun` as today, then deep-copy results into cache.
- [x] Deep-copy `UniscribeRunInfo` owned glyph/advance/cluster/offset buffers into the cache;
  do not rely on transient vector state.
- [x] Add trace counter: shaping cache hits vs. misses per layout pass.
- [x] Validate that `BackTrack()` re-entry into `FindBreakPoint` now hits the shaping cache
  for previously-shaped runs.

Implementation note: the validated slice caches shaping outputs only. A broader attempt to cache logical widths and `ScriptBreak` results was reverted after render regressions.

### P125-4: Path 5 fast path

- [ ] Add fast-path gate function with explicit guard conditions.
- [ ] Implement streamlined break computation using analysis cache.
- [ ] Guarantee exact fallback semantics to generic path.

### P125-5: Feature flag and diagnostics

- [x] Add runtime flags:
  - `FW_PERF_P125_PATH1`
  - `FW_PERF_P125_PATH2`
- [ ] Add runtime flag:
  - `FW_PERF_P125_PATH5`
- [x] Emit counters for analysis/shape hit/miss/evict/compute-time telemetry into trace logs.
- [ ] Emit Path 5 fallback-reason counters once Path 5 exists.

### P125-6: Verification and benchmark reporting

- [x] Run targeted native tests for line-break correctness.
- [x] Run render timing suite and compare against baseline JSON / matched control runs.
- [x] Record before/after in `Output/RenderBenchmarks` and summarize deltas by scenario against
  run `1b193fb08a1b477f9419b59b0ec8d78f` unless a newer validated baseline replaces it.

Latest validation snapshot:

- `msbuild Src\views\Test\TestViews.vcxproj /p:Configuration=Debug /p:Platform=x64 /p:LinkIncremental=false /v:minimal /nologo` passed.
- `.\test.ps1 -Native -TestProject TestViews -NoBuild` passed.
- `msbuild Src\Common\RootSite\RootSiteTests\RootSiteTests.csproj /p:Configuration=Debug /p:Platform=x64 /v:minimal /nologo` passed.
- `.\Build\Agent\Run-AllRenders.ps1 -Scope All -NoBuild -Verbosity minimal` passed.
- `.\test.ps1 -NoBuild -TestProject "Src/Common/RootSite/RootSiteTests" -TestFilter "FullyQualifiedName~RenderTimingSuiteTests"` passed with Path 1/2 enabled and disabled.

## Test Plan

## A. Native Unit Tests (Correctness)

Primary location: `Src/views/Test/RenderEngineTestBase.h` and related native render tests.

### A1. Path 1 shaping cache correctness tests

- [ ] Shaped run with cache enabled produces identical glyphs/widths as without cache.
- [ ] Different `HFONT` for same text produces different cache entry (no false hit).
- [ ] Different `SCRIPT_ANALYSIS` for same text produces different cache entry.
- [ ] Backtracking scenario: re-shaped runs after `BackTrack()` match original shaping.
- [ ] Cache entry ownership: cached copies remain valid after the source `UniscribeRunInfo`
  instance and any transient vectors have been reused or destroyed.

### A2. Path 2 analysis cache correctness tests

- [ ] Surrogate pair boundaries never split across cached NFC offset maps.
- [ ] Combining marks where NFC length differs from source preserve correct offset mapping.
- [ ] Hard-break characters (`\n`, `\r`, tab, object replacement char) preserve break semantics.
- [ ] RTL input with mirrored punctuation yields identical `SCRIPT_ITEM` array with cache on/off.
- [ ] Same text source with different `ws` gets separate cache entries.
- [ ] Same text source with different `ichMinSeg` gets separate cache entries.
- [ ] Cache hit returns identical `cchNfc`, `fTextIsNfc`, `citem` as fresh computation.

### A3. Path 5 fast-path equivalence tests

- [ ] LTR simple run enters fast path and equals generic output.
- [ ] Whitespace handling modes (`ktwshNoWs`, `ktwshOnlyWs`, others) force fallback when required.
- [ ] ScriptPlace failure or special shaping conditions force fallback and preserve output.
- [ ] Mixed writing systems in one candidate span trigger fallback safely.

### A4. Stress and stability tests

- [ ] Thousands of repeated break calls with cache enabled do not leak memory.
- [ ] LRU or fixed-cap eviction behaves deterministically under pressure.
- [ ] Null or empty segment cases remain unchanged.

## B. Render Tests (Pixel + Behavior)

Primary location: `Src/Common/RootSite/RootSiteTests/RenderTimingSuiteTests.cs` and baseline tests.

- [x] Existing pixel-perfect scenarios still pass unchanged.
- [ ] Add scenario variants emphasizing:
  - deep paragraphs with many break opportunities,
  - mixed styles in one paragraph,
  - multi-writing-system lines,
  - RTL-heavy text blocks,
  - combining-mark intensive text.
- [x] Validate no pixel variance compared to baseline snapshots.

## C. Timing and Regression Tests

- [ ] Add per-stage regression assertion for `PerformOffscreenLayout` (improves or no worse within tolerance).
- [x] Add per-scenario delta reporting for heavy scenarios:
  - `long-prose`, `mixed-styles`, `rtl-script`, `lex-extreme`.
- [ ] Add hit-rate telemetry assertions (non-zero hit rate in scenarios designed for reuse).

## D. Flag Matrix Tests

Run matrix:

1. all paths off (control)
2. Path 2 only
3. Path 1 only
4. Path 5 only
5. Path 1+2
6. Path 1+2+5

For each matrix entry:

- [ ] native correctness tests pass,
- [x] render snapshots pass for the `all paths off` and `Path 1+2` runs performed so far,
- [x] timing suite emits valid artifacts for the `all paths off` and `Path 1+2` runs performed so far,
- [ ] no crash or regression on warm path.

## Edge Cases Checklist (Must Explicitly Pass)

- [ ] surrogate pair at break boundary
- [ ] combining sequence contraction or expansion under NFC
- [ ] trailing whitespace segment with backtracking
- [ ] upstream or downstream directional runs in same paragraph
- [ ] hard line break and object replacement character handling
- [ ] mixed ws renderer switching boundaries
- [ ] empty-segment fallback and zero-width fit

## Acceptance Criteria

1. Correctness:
   - zero pixel regressions in existing render baseline suite,
   - all new native edge-case tests green.
2. Performance:
   - `PerformOffscreenLayout` improvement in at least 3 of 4 heavy scenarios,
   - average `PerformOffscreenLayout` reduction >= 20% in heavy-scenario subset.
3. Safety:
   - feature flags allow disabling each path independently,
   - no memory growth trend in stress loop.

## Managed-Layer Scroll Rendering Fix (DataTree Separator Lines)

### Problem

DataTree is a scrollable `UserControl` that hosts child `Slice` controls (each a separate HWND).
Thin horizontal separator lines are painted in the parent background between slices. During
scrolling, Windows bitblts the entire client area (including parent-painted separator pixels)
to the new scroll position and only repaints the newly-exposed strip. This caused separator
lines to accumulate at wrong positions — visible as persistent gray line artifacts during
fast scrolling.

The original code used `ScrollWindowEx` (P/Invoke) to manually bitblt separator content during
scroll. This was both unnecessary (WinForms handles scrolling natively) and the root cause of
additional jitter because manually blitting parent-painted content races with the framework's
own scroll handling.

### Approaches Tried

1. **ScrollWindowEx removal**: Removed all `ScrollWindowEx` interop (`DllImport`, `RECT` struct,
   `SW_INVALIDATE` constant, `m_inScrollWindowEx` flag, `ScrollParentPaintedContent()` method).
   This eliminated the manual bitblt jitter but did not solve the stale line artifacts from
   Windows' own scroll bitblt.

2. **SeparatorOverlayControl**: A dedicated transparent child control drawn on top of all slices
   to paint separator lines independently. Failed: (a) caused `NullReferenceException` because
   `Controls.Add` triggered layout before `Slices` was initialized; (b) after fixing the crash,
   the opaque sibling covered slice content — WinForms HWND-per-control architecture made this
   fundamentally unworkable without `WS_CLIPSIBLINGS` management. Fully reverted.

3. **OnScroll override**: Added `Invalidate(false)` in `OnScroll`. Partially worked but
   `OnScroll` does not fire for mouse wheel input.

4. **WndProc scroll message interception (final solution)**: Override `WndProc` to catch
   `WM_VSCROLL`, `WM_HSCROLL`, `WM_MOUSEWHEEL`, and `WM_MOUSEHWHEEL`. After `base.WndProc`,
   call `Invalidate(false); Update();`. This works because:
   - `Invalidate(false)` invalidates only the parent background (separator gap areas), not
     child HWNDs
   - `Update()` forces synchronous `WM_PAINT` processing before the next scroll message can
     bitblt stale pixels
   - Covers all scroll input vectors (scrollbar drag, mouse wheel, horizontal wheel)

### Additional Managed Optimizations

- **Double buffering**: `SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true)`
  eliminates flicker from the synchronous repaint.

- **Deferred slice prewarm**: After scroll, queues idle-time work to pre-materialize lazy slices
  in the scroll direction ahead of the viewport. Uses `IdleQueue` integration when available,
  with configurable chunk size and time budget to avoid jank.

- **VwDrawRootBuffered cached frame reuse**: When `PrepareToDrawRoot` returns `kxpdrInvalidate`
  (content not yet ready), the previous successfully-drawn frame is re-blitted instead of
  showing a blank or partially-drawn frame. Implemented in both managed (`VwDrawRootBuffered.cs`)
	and native (`VwRootBox.cpp`) paths so the fallback is always available during invalidate-only passes.

### Key Insight

WinForms hosts each child control in a separate native HWND. Unlike a browser (single composited
texture), parent-painted content between child HWNDs is just pixels in the parent DC — Windows
has no knowledge that those pixels represent separator lines. When scrolling bitblts the client
area, those pixels move with everything else. The fix is to repaint them synchronously after
every scroll event, which has negligible cost because `Invalidate(false)` skips child
invalidation and separator line drawing is a handful of `DrawLine` calls.

## Deliverables

1. Code changes for paths 1, 2, and 5.
2. New or updated native tests for edge-case correctness.
3. New or updated render and timing tests plus scenario coverage.
4. Before/after benchmark artifacts and summary note linked from this change.
5. DataTree scroll rendering fix with separator line artifact elimination.
