# PerformOffscreenLayout Optimization Plan (Paths 1+2+5)

## Purpose

Define the implementation plan for three native text-layout optimization paths targeting `PerformOffscreenLayout`:

- Path 1: ShapePlaceRun result caching (revised from FindBreakPoint memoization)
- Path 2: Text analysis cache (NFC + ScriptItemize results)
- Path 5: Common-case fast path in line breaking

This plan includes:

- concrete code changes,
- phased task list,
- expected savings grounded in the latest render run,
- edge-case test coverage (unit + render/timing).

## Baseline Evidence (Current Run)

Source artifacts:

- `Output/RenderBenchmarks/summary.md`
- `Output/RenderBenchmarks/results.json`

Key numbers from run `5767ea6c5bfd4b7baa025bd683817816`:

- Avg cold render: 64.98 ms
- Avg `PerformOffscreenLayout`: 26.82 ms (67.9% of cold)

Heavy scenarios where `PerformOffscreenLayout` dominates cold time:

- `long-prose`: 156.42/160.52 ms (97.4%)
- `mixed-styles`: 141.68/146.69 ms (96.6%)
- `rtl-script`: 70.39/74.08 ms (95.0%)
- `lex-extreme`: 81.43/86.18 ms (94.5%)

## Design Goals and Constraints

Goals:

1. Reduce cold `PerformOffscreenLayout` by at least 20% in heavy scenarios.
2. Preserve exact line-break and pixel output behavior.
3. Keep warm-path gains intact (no regressions to existing `VwRootBox` guards).

Constraints:

1. Text pipeline correctness (bidi, surrogates, trailing whitespace, fallback) is non-negotiable.
2. Optimizations must be safely disableable (feature flags or compile-time guard).
3. Cache invalidation must be conservative; stale cache is worse than a miss.

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
  (Already handled by PATH-L1 guard when width is unchanged.)

The **expensive operations** inside `FindBreakPoint` — `CallScriptItemize`, `ShapePlaceRun`,
`ScriptBreak`, `ScriptGetLogicalWidths` — depend only on **text content + font/HDC + writing system**.
They do NOT depend on `dxMaxWidth`, `ichLimBacktrack`, `lbPref`, `twsh`, or any layout-policy
parameters. The policy parameters only control how the main loop uses those results to pick a
break point.

This means the valuable cache target is the **sub-result level** (analysis products and shaped runs),
not the full FindBreakPoint output.

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
2. **Layout-pass context on `VwParagraphBox`** or `ParaBuilder` — passed via `IVwTextSource`
   extension or side-channel. Cleared when `ParaBuilder` is destroyed.

Preferred: Option 1 (thread-local or added parameter), since it keeps cache lifetime explicit and
avoids any cross-paragraph pollution.

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

1. Introduce a `ShapeRunCache` class with a small fixed-capacity map (e.g. 32 entries).
   Key comparison is `memcmp` on the NFC text content + HFONT + SCRIPT_ANALYSIS bytes.
2. At the start of `FindBreakPoint`, accept or create the cache context.
3. Before `ShapePlaceRun(uri, true)`, probe the cache. On hit, copy cached values into `uri`.
   On miss, call `ShapePlaceRun` as before, then store the result.
4. Cache lifetime: created by `ParaBuilder` at layout-pass start, destroyed when `ParaBuilder`
   goes out of scope. Passed to `FindBreakPoint` via thread-local or added parameter.
5. Sub-task: Verify that `UniscribeRunInfo` (`uri`) fields are fully copyable without dangling
   pointers — the glyph/advance/cluster arrays point into `UniscribeSegment::g_v*` static
   vectors, so cached values must own their own copies.

### Scenarios that benefit

- **Backtracking** (Pattern B): runs shaped in the first attempt are reused when the loop retries
  with `ichLimBacktrack` reduced.
- **Bidi ws toggle** (Pattern C): the same text is shaped twice with different `twsh` — the
  shaping result is identical.
- **Multi-line paragraphs**: runs near the end of line N that didn't fit may be re-shaped at
  the start of line N+1 with the same text content.

