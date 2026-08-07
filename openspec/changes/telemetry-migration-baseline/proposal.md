## Why

A future Avalonia UI rewrite (in progress on a separate, unmerged branch) will let users opt into a new UI per-surface, and that effort will need a "Legacy baseline" of usage/crash/session data to judge whether the new UI is healthy relative to today's WinForms app. FieldWorks already ships `SIL.DesktopAnalytics` (Mixpanel-backed), gated by the `OkToPingBasicUsageData` consent flag, but it tracks only sparse per-tool events and has no session concept — so the baseline that later work will need to compare against does not exist yet, and every day this ships without it is baseline data that can never be recovered. This change adds that baseline instrumentation entirely within the current WinForms app, with no dependency on the unmerged Avalonia work.

Offline durability of delivery (events generated while Mixpanel is unreachable) is deliberately **not** solved here. It is owned by the `SIL.DesktopAnalytics` library itself (a durable `MixpanelClient` with an on-disk spool and a real per-event delivery ack). FieldWorks calls `Analytics.Track`/`Analytics.ReportException` directly and inherits durability from the library once a build references the durable version — there is no FieldWorks-side queue.

## What Changes

- Call the existing (currently unused) `Analytics.SetApplicationProperty("UiFramework", "WinForms")` once at startup, so every event and exception report is segmentable by UI framework the moment a future second value exists. The value is a constant today; no code here reads or depends on any UIMode concept, since none exists in this codebase yet.
- Add session-level tracking: a session-start event at launch and a session-end event at shutdown, sufficient to compute session duration and a crash-free-session rate (a session ending via the unhandled-exception path must be distinguishable from a clean shutdown).
- Enrich the existing `SwitchToTool` event with per-tool dwell time (time spent in a tool before switching away or app exit), without changing its existing `area`/`tool` properties or its once-per-tool-per-day throttle.

## Non-Goals

- Not building any FieldWorks-side offline queue, retry, or persistence layer for analytics — offline durability is the `SIL.DesktopAnalytics` library's responsibility, not this change's.
- Not changing the hardcoded Mixpanel API keys in `FieldWorks.cs` — a pre-existing, unrelated secrets-hygiene issue noted but explicitly out of scope.
- Not adding, reading, or depending on `UIMode`, `LexicalEditSurfaceResolver`, or any Avalonia-branch-only concept. No such code exists on this branch; any anticipated future segmentation is represented as a constant value today, not a live branch.
- Not defining the Avalonia-side "bake metric" thresholds, rollout ladder, or beta-cohort mechanics — this change only produces the baseline data those future decisions would consume.
- Not changing what the consent flag means or how it's surfaced in the UI; it remains the sole authority for whether any telemetry (new or existing) is sent.

## Capabilities

### New Capabilities
- `telemetry-session-baseline`: session-start/session-end tracking with clean-vs-crash termination classification, enabling session duration and crash-free-session rate.
- `telemetry-usage-enrichment`: the `UiFramework` application property and `SwitchToTool` dwell-time enrichment.

### Modified Capabilities
(none — no existing `openspec/specs/` capability covers analytics/telemetry today)

## Impact

- **Managed C# only** (`Src/Common/FieldWorks/`, `Src/LexText/LexTextDll/`); no native (C++) changes.
- Touches the `Analytics` construction/shutdown path in `Src/Common/FieldWorks/FieldWorks.cs` and the `SwitchToTool` tracking in `Src/LexText/LexTextDll/AreaListener.cs`.
- Depends on the existing `SIL.DesktopAnalytics` package and the existing `OkToPingBasicUsageData` consent flag; introduces no new external dependencies and no new on-disk state.
- Testable via `.\test.ps1`; no changes to `build.ps1` phase ordering expected.