### Expected savings

- `PerformOffscreenLayout`: 5-15% reduction (concentrated in backtrack-heavy scenarios)
- Best gains in `long-prose`, `lex-extreme` (many lines, frequent backtracking)
- Modest gains in simple scenarios (little backtracking)

## Path 2: Text Analysis Cache (NFC + ScriptItemize)

### Intent

Compute and reuse the expensive text-analysis products (NFC normalization, `ScriptItemize`,
offset maps) once per paragraph text source, instead of recomputing them on every
`FindBreakPoint` call.

This is the **foundational optimization** — Path 1 (shaping cache) and Path 5 (fast path)
both depend on having pre-computed analysis available.

### Call-chain analysis: where the redundancy is

During a single `ParaBuilder::MainLoop` pass for one paragraph:

1. `AddWsRun` calls `GetSegment` → `FindBreakPoint` once per line segment.
2. Each `FindBreakPoint` call re-runs `CallScriptItemize` on the same text range
   (`ichMinSeg..ichLimText`). For a 10-line paragraph, this means 10+ calls to
   `ScriptItemize` + NFC normalization on overlapping/identical text.
3. `BackTrack` re-calls `FindBreakPoint` on the same text with different limits,
   triggering another full `CallScriptItemize`.
4. `DoAllRuns` (segment rendering) also calls `CallScriptItemize` again when the
   segment is drawn.

The text content doesn't change between any of these calls. The `IVwTextSource *` is the
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

Note: `HFONT` is NOT part of this key — ScriptItemize and NFC normalization are
font-independent. Font only matters for shaping (Path 1).

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

1. **Introduce `TextAnalysisCache` class** with a small map (capacity ~8-16 entries,
   keyed by `AnalysisKey`).
   - Owns allocated buffers; entries freed on cache destruction.
   - Supports `Lookup(key) → AnalysisValue*` and `Store(key, value)`.

2. **Refactor `CallScriptItemize` call site in `FindBreakPoint`** (currently at
   `UniscribeEngine.cpp` ~line 420):
   - Before calling `CallScriptItemize`, probe the cache.
   - On hit: copy `prgchNfc`, `cchNfc`, `fTextIsNfc`, `prgscri`, `citem` from cache
     into local variables. Skip the `CallScriptItemize` call entirely.
   - On miss: call `CallScriptItemize` as today, then store results in cache.

3. **Refactor offset conversion calls** (`OffsetInNfc`, `OffsetToOrig`):
   - When the analysis cache entry exists with `fTextIsNfc == true`, these are identity
     (already optimized by PATH-N1). No change needed.
   - When `fTextIsNfc == false`, use the cached offset maps instead of re-normalizing.
     Currently `OffsetInNfc` calls `StrUtil::NormalizeStrUni` on each call.

4. **Wire `DoAllRuns` to consume cached analysis** when the same text source
   is still active. `DoAllRuns` in `UniscribeSegment.cpp` also calls
   `CallScriptItemize` — if the cache is available via thread-local or parameter,
   it can skip re-itemization.

5. **Cache lifetime:** Same as Path 1 — created by `ParaBuilder`, destroyed when
   `ParaBuilder` goes out of scope. Same context object can hold both caches.

### Concrete code changes

| File | Change |
|------|--------|
| New: `Src/views/lib/LayoutCache.h` | `TextAnalysisCache` and `ShapeRunCache` class definitions, key/value structs |
| `Src/views/lib/UniscribeEngine.cpp` | `FindBreakPoint`: probe analysis cache before `CallScriptItemize`, store on miss |
| `Src/views/lib/UniscribeSegment.cpp` | `CallScriptItemize`: extract into cacheable form; `DoAllRuns`: accept optional cache |
| `Src/views/lib/UniscribeSegment.h` | Updated `CallScriptItemize` signature or overload accepting cache |
| `Src/views/VwTextBoxes.cpp` | `ParaBuilder`: create/own `LayoutPassCache` context, pass to `GetSegment` |

### Expected savings

- `PerformOffscreenLayout`: 10-20% reduction across the board
- Total cold average: about 7-14% reduction
- Strongest in multi-line paragraphs and multi-ws/RTL-heavy scenarios
- Also eliminates redundant NFC normalization (stacks with PATH-N1)

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

- `PerformOffscreenLayout`: 8-15% reduction
- Total cold average: about 5-10% reduction

## Execution Order

Based on the deep-dive analysis, the revised execution order is:

1. **Path 2 first** — Analysis cache. Lowest risk, broadest impact, required infrastructure
   for both Path 1 and Path 5. Provides the `LayoutPassCache` context that the other paths
   plug into.
2. **Path 1 second** — ShapePlaceRun cache. Layers on top of Path 2's context object.
   Benefits backtracking scenarios specifically.
3. **Re-profile** after Path 2 + Path 1 to measure actual gains and identify remaining hot spots.
4. **Path 5 last** — Only if re-profiling shows the generic loop overhead is still significant
   after caching eliminates the expensive API calls.

## Combined Savings Model (Paths 1+2+5)

Combined reductions are not additive. Revised expectation envelope:

- `PerformOffscreenLayout`: 15-30% reduction on heavy scenarios (revised down from 25-45%)
- Total cold average: 10-20% reduction

Using current average cold baseline (64.98 ms):

- projected cold range: about 52.0-58.5 ms
- projected per-click savings: about 6.5-13 ms average
- heavy-case savings (`long-prose`/`mixed-styles`/`lex-extreme`) can be materially larger.

Note: Previous estimates were optimistic because they assumed full FBP memoization hits.
The revised model reflects sub-result caching, which eliminates API call overhead but
still requires the loop logic to run.

## Implementation Task List

### P125-1: LayoutPassCache infrastructure

- [ ] Create `Src/views/lib/LayoutCache.h` with:
  - `AnalysisKey` and `AnalysisValue` structs.
  - `ShapeRunKey` and `ShapeRunValue` structs.
  - `TextAnalysisCache` class (fixed-capacity map, ~16 entries, key comparison via `memcmp`).
  - `ShapeRunCache` class (fixed-capacity map, ~32 entries).
  - `LayoutPassCache` wrapper owning both caches.
- [ ] Add `LayoutPassCache` creation in `ParaBuilder::Initialize` (`VwTextBoxes.cpp`).
- [ ] Thread the cache to `GetSegment` → `FindBreakPoint` via either:
  - (a) Thread-local pointer set/cleared by `ParaBuilder`, or
  - (b) Added parameter to `IRenderEngine::FindBreakPoint` (COM signature change — heavier).
  - Decision: prefer (a) to avoid COM interface change.
- [ ] Ensure cache is destroyed when `ParaBuilder` goes out of scope (RAII or destructor).

### P125-2: Path 2 — Text analysis cache

- [ ] Refactor `CallScriptItemize` call in `FindBreakPoint` (~line 420 of `UniscribeEngine.cpp`):
  - Build `AnalysisKey` from `{pts, ichMinSeg, ichLimText, chrpThis.ws, fParaRtoL}`.
  - Probe `TextAnalysisCache`. On hit, use cached `prgchNfc`, `cchNfc`, `fTextIsNfc`,
    `prgscri`, `citem`. Skip `CallScriptItemize`.
  - On miss, call `CallScriptItemize` as today, then store results.
- [ ] Refactor `OffsetInNfc`/`OffsetToOrig` calls in the main loop to use cached offset maps
  when `fTextIsNfc == false` (when `true`, these are already identity via PATH-N1).
- [ ] Add overload or optional parameter to `DoAllRuns` in `UniscribeSegment.cpp` to accept
  cached analysis and skip its own `CallScriptItemize` call.
- [ ] Add trace counter: analysis cache hits vs. misses per `FindBreakPoint` call.

### P125-3: Path 1 — ShapePlaceRun cache

- [ ] Before `ShapePlaceRun(uri, true)` in `FindBreakPoint` main loop (~line 555):
  - Build `ShapeRunKey` from `{uri.prgch, uri.cch, hfont (from VwGraphics), uri.psa}`.
  - Probe `ShapeRunCache`. On hit, copy cached glyph/advance/cluster/width data into `uri`.
  - On miss, call `ShapePlaceRun` as today, then deep-copy results into cache.
- [ ] Verify `UniscribeRunInfo` (`uri`) field ownership: the glyph/advance/cluster arrays
  point into `UniscribeSegment::g_v*` static vectors. Cached values must own independent
  copies to avoid aliasing bugs.
- [ ] Add trace counter: shaping cache hits vs. misses per run.
- [ ] Validate that `BackTrack()` re-entry into `FindBreakPoint` now hits the shaping cache
  for previously-shaped runs.

### P125-4: Path 5 fast path

- [ ] Add fast-path gate function with explicit guard conditions.
- [ ] Implement streamlined break computation using analysis cache.
- [ ] Guarantee exact fallback semantics to generic path.

### P125-5: Feature flag and diagnostics

- [ ] Add runtime flags:
  - `FW_PERF_P125_PATH1`
  - `FW_PERF_P125_PATH2`
  - `FW_PERF_P125_PATH5`
- [ ] Emit counters for hit/miss/fallback reasons into trace logs.

### P125-6: Verification and benchmark reporting

- [ ] Run targeted native tests for line-break correctness.
- [ ] Run render timing suite and compare against baseline JSON.
- [ ] Record before/after in `Output/RenderBenchmarks` and summarize deltas by scenario.

## Test Plan

## A. Native Unit Tests (Correctness)

Primary location: `Src/views/Test/RenderEngineTestBase.h` and related native render tests.

### A1. Path 1 shaping cache correctness tests

- [ ] Shaped run with cache enabled produces identical glyphs/widths as without cache.
- [ ] Different HFONT for same text produces different cache entry (no false hit).
- [ ] Different SCRIPT_ANALYSIS for same text produces different cache entry.
- [ ] Backtracking scenario: re-shaped runs after `BackTrack()` match original shaping.
- [ ] Cache entry ownership: modifying `UniscribeSegment::g_v*` static vectors after cache
  store does not corrupt cached values (deep-copy verification).

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
- [ ] ScriptPlace failure/special shaping conditions force fallback and preserve output.
- [ ] Mixed writing systems in one candidate span trigger fallback safely.

### A4. Stress and stability tests

- [ ] Thousands of repeated break calls with cache enabled do not leak memory.
- [ ] LRU/cap eviction behaves deterministically under pressure.
- [ ] Null/empty segment cases remain unchanged.

## B. Render Tests (Pixel + Behavior)

Primary location: `Src/Common/RootSite/RootSiteTests/RenderTimingSuiteTests.cs` and baseline tests.

- [ ] Existing pixel-perfect scenarios still pass unchanged.
- [ ] Add scenario variants emphasizing:
  - deep paragraphs with many break opportunities,
  - mixed styles in one paragraph,
  - multi-writing-system lines,
  - RTL-heavy text blocks,
  - combining-mark intensive text.
- [ ] Validate no pixel variance compared to baseline snapshots.

## C. Timing and Regression Tests

- [ ] Add per-stage regression assertion for `PerformOffscreenLayout` (improves or no worse within tolerance).
- [ ] Add per-scenario delta reporting for heavy scenarios:
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
- [ ] render snapshots pass,
- [ ] timing suite emits valid artifacts,
- [ ] no crash/regression on warm path.

## Edge Cases Checklist (Must Explicitly Pass)

- [ ] surrogate pair at break boundary
- [ ] combining sequence contraction/expansion under NFC
- [ ] trailing whitespace segment with backtracking
- [ ] upstream/downstream directional runs in same paragraph
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

## Deliverables

1. Code changes for paths 1, 2, and 5.
2. New/updated native tests for edge-case correctness.
3. New/updated render/timing tests and scenario coverage.
4. Before/after benchmark artifacts and summary note linked from this change.
